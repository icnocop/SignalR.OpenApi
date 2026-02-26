// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignalR.OpenApi.Discovery;
using SignalR.OpenApi.Tests.TestHubs;

namespace SignalR.OpenApi.Tests;

/// <summary>
/// Tests for <see cref="ReflectionHubDiscoverer"/>.
/// </summary>
[TestClass]
public class ReflectionHubDiscovererTests
{
    /// <summary>
    /// Verifies basic hub discovery finds hubs in the configured assembly.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_FindsBasicHub()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var basic = hubs.FirstOrDefault(h => h.HubType == typeof(BasicHub));
        Assert.IsNotNull(basic);
        Assert.AreEqual("Basic", basic.Name);
    }

    /// <summary>
    /// Verifies typed hub discovery extracts client interface type.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_FindsTypedHub_WithClientInterface()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var typed = hubs.FirstOrDefault(h => h.HubType == typeof(TypedChatHub));
        Assert.IsNotNull(typed);
        Assert.AreEqual(typeof(IChatClient), typed.ClientInterfaceType);
    }

    /// <summary>
    /// Verifies client events are extracted from the typed hub interface.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_TypedHub_ExtractsClientEvents()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var typed = hubs.First(h => h.HubType == typeof(TypedChatHub));
        Assert.IsTrue(typed.ClientEvents.Count >= 2);

        var receiveMessage = typed.ClientEvents.FirstOrDefault(e => e.Name == "ReceiveMessage");
        Assert.IsNotNull(receiveMessage);
        Assert.AreEqual(2, receiveMessage.Parameters.Count);
    }

    /// <summary>
    /// Verifies streaming methods are detected.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_DetectsStreamingMethods()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var streaming = hubs.First(h => h.HubType == typeof(StreamingHub));
        var streamIntegers = streaming.Methods.First(m => m.Name == "StreamIntegers");

        Assert.IsTrue(streamIntegers.IsStreamingResponse);
        Assert.AreEqual(typeof(int), streamIntegers.StreamItemType);
    }

    /// <summary>
    /// Verifies ChannelReader streaming is detected.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_DetectsChannelReaderStreaming()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var streaming = hubs.First(h => h.HubType == typeof(StreamingHub));
        var channelMethod = streaming.Methods.First(m => m.Name == "StreamViaChannel");

        Assert.IsTrue(channelMethod.IsStreamingResponse);
        Assert.AreEqual(typeof(string), channelMethod.StreamItemType);
    }

    /// <summary>
    /// Verifies CancellationToken parameters are filtered out.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_FiltersCancellationTokenParameters()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var streaming = hubs.First(h => h.HubType == typeof(StreamingHub));
        var streamIntegers = streaming.Methods.First(m => m.Name == "StreamIntegers");

        Assert.IsTrue(streamIntegers.Parameters.All(p => p.ParameterType != typeof(CancellationToken)));
        Assert.AreEqual(1, streamIntegers.Parameters.Count);
    }

    /// <summary>
    /// Verifies hidden hubs are excluded.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ExcludesHiddenHub()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        Assert.IsFalse(hubs.Any(h => h.HubType == typeof(HiddenHub)));
    }

    /// <summary>
    /// Verifies hidden methods are excluded.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ExcludesHiddenMethods()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var attributeHub = hubs.First(h => h.HubType == typeof(AttributeHub));
        Assert.IsFalse(attributeHub.Methods.Any(m => m.Name == "HiddenMethod"));
    }

    /// <summary>
    /// Verifies Authorize attribute is detected on hub.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_DetectsAuthorizeAttribute()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var attributeHub = hubs.First(h => h.HubType == typeof(AttributeHub));
        Assert.IsTrue(attributeHub.RequiresAuthorization);
    }

    /// <summary>
    /// Verifies AllowAnonymous attribute is detected on methods.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_DetectsAllowAnonymous()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var attributeHub = hubs.First(h => h.HubType == typeof(AttributeHub));
        var healthCheck = attributeHub.Methods.First(m => m.Name == "HealthCheck");
        Assert.IsTrue(healthCheck.AllowAnonymous);
    }

    /// <summary>
    /// Verifies Obsolete attribute marks methods as deprecated.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_DetectsObsoleteAsDeprecated()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var attributeHub = hubs.First(h => h.HubType == typeof(AttributeHub));
        var legacy = attributeHub.Methods.First(m => m.Name == "GetUserLegacy");
        Assert.IsTrue(legacy.IsDeprecated);
    }

    /// <summary>
    /// Verifies EndpointName attribute overrides the operation ID.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ReadsEndpointNameAttribute()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var attributeHub = hubs.First(h => h.HubType == typeof(AttributeHub));
        var method = attributeHub.Methods.First(m => m.Name == "GetUserDetails");
        Assert.IsNotNull(method);
    }

    /// <summary>
    /// Verifies Tags attribute is read from hub.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ReadsTagsAttribute()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var attributeHub = hubs.First(h => h.HubType == typeof(AttributeHub));
        Assert.IsTrue(attributeHub.Tags.Contains("Admin"));
    }

    /// <summary>
    /// Verifies hub filter predicate is applied.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_AppliesHubFilter()
    {
        var discoverer = CreateDiscoverer(o =>
            o.HubFilter = t => t == typeof(BasicHub));

        var hubs = discoverer.DiscoverHubs();
        Assert.AreEqual(1, hubs.Count);
        Assert.AreEqual(typeof(BasicHub), hubs[0].HubType);
    }

    /// <summary>
    /// Verifies method filter predicate is applied.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_AppliesMethodFilter()
    {
        var discoverer = CreateDiscoverer(o =>
            o.MethodFilter = m => m.Name == "SendMessage");

        var hubs = discoverer.DiscoverHubs();
        var basic = hubs.First(h => h.HubType == typeof(BasicHub));
        Assert.AreEqual(1, basic.Methods.Count);
        Assert.AreEqual("SendMessage", basic.Methods[0].Name);
    }

    /// <summary>
    /// Verifies method names and operation IDs preserve the Async suffix
    /// since stripping is a UI-only concern.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_PreservesAsyncSuffixInNameAndOperationId()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var attributeHub = hubs.First(h => h.HubType == typeof(AttributeHub));
        var fetchData = attributeHub.Methods.First(m => m.Name == "FetchDataAsync");

        Assert.AreEqual("FetchDataAsync", fetchData.Name);
        Assert.AreEqual("Attribute_FetchDataAsync", fetchData.OperationId);
    }

    /// <summary>
    /// Verifies return types are properly unwrapped from Task wrappers.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_UnwrapsReturnType()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var basic = hubs.First(h => h.HubType == typeof(BasicHub));
        var sendMessage = basic.Methods.First(m => m.Name == "SendMessage");
        Assert.AreEqual(typeof(string), sendMessage.ReturnType);
    }

    /// <summary>
    /// Verifies data annotation attributes are read from parameters.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ReadsDataAnnotationsOnModel()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var attributeHub = hubs.First(h => h.HubType == typeof(AttributeHub));
        var validateMethod = attributeHub.Methods.First(m => m.Name == "ValidateInput");
        Assert.AreEqual(1, validateMethod.Parameters.Count);
        Assert.AreEqual(typeof(ValidatedModel), validateMethod.Parameters[0].ParameterType);
    }

    /// <summary>
    /// Verifies base Hub methods are not included.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ExcludesBaseHubMethods()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var basic = hubs.First(h => h.HubType == typeof(BasicHub));
        Assert.IsFalse(basic.Methods.Any(m => m.Name == "OnConnectedAsync"));
        Assert.IsFalse(basic.Methods.Any(m => m.Name == "OnDisconnectedAsync"));
        Assert.IsFalse(basic.Methods.Any(m => m.Name == "Dispose"));
    }

    /// <summary>
    /// Verifies that XML docs are resolved from interface methods via inheritdoc.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_InheritDoc_ResolvesXmlDocsFromInterface()
    {
        var discoverer = CreateDiscoverer();
        var hubs = discoverer.DiscoverHubs();

        var hub = hubs.First(h => h.HubType == typeof(InheritDocHub));
        Assert.AreEqual("Defines the server-side methods for the inherited-doc hub.", hub.Summary);

        var greet = hub.Methods.First(m => m.Name == "Greet");
        Assert.AreEqual("Greets a user by name.", greet.Summary);
        Assert.AreEqual("A greeting message.", greet.ReturnDescription);

        var nameParam = greet.Parameters.First(p => p.Name == "name");
        Assert.AreEqual("The user's name.", nameParam.Description);
    }

    private static ReflectionHubDiscoverer CreateDiscoverer(Action<SignalROpenApiOptions>? configure = null)
    {
        var options = new SignalROpenApiOptions
        {
            Assemblies = [typeof(BasicHub).Assembly],
        };

        configure?.Invoke(options);
        return new ReflectionHubDiscoverer(Options.Create(options));
    }
}
