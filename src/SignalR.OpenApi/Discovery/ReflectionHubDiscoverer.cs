// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml;
using System.Xml.XPath;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using SignalR.OpenApi.Examples;
using SignalR.OpenApi.Models;

namespace SignalR.OpenApi.Discovery;

/// <summary>
/// Discovers SignalR hubs using reflection and extracts metadata
/// from standard ASP.NET Core attributes and XML documentation.
/// </summary>
public sealed class ReflectionHubDiscoverer : IHubDiscoverer
{
    private static readonly HashSet<string> BaseHubMethodNames = new(
        typeof(Hub).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name),
        StringComparer.Ordinal);

    private readonly SignalROpenApiOptions options;
    private readonly Dictionary<string, XPathNavigator?> xmlDocCache = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionHubDiscoverer"/> class.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public ReflectionHubDiscoverer(IOptions<SignalROpenApiOptions> options)
    {
        this.options = options.Value;
    }

    /// <inheritdoc/>
    public IReadOnlyList<SignalRHubInfo> DiscoverHubs()
    {
        var assemblies = this.options.Assemblies;
        if (assemblies.Count == 0)
        {
            var entry = Assembly.GetEntryAssembly();
            if (entry is not null)
            {
                assemblies = [entry];
            }
        }

        var hubTypes = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(IsHubType)
            .Where(t => !IsExcluded(t))
            .Where(t => this.options.HubFilter?.Invoke(t) ?? true)
            .ToList();

        return hubTypes.Select(this.CreateHubInfo).ToList();
    }

    private static bool IsHubType(Type type)
    {
        return !type.IsAbstract
            && type.IsClass
            && IsSubclassOfHub(type);
    }

    private static bool IsSubclassOfHub(Type type)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current == typeof(Hub))
            {
                return true;
            }

            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Hub<>))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static bool IsExcluded(MemberInfo member)
    {
        if (member.GetCustomAttribute<ApiExplorerSettingsAttribute>() is { IgnoreApi: true })
        {
            return true;
        }

        if (member.GetCustomAttribute<ExcludeFromDescriptionAttribute>() is not null)
        {
            return true;
        }

        return false;
    }

    private static Type? GetClientInterfaceType(Type hubType)
    {
        var current = hubType.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Hub<>))
            {
                return current.GetGenericArguments()[0];
            }

            current = current.BaseType;
        }

        return null;
    }

    private static Type? UnwrapReturnType(Type returnType)
    {
        if (returnType == typeof(void) || returnType == typeof(Task) || returnType == typeof(ValueTask))
        {
            return null;
        }

        if (returnType.IsGenericType)
        {
            var genericDef = returnType.GetGenericTypeDefinition();
            if (genericDef == typeof(Task<>) || genericDef == typeof(ValueTask<>))
            {
                return returnType.GetGenericArguments()[0];
            }
        }

        return returnType;
    }

    private static bool IsStreamingType(Type type, out Type? itemType)
    {
        itemType = null;

        if (!type.IsGenericType)
        {
            return false;
        }

        var genericDef = type.GetGenericTypeDefinition();

        if (genericDef == typeof(IAsyncEnumerable<>) || genericDef == typeof(ChannelReader<>))
        {
            itemType = type.GetGenericArguments()[0];
            return true;
        }

        // Check for Task<ChannelReader<T>> or Task<IAsyncEnumerable<T>>
        if (genericDef == typeof(Task<>) || genericDef == typeof(ValueTask<>))
        {
            var innerType = type.GetGenericArguments()[0];
            if (innerType.IsGenericType)
            {
                var innerGenericDef = innerType.GetGenericTypeDefinition();
                if (innerGenericDef == typeof(IAsyncEnumerable<>) || innerGenericDef == typeof(ChannelReader<>))
                {
                    itemType = innerType.GetGenericArguments()[0];
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsCancellationToken(Type type)
    {
        return type == typeof(CancellationToken);
    }

    private static bool IsStreamingInputParameter(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericDef = type.GetGenericTypeDefinition();
        return genericDef == typeof(IAsyncEnumerable<>) || genericDef == typeof(ChannelReader<>);
    }

    private static string GetHubName(Type hubType)
    {
        var endpointName = hubType.GetCustomAttribute<EndpointNameAttribute>();
        if (endpointName is not null)
        {
            return endpointName.EndpointName;
        }

        var name = hubType.Name;
        if (name.EndsWith("Hub", StringComparison.Ordinal))
        {
            name = name[..^3];
        }

        return name;
    }

    private static IReadOnlyList<string> GetTags(MemberInfo member, string defaultTag)
    {
        var tagsAttr = member.GetCustomAttribute<TagsAttribute>();
        if (tagsAttr is not null)
        {
            return tagsAttr.Tags.ToList();
        }

        return [defaultTag];
    }

    private static bool HasAuthorizeAttribute(MemberInfo member)
    {
        return member.GetCustomAttribute<AuthorizeAttribute>() is not null;
    }

    private static IReadOnlyList<string> GetAuthorizationPolicies(MemberInfo member)
    {
        return member.GetCustomAttributes<AuthorizeAttribute>()
            .Where(a => !string.IsNullOrEmpty(a.Policy))
            .Select(a => a.Policy!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string? GetEndpointDescription(MemberInfo member)
    {
        return member.GetCustomAttribute<EndpointDescriptionAttribute>()?.Description;
    }

    private static string GetMethodName(MethodInfo method)
    {
        var endpointName = method.GetCustomAttribute<EndpointNameAttribute>();
        if (endpointName is not null)
        {
            return endpointName.EndpointName;
        }

        return method.Name;
    }

    private static string GetXmlMemberName(MemberInfo member)
    {
        if (member is Type type)
        {
            return $"T:{type.FullName}";
        }

        if (member is MethodInfo method)
        {
            var parameters = method.GetParameters();
            var paramString = parameters.Length > 0
                ? $"({string.Join(",", parameters.Select(p => p.ParameterType.FullName))})"
                : string.Empty;
            return $"M:{method.DeclaringType!.FullName}.{method.Name}{paramString}";
        }

        return $"P:{member.DeclaringType!.FullName}.{member.Name}";
    }

    private static string? GetNodeText(XPathNavigator? node)
    {
        if (node is null)
        {
            return null;
        }

        var innerXml = node.InnerXml?.Trim();
        if (string.IsNullOrEmpty(innerXml))
        {
            return null;
        }

        // Replace <see cref="T:Namespace.TypeName"/> and <see cref="..."/> with the short type name.
        var result = Regex.Replace(innerXml, @"<see\s+cref=""[^""]*?\.([^"".]+)""\s*/>", "$1");

        // Replace <paramref name="paramName"/> with the parameter name.
        result = Regex.Replace(result, @"<paramref\s+name=""([^""]+)""\s*/>", "$1");

        // Strip any remaining XML tags.
        result = Regex.Replace(result, @"<[^>]+>", string.Empty);

        // Collapse multiple spaces into one.
        result = Regex.Replace(result, @"\s{2,}", " ");

        return result.Trim();
    }

    private SignalRHubInfo CreateHubInfo(Type hubType)
    {
        var clientInterfaceType = GetClientInterfaceType(hubType);
        var hubName = GetHubName(hubType);

        var hubInfo = new SignalRHubInfo
        {
            HubType = hubType,
            Name = hubName,
            Summary = this.GetXmlSummary(hubType),
            Description = this.GetXmlRemarks(hubType) ?? GetEndpointDescription(hubType),
            Tags = GetTags(hubType, hubName),
            RequiresAuthorization = HasAuthorizeAttribute(hubType),
            AuthorizationPolicies = GetAuthorizationPolicies(hubType),
            IsDeprecated = hubType.GetCustomAttribute<ObsoleteAttribute>() is not null,
            ClientInterfaceType = clientInterfaceType,
        };

        if (this.options.HubRoutes.TryGetValue(hubType, out var route))
        {
            hubInfo.Path = route;
        }

        hubInfo.Methods = this.DiscoverMethods(hubType);
        hubInfo.ClientEvents = clientInterfaceType is not null
            ? this.DiscoverClientEvents(clientInterfaceType)
            : [];

        return hubInfo;
    }

    private IReadOnlyList<SignalRMethodInfo> DiscoverMethods(Type hubType)
    {
        return hubType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Where(m => !BaseHubMethodNames.Contains(m.Name))
            .Where(m => !IsExcluded(m))
            .Where(m => this.options.MethodFilter?.Invoke(m) ?? true)
            .Select(m => this.CreateMethodInfo(m, hubType))
            .ToList();
    }

    private SignalRMethodInfo CreateMethodInfo(MethodInfo method, Type hubType)
    {
        var hubName = GetHubName(hubType);
        var methodName = GetMethodName(method);
        var returnType = method.ReturnType;
        var isStreaming = IsStreamingType(returnType, out var streamItemType);

        var actualReturnType = isStreaming
            ? streamItemType
            : UnwrapReturnType(returnType);

        var producesContentTypes = method.GetCustomAttributes<ProducesAttribute>()
            .SelectMany(p => p.ContentTypes)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new SignalRMethodInfo
        {
            MethodInfo = method,
            Name = methodName,
            OperationId = this.GenerateOperationId(hubName, method),
            Summary = this.GetXmlSummary(method)
                ?? method.GetCustomAttribute<EndpointSummaryAttribute>()?.Summary,
            Description = this.GetXmlRemarks(method)
                ?? GetEndpointDescription(method),
            Tags = GetTags(method, hubName),
            Parameters = this.DiscoverParameters(method),
            ReturnType = actualReturnType,
            ReturnDescription = this.GetXmlReturns(method),
            ProducesContentTypes = producesContentTypes,
            IsStreamingResponse = isStreaming,
            StreamItemType = streamItemType,
            RequiresAuthorization = HasAuthorizeAttribute(method),
            AllowAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() is not null,
            AuthorizationPolicies = GetAuthorizationPolicies(method),
            IsDeprecated = method.GetCustomAttribute<ObsoleteAttribute>() is not null,
            Example = this.GetXmlExample(method),
            RequestExampleProviderTypes = method.GetCustomAttributes<SignalROpenApiRequestExamplesAttribute>()
                .Select(a => a.ExamplesProviderType)
                .ToList(),
            ResponseExampleProviderTypes = method.GetCustomAttributes<SignalROpenApiResponseExamplesAttribute>()
                .Select(a => a.ExamplesProviderType)
                .ToList(),
        };
    }

    private string GenerateOperationId(string hubName, MethodInfo method)
    {
        var methodName = method.Name;
        if (this.options.StripAsyncSuffix && methodName.EndsWith("Async", StringComparison.Ordinal))
        {
            methodName = methodName[..^5];
        }

        return $"{hubName}_{methodName}";
    }

    private IReadOnlyList<SignalRParameterInfo> DiscoverParameters(MethodInfo method)
    {
        return method.GetParameters()
            .Where(p => !IsCancellationToken(p.ParameterType))
            .Select(p => this.CreateParameterInfo(p, method))
            .ToList();
    }

    private SignalRParameterInfo CreateParameterInfo(ParameterInfo param, MethodInfo method)
    {
        var paramType = param.ParameterType;
        var isStreamingInput = IsStreamingInputParameter(paramType);
        Type? streamItemType = null;

        if (isStreamingInput && paramType.IsGenericType)
        {
            streamItemType = paramType.GetGenericArguments()[0];
        }

        var description = param.GetCustomAttribute<DescriptionAttribute>()?.Description
            ?? this.GetXmlParamDescription(method, param.Name!);

        var isRequired = param.GetCustomAttribute<RequiredAttribute>() is not null
            || (!param.HasDefaultValue && Nullable.GetUnderlyingType(paramType) is null && !paramType.IsClass);

        return new SignalRParameterInfo
        {
            ParameterInfo = param,
            Name = param.Name ?? $"arg{param.Position}",
            ParameterType = paramType,
            Description = description,
            IsRequired = isRequired,
            HasDefaultValue = param.HasDefaultValue,
            DefaultValue = param.HasDefaultValue ? param.DefaultValue : null,
            IsStreamingInput = isStreamingInput,
            StreamItemType = streamItemType,
        };
    }

    private IReadOnlyList<SignalRClientEventInfo> DiscoverClientEvents(Type clientInterface)
    {
        return clientInterface
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => new SignalRClientEventInfo
            {
                Name = m.Name,
                Summary = this.GetXmlSummary(m),
                Description = this.GetXmlRemarks(m),
                Parameters = m.GetParameters()
                    .Select(p => new SignalRParameterInfo
                    {
                        ParameterInfo = p,
                        Name = p.Name ?? $"arg{p.Position}",
                        ParameterType = p.ParameterType,
                        Description = this.GetXmlParamDescription(m, p.Name!),
                        IsRequired = true,
                    })
                    .ToList(),
            })
            .ToList();
    }

    // XML documentation helpers
    private XPathNavigator? GetXmlDocNavigator(Type type)
    {
        var assembly = type.Assembly;
        var xmlFile = Path.ChangeExtension(assembly.Location, ".xml");

        if (this.xmlDocCache.TryGetValue(xmlFile, out var cached))
        {
            return cached;
        }

        if (!File.Exists(xmlFile))
        {
            this.xmlDocCache[xmlFile] = null;
            return null;
        }

        var doc = new XPathDocument(xmlFile);
        var navigator = doc.CreateNavigator();
        this.xmlDocCache[xmlFile] = navigator;
        return navigator;
    }

    private string? GetXmlSummary(MemberInfo member)
    {
        var ownerType = member as Type ?? member.DeclaringType ?? member.ReflectedType;
        if (ownerType is null)
        {
            return null;
        }

        var nav = this.GetXmlDocNavigator(ownerType);
        var memberName = GetXmlMemberName(member);
        var node = nav?.SelectSingleNode($"/doc/members/member[@name='{memberName}']/summary");
        return GetNodeText(node);
    }

    private string? GetXmlRemarks(MemberInfo member)
    {
        var ownerType = member as Type ?? member.DeclaringType ?? member.ReflectedType;
        if (ownerType is null)
        {
            return null;
        }

        var nav = this.GetXmlDocNavigator(ownerType);
        var memberName = GetXmlMemberName(member);
        var node = nav?.SelectSingleNode($"/doc/members/member[@name='{memberName}']/remarks");
        return GetNodeText(node);
    }

    private string? GetXmlReturns(MethodInfo method)
    {
        var nav = this.GetXmlDocNavigator(method.DeclaringType ?? method.ReflectedType!);
        var memberName = GetXmlMemberName(method);
        var node = nav?.SelectSingleNode($"/doc/members/member[@name='{memberName}']/returns");
        return GetNodeText(node);
    }

    private string? GetXmlParamDescription(MethodInfo method, string paramName)
    {
        var nav = this.GetXmlDocNavigator(method.DeclaringType ?? method.ReflectedType!);
        var memberName = GetXmlMemberName(method);
        var node = nav?.SelectSingleNode($"/doc/members/member[@name='{memberName}']/param[@name='{paramName}']");
        return GetNodeText(node);
    }

    private string? GetXmlExample(MethodInfo method)
    {
        var nav = this.GetXmlDocNavigator(method.DeclaringType ?? method.ReflectedType!);
        var memberName = GetXmlMemberName(method);
        var node = nav?.SelectSingleNode($"/doc/members/member[@name='{memberName}']/example");
        return GetNodeText(node);
    }
}
