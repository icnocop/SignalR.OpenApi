// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.SingleObjectParameter;

/// <summary>
/// Tests for hub methods that take a single complex object parameter. The object's
/// properties are flattened into the request body with no wrapper property.
/// </summary>
[TestClass]
public class SingleObjectParameterTests
{
    /// <summary>
    /// Verifies the object's properties are flattened into the request body root.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_FlattensPropertiesIntoRequestBody()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(SingleObjectParameterHub));

        var saveProfile = doc.Paths["/hubs/SingleObjectParameter/SaveProfile"]
            .Operations[OperationType.Post];

        var schema = saveProfile.RequestBody!.Content["application/json"].Schema;
        Assert.AreEqual("object", schema.Type);

        Assert.IsFalse(schema.Properties.ContainsKey("profile"), "Should not have a wrapper 'profile' property.");
        Assert.IsTrue(schema.Properties.ContainsKey("name"));
        Assert.IsTrue(schema.Properties.ContainsKey("email"));
        Assert.IsTrue(schema.Properties.ContainsKey("age"));
    }

    /// <summary>
    /// Verifies data annotations on the model are applied to the flattened schema.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AppliesDataAnnotationsToSchema()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(SingleObjectParameterHub));

        var saveProfile = doc.Paths["/hubs/SingleObjectParameter/SaveProfile"]
            .Operations[OperationType.Post];

        var schema = saveProfile.RequestBody!.Content["application/json"].Schema;

        Assert.AreEqual(100, schema.Properties["name"].MaxLength, "StringLength should set maxLength.");
        Assert.AreEqual(1, schema.Properties["name"].MinLength, "StringLength should set minLength.");
        Assert.AreEqual("email", schema.Properties["email"].Format, "EmailAddress should set the email format.");
        Assert.AreEqual(0m, schema.Properties["age"].Minimum, "Range should set minimum.");
        Assert.AreEqual(150m, schema.Properties["age"].Maximum, "Range should set maximum.");
    }

    /// <summary>
    /// Verifies a flat object parameter is also offered as form-urlencoded.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_FlatObjectParameterHasFormUrlEncoded()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(SingleObjectParameterHub));

        var saveProfile = doc.Paths["/hubs/SingleObjectParameter/SaveProfile"]
            .Operations[OperationType.Post];

        Assert.IsTrue(saveProfile.RequestBody!.Content.ContainsKey("application/json"));
        Assert.IsTrue(saveProfile.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"));
    }

    /// <summary>
    /// Verifies flattenedBody is true for a single complex object parameter.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_FlattenedBodyIsTrue()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(SingleObjectParameterHub));

        var saveProfile = doc.Paths["/hubs/SingleObjectParameter/SaveProfile"]
            .Operations[OperationType.Post];

        var ext = (Microsoft.OpenApi.Any.OpenApiObject)saveProfile.Extensions["x-signalr"];
        var flattenedBody = ((Microsoft.OpenApi.Any.OpenApiBoolean)ext["flattenedBody"]).Value;
        Assert.IsTrue(flattenedBody, "flattenedBody should be true for a single complex object parameter.");
    }
}
