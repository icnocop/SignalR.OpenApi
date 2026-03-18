// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SignalR.OpenApi.Examples;
using SignalR.OpenApi.Models;

namespace SignalR.OpenApi.Generation;

/// <summary>
/// Generates OpenAPI 3.1 documents from SignalR hub metadata.
/// </summary>
public sealed class SignalROpenApiDocumentGenerator : ISignalROpenApiDocumentGenerator
{
    private readonly SignalROpenApiOptions options;
    private readonly IServiceProvider serviceProvider;
    private readonly IReadOnlyList<ISignalROpenApiSchemaProcessor> schemaProcessors;
    private readonly Dictionary<Type, string> schemaRegistry = [];
    private OpenApiDocument? currentDocument;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalROpenApiDocumentGenerator"/> class.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <param name="serviceProvider">The service provider for resolving example providers.</param>
    /// <param name="schemaProcessors">Optional schema processors to apply after schema creation.</param>
    public SignalROpenApiDocumentGenerator(
        IOptions<SignalROpenApiOptions> options,
        IServiceProvider serviceProvider,
        IEnumerable<ISignalROpenApiSchemaProcessor>? schemaProcessors = null)
    {
        this.options = options.Value;
        this.serviceProvider = serviceProvider;
        this.schemaProcessors = schemaProcessors?.ToList() ?? [];
    }

