// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignalR.OpenApi.Discovery;
using SignalR.OpenApi.Models;

namespace SignalR.OpenApi.Tests.Discovery;

/// <summary>
/// Tests for <see cref="ReflectionHubDiscoverer"/>.
/// </summary>
/// <remarks>
/// Discovery is assembly-wide by design, so these tests locate their own hubs by type
/// rather than filtering. The fixtures live alongside this file so they are not shared
/// with — or perturbed by — the document generator tests.
/// </remarks>
[TestClass]
public class ReflectionHubDiscovererTests
{
    /// <summary>
    /// Verifies hubs in the configured assembly are discovered and named.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_FindsHub()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        var hub = hubs.FirstOrDefault(h => h.HubType == typeof(DiscoveryHub));
        Assert.IsNotNull(hub);
        Assert.AreEqual("Discovery", hub.Name, "The trailing 'Hub' suffix should be stripped.");
    }

    /// <summary>
    /// Verifies the client interface of a strongly typed hub is captured.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_FindsTypedHub_WithClientInterface()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        var typed = hubs.FirstOrDefault(h => h.HubType == typeof(TypedDiscoveryHub));
        Assert.IsNotNull(typed);
        Assert.AreEqual(typeof(ITypedDiscoveryClient), typed.ClientInterfaceType);
    }

    /// <summary>
    /// Verifies client events are extracted from the typed hub's interface.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_TypedHub_ExtractsClientEvents()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        var typed = hubs.First(h => h.HubType == typeof(TypedDiscoveryHub));
        Assert.AreEqual(2, typed.ClientEvents.Count);

        var receiveMessage = typed.ClientEvents.FirstOrDefault(e => e.Name == "ReceiveMessage");
        Assert.IsNotNull(receiveMessage);
        Assert.AreEqual(2, receiveMessage.Parameters.Count);
    }

    /// <summary>
    /// Verifies IAsyncEnumerable methods are detected as streaming.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_DetectsStreamingMethods()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        var streaming = hubs.First(h => h.HubType == typeof(StreamingDiscoveryHub));
        var streamIntegers = streaming.Methods.First(m => m.Name == "StreamIntegers");

        Assert.IsTrue(streamIntegers.IsStreamingResponse);
        Assert.AreEqual(typeof(int), streamIntegers.StreamItemType);
    }

    /// <summary>
    /// Verifies ChannelReader methods are detected as streaming.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_DetectsChannelReaderStreaming()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        var streaming = hubs.First(h => h.HubType == typeof(StreamingDiscoveryHub));
        var channelMethod = streaming.Methods.First(m => m.Name == "StreamViaChannel");

        Assert.IsTrue(channelMethod.IsStreamingResponse);
        Assert.AreEqual(typeof(string), channelMethod.StreamItemType);
    }

    /// <summary>
    /// Verifies CancellationToken parameters are not surfaced as request parameters.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_FiltersCancellationTokenParameters()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        var streaming = hubs.First(h => h.HubType == typeof(StreamingDiscoveryHub));
        var streamIntegers = streaming.Methods.First(m => m.Name == "StreamIntegers");

        Assert.IsTrue(streamIntegers.Parameters.All(p => p.ParameterType != typeof(CancellationToken)));
        Assert.AreEqual(1, streamIntegers.Parameters.Count);
    }

    /// <summary>
    /// Verifies hubs marked with [ApiExplorerSettings(IgnoreApi = true)] are excluded.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ExcludesHiddenHub()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        Assert.IsFalse(hubs.Any(h => h.HubType == typeof(HiddenDiscoveryHub)));
    }

    /// <summary>
    /// Verifies methods marked with [ApiExplorerSettings(IgnoreApi = true)] are excluded.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ExcludesHiddenMethods()
    {
        var hub = GetAttributeHub();

        Assert.IsFalse(hub.Methods.Any(m => m.Name == "HiddenMethod"));
    }

    /// <summary>
    /// Verifies [Authorize] on the hub is detected.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_DetectsAuthorizeAttribute()
    {
        Assert.IsTrue(GetAttributeHub().RequiresAuthorization);
    }

    /// <summary>
    /// Verifies [AllowAnonymous] on a method is detected.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_DetectsAllowAnonymous()
    {
        var healthCheck = GetAttributeHub().Methods.First(m => m.Name == "HealthCheck");

        Assert.IsTrue(healthCheck.AllowAnonymous);
    }

    /// <summary>
    /// Verifies [Obsolete] marks a method deprecated.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_DetectsObsoleteAsDeprecated()
    {
        var legacy = GetAttributeHub().Methods.First(m => m.Name == "GetUserLegacy");

        Assert.IsTrue(legacy.IsDeprecated);
    }

    /// <summary>
    /// Verifies [EndpointName] renames the discovered method.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ReadsEndpointNameAttribute()
    {
        var method = GetAttributeHub().Methods.FirstOrDefault(m => m.Name == "GetUserDetails");

        Assert.IsNotNull(method, "[EndpointName] should override the method name.");
    }

    /// <summary>
    /// Verifies [Tags] on the hub is read.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ReadsTagsAttribute()
    {
        Assert.IsTrue(GetAttributeHub().Tags.Contains("Admin"));
    }

    /// <summary>
    /// Verifies the hub filter predicate is applied.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_AppliesHubFilter()
    {
        var discoverer = CreateDiscoverer(o => o.HubFilter = t => t == typeof(DiscoveryHub));

        var hubs = discoverer.DiscoverHubs();

        Assert.AreEqual(1, hubs.Count);
        Assert.AreEqual(typeof(DiscoveryHub), hubs[0].HubType);
    }

    /// <summary>
    /// Verifies the method filter predicate is applied.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_AppliesMethodFilter()
    {
        var discoverer = CreateDiscoverer(o => o.MethodFilter = m => m.Name == "SendMessage");

        var hubs = discoverer.DiscoverHubs();

        var hub = hubs.First(h => h.HubType == typeof(DiscoveryHub));
        Assert.AreEqual(1, hub.Methods.Count);
        Assert.AreEqual("SendMessage", hub.Methods[0].Name);
    }

    /// <summary>
    /// Verifies the Async suffix survives in the method name and operation ID, since
    /// stripping it is a UI-only concern.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_PreservesAsyncSuffixInNameAndOperationId()
    {
        var fetchData = GetAttributeHub().Methods.First(m => m.Name == "FetchDataAsync");

        Assert.AreEqual("FetchDataAsync", fetchData.Name);
        Assert.AreEqual("AttributeDiscovery_FetchDataAsync", fetchData.OperationId);
    }

    /// <summary>
    /// Verifies Task wrappers are unwrapped to the underlying return type.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_UnwrapsReturnType()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        var hub = hubs.First(h => h.HubType == typeof(DiscoveryHub));
        var sendMessage = hub.Methods.First(m => m.Name == "SendMessage");

        Assert.AreEqual(typeof(string), sendMessage.ReturnType);
    }

    /// <summary>
    /// Verifies complex parameter types are captured.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ReadsDataAnnotationsOnModel()
    {
        var validateMethod = GetAttributeHub().Methods.First(m => m.Name == "ValidateInput");

        Assert.AreEqual(1, validateMethod.Parameters.Count);
        Assert.AreEqual(typeof(DiscoveryValidatedModel), validateMethod.Parameters[0].ParameterType);
    }

    /// <summary>
    /// Verifies inherited <see cref="Microsoft.AspNetCore.SignalR.Hub"/> members are not
    /// treated as hub methods.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ExcludesBaseHubMethods()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        var hub = hubs.First(h => h.HubType == typeof(DiscoveryHub));
        Assert.IsFalse(hub.Methods.Any(m => m.Name == "OnConnectedAsync"));
        Assert.IsFalse(hub.Methods.Any(m => m.Name == "OnDisconnectedAsync"));
        Assert.IsFalse(hub.Methods.Any(m => m.Name == "Dispose"));
    }

    /// <summary>
    /// Verifies XML docs are resolved through &lt;inheritdoc /&gt; from the interface.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_InheritDoc_ResolvesXmlDocsFromInterface()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        var hub = hubs.First(h => h.HubType == typeof(InheritDocHub));
        Assert.AreEqual("Defines the server-side methods for the inherited-doc hub.", hub.Summary);

        var greet = hub.Methods.First(m => m.Name == "Greet");
        Assert.AreEqual("Greets a user by name.", greet.Summary);
        Assert.AreEqual("A greeting message.", greet.ReturnDescription);

        var nameParam = greet.Parameters.First(p => p.Name == "name");
        Assert.AreEqual("The user's name.", nameParam.Description);
    }

    /// <summary>
    /// Verifies [Tags] on a client event method is read.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ReadsTagsAttributeOnClientEvent()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        var typed = hubs.First(h => h.HubType == typeof(TypedDiscoveryHub));
        var userJoined = typed.ClientEvents.First(e => e.Name == "UserJoined");

        CollectionAssert.Contains(userJoined.Tags.ToList(), "Presence");
    }

    /// <summary>
    /// Verifies client events without [Tags] have no tags of their own.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ClientEventWithoutTagsAttribute_HasEmptyTags()
    {
        var hubs = CreateDiscoverer().DiscoverHubs();

        var typed = hubs.First(h => h.HubType == typeof(TypedDiscoveryHub));
        var receiveMessage = typed.ClientEvents.First(e => e.Name == "ReceiveMessage");

        Assert.AreEqual(0, receiveMessage.Tags.Count);
    }

    private static ReflectionHubDiscoverer CreateDiscoverer(Action<SignalROpenApiOptions>? configure = null)
    {
        var options = new SignalROpenApiOptions
        {
            Assemblies = [typeof(DiscoveryHub).Assembly],
        };

        configure?.Invoke(options);
        return new ReflectionHubDiscoverer(Options.Create(options));
    }

    private static SignalRHubInfo GetAttributeHub()
    {
        return CreateDiscoverer().DiscoverHubs().First(h => h.HubType == typeof(AttributeDiscoveryHub));
    }
}