    /// <inheritdoc/>
    public OpenApiDocument GenerateDocument(IReadOnlyList<SignalRHubInfo> hubs)
    {
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = this.options.DocumentTitle,
                Version = this.options.DocumentVersion,
                Description = "OpenAPI specification for SignalR hubs. Operations represent hub methods invoked via the SignalR protocol, not traditional HTTP endpoints.",
            },
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema>(),
            },
        };

        this.currentDocument = document;
        this.schemaRegistry.Clear();

        var requiresAuth = false;

        foreach (var hub in hubs)
        {
            if (hub.RequiresAuthorization)
            {
                requiresAuth = true;
            }

            this.AddHubOperations(document, hub);
            this.AddClientEventOperations(document, hub);
        }

        if (requiresAuth)
        {
            AddSecuritySchemes(document);
        }

        return document;
    }

    private static void AddSignalRExtension(
        OpenApiOperation operation,
        string hubName,
        string methodName,
        string? hubPath = null,
        bool isStream = false,
        bool isClientEvent = false,
        int parameterCount = 0,
        bool flattenedBody = false,
        string? discriminatorProperty = null,
        string? discriminatorValue = null,
        IReadOnlyList<string>? parameterNames = null)
    {
        var extension = new Microsoft.OpenApi.Any.OpenApiObject
        {
            ["hub"] = new Microsoft.OpenApi.Any.OpenApiString(hubName),
            ["method"] = new Microsoft.OpenApi.Any.OpenApiString(methodName),
            ["stream"] = new Microsoft.OpenApi.Any.OpenApiBoolean(isStream),
            ["clientEvent"] = new Microsoft.OpenApi.Any.OpenApiBoolean(isClientEvent),
            ["parameterCount"] = new Microsoft.OpenApi.Any.OpenApiInteger(parameterCount),
            ["flattenedBody"] = new Microsoft.OpenApi.Any.OpenApiBoolean(flattenedBody),
        };

        if (hubPath is not null)
        {
            extension["hubPath"] = new Microsoft.OpenApi.Any.OpenApiString(hubPath);
        }

        if (discriminatorProperty is not null)
        {
            extension["discriminatorProperty"] = new Microsoft.OpenApi.Any.OpenApiString(discriminatorProperty);
        }

        if (discriminatorValue is not null)
        {
            extension["discriminatorValue"] = new Microsoft.OpenApi.Any.OpenApiString(discriminatorValue);
        }

        if (parameterNames is not null && parameterNames.Count > 0)
        {
            var namesArray = new Microsoft.OpenApi.Any.OpenApiArray();
            foreach (var name in parameterNames)
            {
                namesArray.Add(new Microsoft.OpenApi.Any.OpenApiString(name));
            }

            extension["parameterNames"] = namesArray;
        }

        operation.Extensions["x-signalr"] = extension;
    }

    private static void AddSecuritySchemes(OpenApiDocument document)
    {
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token for SignalR hub authentication. The token is passed via the SignalR connection's accessTokenFactory.",
        };
    }

    private static void ApplyDataAnnotations(OpenApiSchema schema, ICustomAttributeProvider member)
    {
        var stringLength = GetAttribute<StringLengthAttribute>(member);
        if (stringLength is not null)
        {
            schema.MinLength = stringLength.MinimumLength;
            schema.MaxLength = stringLength.MaximumLength;
        }

        var maxLength = GetAttribute<MaxLengthAttribute>(member);
        if (maxLength is not null)
        {
            schema.MaxLength = maxLength.Length;
        }

        var minLength = GetAttribute<MinLengthAttribute>(member);
        if (minLength is not null)
        {
            schema.MinLength = minLength.Length;
        }

        var range = GetAttribute<RangeAttribute>(member);
        if (range is not null)
        {
            if (range.Minimum is IConvertible min)
            {
                schema.Minimum = Convert.ToDecimal(min);
            }

            if (range.Maximum is IConvertible max)
            {
                schema.Maximum = Convert.ToDecimal(max);
            }
        }

        var regex = GetAttribute<RegularExpressionAttribute>(member);
        if (regex is not null)
        {
            schema.Pattern = regex.Pattern;
        }

        var emailAddress = GetAttribute<EmailAddressAttribute>(member);
        if (emailAddress is not null)
        {
            schema.Format = "email";
        }

        var url = GetAttribute<UrlAttribute>(member);
        if (url is not null)
        {
            schema.Format = "uri";
        }

        var phone = GetAttribute<PhoneAttribute>(member);
        if (phone is not null)
        {
            schema.Format = "tel";
        }
    }

    private static T? GetAttribute<T>(ICustomAttributeProvider member)
        where T : Attribute
    {
        var attrs = member.GetCustomAttributes(typeof(T), true);
        return attrs.Length > 0 ? (T)attrs[0] : null;
    }

    private static Microsoft.OpenApi.Any.IOpenApiAny? CreateOpenApiAnyValue(object value)
    {
        return value switch
        {
            string s => new Microsoft.OpenApi.Any.OpenApiString(s),
            int i => new Microsoft.OpenApi.Any.OpenApiInteger(i),
            long l => new Microsoft.OpenApi.Any.OpenApiLong(l),
            float f => new Microsoft.OpenApi.Any.OpenApiFloat(f),
            double d => new Microsoft.OpenApi.Any.OpenApiDouble(d),
            bool b => new Microsoft.OpenApi.Any.OpenApiBoolean(b),
            _ => null,
        };
    }

    private static Type? GetEnumerableItemType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static (Type KeyType, Type ValueType)? GetDictionaryTypes(Type type)
    {
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                var args = iface.GetGenericArguments();
                return (args[0], args[1]);
            }
        }

        return null;
    }

    private static bool IsComplexObjectType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return !underlying.IsPrimitive
            && !underlying.IsEnum
            && underlying != typeof(string)
            && underlying != typeof(decimal)
            && underlying != typeof(DateTime)
            && underlying != typeof(DateTimeOffset)
            && underlying != typeof(DateOnly)
            && underlying != typeof(TimeOnly)
            && underlying != typeof(TimeSpan)
            && underlying != typeof(Guid)
            && underlying != typeof(Uri)
            && underlying != typeof(byte[])
            && !underlying.IsArray
            && GetEnumerableItemType(underlying) is null
            && GetDictionaryTypes(underlying) is null;
    }

    /// <summary>
    /// Determines whether a method's parameters can be represented as flat
    /// form fields for <c>application/x-www-form-urlencoded</c> input.
    /// Returns <see langword="true"/> when all parameters are primitive types
    /// or when a single non-polymorphic complex-object parameter has only
    /// primitive properties.
    /// </summary>
    private static bool CanRenderAsFormFields(SignalRMethodInfo method)
    {
        if (method.Parameters.All(p => !IsComplexObjectType(p.ParameterType)))
        {
            return true;
        }

        // A single flat object (all primitive/simple properties) can also
        // be rendered as form fields because its schema is flattened.
        // Polymorphic types are excluded — they get separate sub-endpoints.
        if (method.Parameters.Count == 1)
        {
            var paramType = method.Parameters[0].ParameterType;

            if (IsComplexObjectType(paramType)
                && HasOnlyFlatProperties(paramType)
                && paramType.GetCustomAttribute<JsonPolymorphicAttribute>() is null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> when every public settable property
    /// on <paramref name="type"/> is a primitive/simple type that SwaggerUI
    /// can render as an individual form field.
    /// </summary>
    private static bool HasOnlyFlatProperties(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        var properties = underlying.GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        return properties.Length > 0
            && properties
                .Where(p => p.CanWrite)
                .All(p => !IsComplexObjectType(p.PropertyType));
    }

    private static Microsoft.OpenApi.Any.IOpenApiAny ParseJsonToOpenApiAny(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return ConvertJsonElement(doc.RootElement);
    }

    private static Microsoft.OpenApi.Any.IOpenApiAny ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonObject(element),
            JsonValueKind.Array => ConvertJsonArray(element),
            JsonValueKind.String => new Microsoft.OpenApi.Any.OpenApiString(element.GetString() ?? string.Empty),
            JsonValueKind.Number when element.TryGetInt32(out var intVal) => new Microsoft.OpenApi.Any.OpenApiInteger(intVal),
            JsonValueKind.Number when element.TryGetInt64(out var longVal) => new Microsoft.OpenApi.Any.OpenApiLong(longVal),
            JsonValueKind.Number => new Microsoft.OpenApi.Any.OpenApiDouble(element.GetDouble()),
            JsonValueKind.True => new Microsoft.OpenApi.Any.OpenApiBoolean(true),
            JsonValueKind.False => new Microsoft.OpenApi.Any.OpenApiBoolean(false),
            JsonValueKind.Null => new Microsoft.OpenApi.Any.OpenApiNull(),
            _ => new Microsoft.OpenApi.Any.OpenApiString(element.GetRawText()),
        };
    }

    private static Microsoft.OpenApi.Any.OpenApiObject ConvertJsonObject(JsonElement element)
    {
        var obj = new Microsoft.OpenApi.Any.OpenApiObject();
        using var enumerator = element.EnumerateObject();
        while (enumerator.MoveNext())
        {
            obj[enumerator.Current.Name] = ConvertJsonElement(enumerator.Current.Value);
        }

        return obj;
    }

    private static Microsoft.OpenApi.Any.OpenApiArray ConvertJsonArray(JsonElement element)
    {
        var arr = new Microsoft.OpenApi.Any.OpenApiArray();
        using var enumerator = element.EnumerateArray();
        while (enumerator.MoveNext())
        {
            arr.Add(ConvertJsonElement(enumerator.Current));
        }

        return arr;
    }

    private string ConvertPropertyName(string value)
    {
        var policy = this.options.JsonSerializerOptions.PropertyNamingPolicy;
        if (policy is null)
        {
            return value;
        }

        return policy.ConvertName(value);
    }

    private Microsoft.OpenApi.Any.IOpenApiAny ConvertToOpenApiAny(object value)
    {
        var simple = CreateOpenApiAnyValue(value);
        if (simple is not null)
        {
            return simple;
        }

        // For complex types, serialize to JSON and parse into OpenApiAny structure
        var json = JsonSerializer.Serialize(value, this.options.JsonSerializerOptions);

        return ParseJsonToOpenApiAny(json);
    }

    private OpenApiSchema CreateSchemaForType(Type type)
    {
        // Handle nullable
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType is not null)
        {
            var innerSchema = CreateSchemaForType(underlyingType);
            innerSchema.Nullable = true;
            return innerSchema;
        }

        // Handle polymorphic types
        var polymorphicAttr = type.GetCustomAttribute<JsonPolymorphicAttribute>();
        if (polymorphicAttr is not null)
        {
            return CreatePolymorphicSchema(type, polymorphicAttr);
        }

        // Primitives
        if (type == typeof(string))
        {
            return new OpenApiSchema { Type = "string" };
        }

        if (type == typeof(bool))
        {
            return new OpenApiSchema { Type = "boolean" };
        }

        if (type == typeof(int) || type == typeof(short) || type == typeof(byte))
        {
            return new OpenApiSchema { Type = "integer", Format = "int32" };
        }

        if (type == typeof(long))
        {
            return new OpenApiSchema { Type = "integer", Format = "int64" };
        }

        if (type == typeof(float))
        {
            return new OpenApiSchema { Type = "number", Format = "float" };
        }

        if (type == typeof(double))
        {
            return new OpenApiSchema { Type = "number", Format = "double" };
        }

        if (type == typeof(decimal))
        {
            return new OpenApiSchema { Type = "number", Format = "decimal" };
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return new OpenApiSchema { Type = "string", Format = "date-time" };
        }

        if (type == typeof(DateOnly))
        {
            return new OpenApiSchema { Type = "string", Format = "date" };
        }

        if (type == typeof(TimeOnly) || type == typeof(TimeSpan))
        {
            return new OpenApiSchema { Type = "string", Format = "time" };
        }

        if (type == typeof(Guid))
        {
            return new OpenApiSchema { Type = "string", Format = "uuid" };
        }

        if (type == typeof(Uri))
        {
            return new OpenApiSchema { Type = "string", Format = "uri" };
        }

        if (type == typeof(byte[]))
        {
            return new OpenApiSchema { Type = "string", Format = "byte" };
        }

        if (type.IsEnum)
        {
            return CreateEnumSchema(type);
        }

        // Arrays / collections
        if (type.IsArray)
        {
            return new OpenApiSchema
            {
                Type = "array",
                Items = CreateSchemaForType(type.GetElementType()!),
            };
        }

        var enumerableType = GetEnumerableItemType(type);
        if (enumerableType is not null)
        {
            return new OpenApiSchema
            {
                Type = "array",
                Items = CreateSchemaForType(enumerableType),
            };
        }

        // Dictionary
        var dictionaryTypes = GetDictionaryTypes(type);
        if (dictionaryTypes is not null)
        {
            return new OpenApiSchema
            {
                Type = "object",
                AdditionalPropertiesAllowed = true,
                AdditionalProperties = CreateSchemaForType(dictionaryTypes.Value.ValueType),
            };
        }

        // Complex object — check for circular references
        if (this.schemaRegistry.TryGetValue(type, out var schemaName))
        {
            return new OpenApiSchema
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.Schema,
                    Id = schemaName,
                },
            };
        }

        return this.CreateObjectSchema(type);
    }

    private OpenApiSchema CreateObjectSchema(Type type)
    {
        var typeName = type.Name;
        this.schemaRegistry[type] = typeName;

        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>(),
            Required = new HashSet<string>(),
        };

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var prop in properties)
        {
            if (prop.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
            {
                continue;
            }

            var propName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? this.ConvertPropertyName(prop.Name);

            var propSchema = this.CreateSchemaForType(prop.PropertyType);
            propSchema.Description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description;
            ApplyDataAnnotations(propSchema, prop);

            schema.Properties[propName] = propSchema;

            if (prop.GetCustomAttribute<RequiredAttribute>() is not null
                || prop.GetCustomAttribute<JsonRequiredAttribute>() is not null)
            {
                schema.Required.Add(propName);
            }
        }

        this.ApplySchemaProcessors(schema, type);

        if (this.currentDocument is not null)
        {
            this.currentDocument.Components.Schemas[typeName] = schema;
        }

        return schema;
    }

    private void ApplySchemaProcessors(OpenApiSchema schema, Type type)
    {
        foreach (var processor in this.schemaProcessors)
        {
            processor.ProcessSchema(schema, type);
        }
    }

    private OpenApiSchema CreateEnumSchema(Type enumType)
    {
        if (this.ShouldSerializeEnumAsString(enumType))
        {
            var enumValues = new List<Microsoft.OpenApi.Any.IOpenApiAny>();
            foreach (var value in Enum.GetValues(enumType))
            {
                var json = JsonSerializer.Serialize(value, enumType, this.options.JsonSerializerOptions);
                var name = json.Trim('"');
                enumValues.Add(new Microsoft.OpenApi.Any.OpenApiString(name));
            }

            return new OpenApiSchema
            {
                Type = "string",
                Enum = enumValues,
            };
        }

        var intValues = new List<Microsoft.OpenApi.Any.IOpenApiAny>();
        foreach (var value in Enum.GetValues(enumType))
        {
            intValues.Add(new Microsoft.OpenApi.Any.OpenApiInteger(Convert.ToInt32(value)));
        }

        return new OpenApiSchema
        {
            Type = "integer",
            Enum = intValues,
        };
    }

    /// <summary>
    /// Determines whether the given enum type should be serialized as a string
    /// based on the configured <see cref="JsonSerializerOptions"/> converters
    /// and type-level <see cref="JsonConverterAttribute"/>.
    /// </summary>
    private bool ShouldSerializeEnumAsString(Type enumType)
    {
        var values = Enum.GetValues(enumType);
        if (values.Length == 0)
        {
            return false;
        }

        try
        {
            var json = JsonSerializer.Serialize(values.GetValue(0), enumType, this.options.JsonSerializerOptions);
            return json.Length > 0 && json[0] == '"';
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private OpenApiSchema CreatePolymorphicSchema(Type type, JsonPolymorphicAttribute polymorphicAttr)
    {
        var derivedTypes = type.GetCustomAttributes<JsonDerivedTypeAttribute>();
        var schemas = new List<OpenApiSchema>();
        var mapping = new Dictionary<string, string>();
        var discriminatorPropertyName = polymorphicAttr.TypeDiscriminatorPropertyName ?? "$type";

        foreach (var derived in derivedTypes)
        {
            var schemaName = derived.DerivedType.Name;
            var derivedSchema = (OpenApiSchema?)null;
            if (this.schemaRegistry.ContainsKey(derived.DerivedType))
            {
                this.currentDocument?.Components.Schemas.TryGetValue(schemaName, out derivedSchema);
            }
            else
            {
                derivedSchema = CreateSchemaForType(derived.DerivedType);
            }

            if (derivedSchema is null)
            {
                continue;
            }

            var discriminatorValue = derived.TypeDiscriminator switch
            {
                string s => s,
                int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => null,
            };

            if (discriminatorValue is not null)
            {
                derivedSchema.Properties ??= new Dictionary<string, OpenApiSchema>();

                // Preserve ReadOnly if the discriminator property was already set
                // (e.g., by AddPolymorphicSubEndpoints for form schemas).
                var preserveReadOnly = derivedSchema.Properties.TryGetValue(discriminatorPropertyName, out var existing)
                    && existing.ReadOnly;

                derivedSchema.Properties[discriminatorPropertyName] = new OpenApiSchema
                {
                    Type = derived.TypeDiscriminator is string ? "string" : "integer",
                    Enum = derived.TypeDiscriminator is string
                        ? [new Microsoft.OpenApi.Any.OpenApiString(discriminatorValue)]
                        : [new Microsoft.OpenApi.Any.OpenApiInteger(int.Parse(discriminatorValue, System.Globalization.CultureInfo.InvariantCulture))],
                    ReadOnly = preserveReadOnly,
                };

                mapping[discriminatorValue] = $"#/components/schemas/{schemaName}";
            }

            derivedSchema.Title = schemaName;

            // Register in components/schemas for $ref support.
            if (this.currentDocument is not null)
            {
                this.currentDocument.Components.Schemas[schemaName] = derivedSchema;
            }

            schemas.Add(new OpenApiSchema
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.Schema,
                    Id = schemaName,
                },
            });
        }

        if (schemas.Count == 0)
        {
            return CreateObjectSchema(type);
        }

        return new OpenApiSchema
        {
            OneOf = schemas,
            Discriminator = new OpenApiDiscriminator
            {
                PropertyName = discriminatorPropertyName,
                Mapping = mapping.Count > 0 ? mapping : null,
            },
        };
    }

    private void AddHubOperations(OpenApiDocument document, SignalRHubInfo hub)
    {
        foreach (var method in hub.Methods)
        {
            var pathKey = $"/hubs/{hub.Name}/{method.Name}";
            var operation = this.CreateOperation(hub, method);

            var pathItem = new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Post] = operation,
                },
            };

            document.Paths[pathKey] = pathItem;

            // For polymorphic parameters, generate a sub-endpoint per derived type
            // so each type gets its own flat form-friendly schema.
            this.AddPolymorphicSubEndpoints(document, hub, method, pathKey);
        }
    }

    private void AddPolymorphicSubEndpoints(
        OpenApiDocument document,
        SignalRHubInfo hub,
        SignalRMethodInfo method,
        string parentPathKey)
    {
        if (method.Parameters.Count != 1)
        {
            return;
        }

        var paramType = method.Parameters[0].ParameterType;
        var polymorphicAttr = paramType.GetCustomAttribute<JsonPolymorphicAttribute>();
        if (polymorphicAttr is null)
        {
            return;
        }

        var derivedTypes = paramType.GetCustomAttributes<JsonDerivedTypeAttribute>().ToList();
        if (derivedTypes.Count == 0)
        {
            return;
        }

        var discriminatorPropertyName = polymorphicAttr.TypeDiscriminatorPropertyName ?? "$type";

        foreach (var derived in derivedTypes)
        {
            var discriminatorValue = derived.TypeDiscriminator switch
            {
                string s => s,
                int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => null,
            };

            if (discriminatorValue is null)
            {
                continue;
            }

            var subPathKey = $"{parentPathKey}/{discriminatorValue}";
            var derivedSchema = this.CreateObjectSchema(derived.DerivedType);

            // Build the discriminator schema entry.
            var discriminatorSchema = new OpenApiSchema
            {
                Type = derived.TypeDiscriminator is string ? "string" : "integer",
                Enum = derived.TypeDiscriminator is string
                    ? [new Microsoft.OpenApi.Any.OpenApiString(discriminatorValue)]
                    : [new Microsoft.OpenApi.Any.OpenApiInteger(int.Parse(discriminatorValue, System.Globalization.CultureInfo.InvariantCulture))],
                Default = derived.TypeDiscriminator is string
                    ? new Microsoft.OpenApi.Any.OpenApiString(discriminatorValue)
                    : new Microsoft.OpenApi.Any.OpenApiInteger(int.Parse(discriminatorValue, System.Globalization.CultureInfo.InvariantCulture)),
            };

            // When IncludeDiscriminatorInExamples is false, hide from all examples.
            if (!this.options.IncludeDiscriminatorInExamples)
            {
                discriminatorSchema.ReadOnly = true;
            }

            derivedSchema.Properties ??= new Dictionary<string, OpenApiSchema>();
            derivedSchema.Properties[discriminatorPropertyName] = discriminatorSchema;

            var jsonMediaType = new OpenApiMediaType { Schema = derivedSchema };
            this.ApplyRequestExamplesForDerivedType(jsonMediaType, method, derived.DerivedType);

            var content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = jsonMediaType,
            };

            // Offer form-urlencoded when all properties are flat.
            // Use a separate schema that hides the discriminator from form inputs.
            // Build inline to avoid overwriting the component schema entry.
            if (HasOnlyFlatProperties(derived.DerivedType))
            {
                var formSchema = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>(derivedSchema.Properties ?? new Dictionary<string, OpenApiSchema>()),
                    Required = derivedSchema.Required is not null ? new HashSet<string>(derivedSchema.Required) : new HashSet<string>(),
                };

                formSchema.Properties[discriminatorPropertyName] = new OpenApiSchema
                {
                    Type = discriminatorSchema.Type,
                    Enum = discriminatorSchema.Enum,
                    Default = discriminatorSchema.Default,
                    ReadOnly = true,
                };

                this.ApplyPropertyExamplesForDerivedType(formSchema, method, derived.DerivedType);

                content["application/x-www-form-urlencoded"] = new OpenApiMediaType
                {
                    Schema = formSchema,
                };
            }

            var subOperation = new OpenApiOperation
            {
                Tags = method.Tags.Select(t => new OpenApiTag { Name = t }).ToList(),
                Summary = $"{method.Summary ?? method.Name} ({derived.DerivedType.Name})",
                Description = method.Description,
                OperationId = $"{method.OperationId}_{discriminatorValue}",
                Deprecated = method.IsDeprecated,
                Responses = this.CreateResponses(method),
                RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Content = content,
                },
            };

            AddSignalRExtension(
                subOperation,
                hub.Name,
                method.Name,
                hubPath: hub.Path,
                isStream: method.IsStreamingResponse,
                isClientEvent: false,
                parameterCount: 1,
                flattenedBody: true,
                discriminatorProperty: discriminatorPropertyName,
                discriminatorValue: discriminatorValue);

            document.Paths[subPathKey] = new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Post] = subOperation,
                },
            };
        }
    }

    private void AddClientEventOperations(OpenApiDocument document, SignalRHubInfo hub)
    {
        foreach (var clientEvent in hub.ClientEvents)
        {
            var pathKey = $"/hubs/{hub.Name}/events/{clientEvent.Name}";

            var operation = new OpenApiOperation
            {
                Tags = [new OpenApiTag { Name = $"{hub.Name} Events" }],
                Summary = clientEvent.Summary ?? $"Client event: {clientEvent.Name}",
                Description = clientEvent.Description ?? "Server-to-client callback. Subscribe to this event to receive notifications.",
                OperationId = $"{hub.Name}_Event_{clientEvent.Name}",
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse
                    {
                        Description = "Event payload received from the server.",
                    },
                },
            };

            var paramNames = clientEvent.Parameters.Select(p => p.Name).ToList();
            AddSignalRExtension(operation, hub.Name, clientEvent.Name, hubPath: hub.Path, isClientEvent: true, parameterNames: paramNames);

            // Add discriminator inference metadata for polymorphic parameters.
            // SignalR's JsonHubProtocol serializes with the runtime type, which
            // omits the discriminator. The JS plugin uses this metadata to infer
            // and inject the discriminator value from the received properties.
            this.AddEventDiscriminators(operation, clientEvent.Parameters);

            if (clientEvent.Parameters.Count > 0)
            {
                var schema = this.CreateParametersSchema(clientEvent.Parameters, document);
                operation.Responses["200"].Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = schema,
                    },
                };
            }

            var pathItem = new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Get] = operation,
                },
            };

            document.Paths[pathKey] = pathItem;
        }
    }

    private void AddEventDiscriminators(OpenApiOperation operation, IReadOnlyList<SignalRParameterInfo> parameters)
    {
        Microsoft.OpenApi.Any.OpenApiObject? discriminators = null;

        foreach (var param in parameters)
        {
            var polyAttr = param.ParameterType.GetCustomAttribute<JsonPolymorphicAttribute>();
            if (polyAttr is null)
            {
                continue;
            }

            var discriminatorProp = polyAttr.TypeDiscriminatorPropertyName ?? "$type";
            var derivedTypes = param.ParameterType.GetCustomAttributes<JsonDerivedTypeAttribute>();

            var mapping = new Microsoft.OpenApi.Any.OpenApiObject();
            foreach (var derived in derivedTypes)
            {
                var discriminatorValue = derived.TypeDiscriminator?.ToString();
                if (discriminatorValue is null)
                {
                    continue;
                }

                var props = new Microsoft.OpenApi.Any.OpenApiArray();
                foreach (var prop in derived.DerivedType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    props.Add(new Microsoft.OpenApi.Any.OpenApiString(this.ConvertPropertyName(prop.Name)));
                }

                mapping[discriminatorValue] = props;
            }

            if (mapping.Count > 0)
            {
                discriminators ??= new Microsoft.OpenApi.Any.OpenApiObject();
                discriminators[param.Name] = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["property"] = new Microsoft.OpenApi.Any.OpenApiString(discriminatorProp),
                    ["mapping"] = mapping,
                };
            }
        }

        if (discriminators is not null)
        {
            var ext = (Microsoft.OpenApi.Any.OpenApiObject)operation.Extensions["x-signalr"];
            ext["eventDiscriminators"] = discriminators;
        }
    }

    private OpenApiOperation CreateOperation(SignalRHubInfo hub, SignalRMethodInfo method)
    {
        var operation = new OpenApiOperation
        {
            Tags = method.Tags.Select(t => new OpenApiTag { Name = t }).ToList(),
            Summary = method.Summary,
            Description = method.Description,
            OperationId = method.OperationId,
            Deprecated = method.IsDeprecated,
            Responses = this.CreateResponses(method),
        };

        if (method.Parameters.Count > 0)
        {
            var requestSchema = this.CreateRequestBodySchema(method);
            var mediaType = new OpenApiMediaType
            {
                Schema = requestSchema,
            };

            this.ApplyRequestExamples(mediaType, method);

            var content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = mediaType,
            };

            // Offer form-urlencoded when the resulting schema has only
            // primitive/flat fields that SwaggerUI can render as form inputs.
            if (CanRenderAsFormFields(method))
            {
                content["application/x-www-form-urlencoded"] = new OpenApiMediaType
                {
                    Schema = requestSchema,
                };

                this.ApplyPropertyExamplesFromProviders(requestSchema, method);
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = method.Parameters.Any(p => p.IsRequired),
                Content = content,
            };
        }

        // Security
        if (method.RequiresAuthorization || (hub.RequiresAuthorization && !method.AllowAnonymous))
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    }
                    ] = [],
                },
            ];
        }

        var isFlattened = method.Parameters.Count == 1
            && IsComplexObjectType(method.Parameters[0].ParameterType);

        AddSignalRExtension(
            operation,
            hub.Name,
            method.Name,
            hubPath: hub.Path,
            isStream: method.IsStreamingResponse,
            isClientEvent: false,
            parameterCount: method.Parameters.Count,
            flattenedBody: isFlattened);

        return operation;
    }

    private OpenApiSchema CreateRequestBodySchema(SignalRMethodInfo method)
    {
        // When there is exactly one parameter and it is a complex object type,
        // flatten it directly as the request body schema (no wrapper property).
        if (method.Parameters.Count == 1 && IsComplexObjectType(method.Parameters[0].ParameterType))
        {
            var param = method.Parameters[0];
            var schema = CreateSchemaForType(param.ParameterType);
            schema.Description ??= param.Description;
            return schema;
        }

        var properties = new Dictionary<string, OpenApiSchema>();
        var requiredProperties = new HashSet<string>();

        foreach (var param in method.Parameters)
        {
            var paramSchema = CreateSchemaForType(param.ParameterType);
            paramSchema.Description = param.Description;
            ApplyDataAnnotations(paramSchema, param.ParameterInfo);

            if (param.HasDefaultValue && param.DefaultValue is not null)
            {
                paramSchema.Default = CreateOpenApiAnyValue(param.DefaultValue);
            }

            properties[param.Name] = paramSchema;

            if (param.IsRequired)
            {
                requiredProperties.Add(param.Name);
            }
        }

        return new OpenApiSchema
        {
            Type = "object",
            Properties = properties,
            Required = requiredProperties,
        };
    }

    private OpenApiResponses CreateResponses(SignalRMethodInfo method)
    {
        var responses = new OpenApiResponses();

        if (method.ReturnType is null)
        {
            responses["204"] = new OpenApiResponse
            {
                Description = method.ReturnDescription ?? "No Content",
            };
        }
        else
        {
            var returnSchema = CreateSchemaForType(method.ReturnType);
            var contentType = method.ProducesContentTypes.Count > 0
                ? method.ProducesContentTypes[0]
                : "application/json";

            var description = method.ReturnDescription ?? "Success.";
            if (method.IsStreamingResponse)
            {
                description = method.ReturnDescription ?? "Stream of items. Each item is delivered as it becomes available.";
                returnSchema = new OpenApiSchema
                {
                    Type = "array",
                    Items = returnSchema,
                    Description = $"Stream of {method.StreamItemType?.Name ?? "items"}",
                };
            }

            var mediaType = new OpenApiMediaType
            {
                Schema = returnSchema,
            };

            this.ApplyResponseExamples(mediaType, method);

            responses["200"] = new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [contentType] = mediaType,
                },
            };
        }

        return responses;
    }

    private OpenApiSchema CreateParametersSchema(
        IReadOnlyList<SignalRParameterInfo> parameters,
        OpenApiDocument document)
    {
        var properties = new Dictionary<string, OpenApiSchema>();

        foreach (var param in parameters)
        {
            properties[param.Name] = CreateSchemaForType(param.ParameterType);
        }

        return new OpenApiSchema
        {
            Type = "object",
            Properties = properties,
        };
    }

    private void ApplyRequestExamples(OpenApiMediaType mediaType, SignalRMethodInfo method)
    {
        if (method.RequestExampleProviderTypes.Count == 0)
        {
            return;
        }

        var examples = new Dictionary<string, OpenApiExample>();

        foreach (var providerType in method.RequestExampleProviderTypes)
        {
            var resolvedExamples = this.ResolveExamples(providerType);
            foreach (var example in resolvedExamples)
            {
                examples[example.Key] = example.Value;
            }
        }

        if (examples.Count > 0)
        {
            mediaType.Examples = examples;
        }
    }

    private void ApplyRequestExamplesForDerivedType(OpenApiMediaType mediaType, SignalRMethodInfo method, Type derivedType)
    {
        if (method.RequestExampleProviderTypes.Count == 0)
        {
            return;
        }

        var examples = new Dictionary<string, OpenApiExample>();

        foreach (var providerType in method.RequestExampleProviderTypes)
        {
            var resolvedExamples = this.ResolveExamplesForDerivedType(providerType, derivedType);
            foreach (var example in resolvedExamples)
            {
                examples[example.Key] = example.Value;
            }
        }

        if (examples.Count > 0)
        {
            mediaType.Examples = examples;
        }
    }

    private void ApplyResponseExamples(OpenApiMediaType mediaType, SignalRMethodInfo method)
    {
        if (method.ResponseExampleProviderTypes.Count == 0)
        {
            return;
        }

        var examples = new Dictionary<string, OpenApiExample>();

        foreach (var providerType in method.ResponseExampleProviderTypes)
        {
            var resolvedExamples = this.ResolveExamples(providerType);
            foreach (var example in resolvedExamples)
            {
                examples[example.Key] = example.Value;
            }
        }

        if (examples.Count > 0)
        {
            mediaType.Examples = examples;
        }
    }

    private void ApplyPropertyExamplesFromProviders(OpenApiSchema schema, SignalRMethodInfo method)
    {
        var value = this.ResolveFirstExampleValue(method.RequestExampleProviderTypes);
        if (value is not null)
        {
            this.ApplyPropertyExamples(schema, value);
        }
    }

    private void ApplyPropertyExamplesForDerivedType(OpenApiSchema schema, SignalRMethodInfo method, Type derivedType)
    {
        var value = this.ResolveFirstExampleValueForDerivedType(method.RequestExampleProviderTypes, derivedType);
        if (value is not null)
        {
            this.ApplyPropertyExamples(schema, value);
        }
    }

    private void ApplyPropertyExamples(OpenApiSchema schema, object exampleValue)
    {
        var json = JsonSerializer.Serialize(exampleValue, this.options.JsonSerializerOptions);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object || schema.Properties is null)
        {
            return;
        }

        using var enumerator = doc.RootElement.EnumerateObject();
        while (enumerator.MoveNext())
        {
            if (schema.Properties.TryGetValue(enumerator.Current.Name, out var propSchema)
                && !propSchema.ReadOnly)
            {
                propSchema.Example = ConvertJsonElement(enumerator.Current.Value);
            }
        }
    }

    private Dictionary<string, OpenApiExample> ResolveExamples(Type providerType)
    {
        var result = new Dictionary<string, OpenApiExample>();

        // Create the provider using ActivatorUtilities, which resolves constructor
        // dependencies from DI while allowing types that are not registered themselves.
        object provider;
        try
        {
            provider = ActivatorUtilities.CreateInstance(this.serviceProvider, providerType);
        }
        catch (Exception)
        {
            return result;
        }

        // Find the ISignalROpenApiExamplesProvider<T> interface on the provider type
        var providerInterface = providerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(ISignalROpenApiExamplesProvider<>));

        if (providerInterface is null)
        {
            return result;
        }

        // Invoke GetExamples() via reflection
        var getExamplesMethod = providerInterface.GetMethod(nameof(ISignalROpenApiExamplesProvider<object>.GetExamples));
        if (getExamplesMethod is null)
        {
            return result;
        }

        var examples = getExamplesMethod.Invoke(provider, null);
        if (examples is null)
        {
            return result;
        }

        // Iterate through the IEnumerable<SignalROpenApiExample<T>>
        foreach (var exampleObj in (System.Collections.IEnumerable)examples)
        {
            var exampleType = exampleObj.GetType();
            var nameProperty = exampleType.GetProperty(nameof(SignalROpenApiExample<object>.Name));
            var summaryProperty = exampleType.GetProperty(nameof(SignalROpenApiExample<object>.Summary));
            var descriptionProperty = exampleType.GetProperty(nameof(SignalROpenApiExample<object>.Description));
            var valueProperty = exampleType.GetProperty(nameof(SignalROpenApiExample<object>.Value));

            var name = nameProperty?.GetValue(exampleObj) as string ?? "example";
            var summary = summaryProperty?.GetValue(exampleObj) as string;
            var description = descriptionProperty?.GetValue(exampleObj) as string;
            var value = valueProperty?.GetValue(exampleObj);

            var openApiExample = new OpenApiExample
            {
                Summary = summary,
                Description = description,
            };

            if (value is not null)
            {
                openApiExample.Value = ConvertToOpenApiAny(value);
            }

            result[name] = openApiExample;
        }

        return result;
    }

    private Dictionary<string, OpenApiExample> ResolveExamplesForDerivedType(Type providerType, Type derivedType)
    {
        var result = new Dictionary<string, OpenApiExample>();

        object provider;
        try
        {
            provider = ActivatorUtilities.CreateInstance(this.serviceProvider, providerType);
        }
        catch (Exception)
        {
            return result;
        }

        var providerInterface = providerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(ISignalROpenApiExamplesProvider<>));

        if (providerInterface is null)
        {
            return result;
        }

        var getExamplesMethod = providerInterface.GetMethod(nameof(ISignalROpenApiExamplesProvider<object>.GetExamples));
        if (getExamplesMethod is null)
        {
            return result;
        }

        var examples = getExamplesMethod.Invoke(provider, null);
        if (examples is null)
        {
            return result;
        }

        foreach (var exampleObj in (System.Collections.IEnumerable)examples)
        {
            var exampleType = exampleObj.GetType();
            var valueProperty = exampleType.GetProperty(nameof(SignalROpenApiExample<object>.Value));
            var value = valueProperty?.GetValue(exampleObj);

            // Only include examples whose value matches the derived type.
            if (value is null || !derivedType.IsInstanceOfType(value))
            {
                continue;
            }

            var nameProperty = exampleType.GetProperty(nameof(SignalROpenApiExample<object>.Name));
            var summaryProperty = exampleType.GetProperty(nameof(SignalROpenApiExample<object>.Summary));
            var descriptionProperty = exampleType.GetProperty(nameof(SignalROpenApiExample<object>.Description));

            var name = nameProperty?.GetValue(exampleObj) as string ?? "example";
            var summary = summaryProperty?.GetValue(exampleObj) as string;
            var description = descriptionProperty?.GetValue(exampleObj) as string;

            result[name] = new OpenApiExample
            {
                Summary = summary,
                Description = description,
                Value = ConvertToOpenApiAny(value),
            };
        }

        return result;
    }

    private object? ResolveFirstExampleValue(IReadOnlyList<Type> providerTypes)
    {
        foreach (var providerType in providerTypes)
        {
            var value = this.GetFirstExampleValueFromProvider(providerType);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private object? ResolveFirstExampleValueForDerivedType(IReadOnlyList<Type> providerTypes, Type derivedType)
    {
        foreach (var providerType in providerTypes)
        {
            var value = this.GetFirstExampleValueFromProvider(providerType, derivedType);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private object? GetFirstExampleValueFromProvider(Type providerType, Type? filterType = null)
    {
        object provider;
        try
        {
            provider = ActivatorUtilities.CreateInstance(this.serviceProvider, providerType);
        }
        catch (Exception)
        {
            return null;
        }

        var providerInterface = providerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(ISignalROpenApiExamplesProvider<>));

        var getExamplesMethod = providerInterface?.GetMethod(nameof(ISignalROpenApiExamplesProvider<object>.GetExamples));
        var examples = getExamplesMethod?.Invoke(provider, null);
        if (examples is null)
        {
            return null;
        }

        foreach (var exampleObj in (System.Collections.IEnumerable)examples)
        {
            var valueProperty = exampleObj.GetType().GetProperty(nameof(SignalROpenApiExample<object>.Value));
            var value = valueProperty?.GetValue(exampleObj);

            if (value is null)
            {
                continue;
            }

            if (filterType is null || filterType.IsInstanceOfType(value))
            {
                return value;
            }
        }

        return null;
    }
}
