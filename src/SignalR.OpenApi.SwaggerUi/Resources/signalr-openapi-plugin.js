// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.
// SignalR OpenAPI SwaggerUI Plugin
// Intercepts SwaggerUI operations marked with x-signalr and redirects
// execution to use the SignalR protocol via @microsoft/signalr.
"use strict";

var SignalROpenApiPlugin = function (system) {

  // Active hub connections keyed by hub path (e.g. "/hubs/chat")
  var _hubs = {};

  // Active stream subscriptions keyed by operation path (e.g. "/hubs/Chat/Countdown")
  var _streams = {};

  // Stream item history keyed by operation path
  var _streamItems = {};

  // Client event logs keyed by hub path
  var _eventLogs = {};

  // Event log subscribers (components that need re-render on new events)
  var _eventListeners = {};

  // Get the Bearer token from the SwaggerUI authorize dialog
  var _getAccessToken = function () {
    var state = system.getState().get("auth").get("authorized");
    if (!state) {
      return undefined;
    }

    var authState = state.toJS();
    var keys = Object.keys(authState);
    if (keys.length === 0) {
      return undefined;
    }

    var entry = authState[keys[0]];
    if (entry && entry.schema && entry.schema.type === "http" && entry.schema.scheme === "basic") {
      return btoa(entry.value.username + ":" + entry.value.password);
    }

    return entry ? entry.value : undefined;
  };

  // Subscribe to all client events on a hub connection
  var _subscribeClientEvents = function (hubPath, hub) {
    var specJson = system.specSelectors.specJson().toJS();
    var paths = specJson.paths || {};

    Object.keys(paths).forEach(function (path) {
      var getOp = paths[path] && paths[path].get;
      if (!getOp) {
        return;
      }

      var ext = getOp["x-signalr"];
      if (!ext || !ext.clientEvent) {
        return;
      }

      var extHubPath = ext.hubPath || ("/" + ext.hub.toLowerCase());
      if (extHubPath !== hubPath) {
        return;
      }

      var eventName = ext.method;
      var paramNames = ext.parameterNames || [];
      var eventDiscriminators = ext.eventDiscriminators || {};
      hub.on(eventName, function () {
        var args = Array.prototype.slice.call(arguments);

        // Map positional args to named parameters when names are available.
        var payload;
        if (paramNames.length > 0 && args.length > 0) {
          payload = {};
          for (var pi = 0; pi < args.length; pi++) {
            var key = pi < paramNames.length ? paramNames[pi] : "arg" + pi;
            payload[key] = args[pi];
          }
        } else {
          payload = args.length === 1 ? args[0] : args;
        }

        // Inject discriminator values for polymorphic parameters.
        // SignalR serializes with the runtime type, omitting the discriminator.
        // Use the property mapping from x-signalr to infer the derived type.
        // Matching is case-insensitive because SignalR and OpenAPI may use
        // different naming policies (e.g. camelCase vs PascalCase).
        if (payload && typeof payload === "object" && !Array.isArray(payload)) {
          try {
            var discKeys = Object.keys(eventDiscriminators);
            for (var di = 0; di < discKeys.length; di++) {
              var paramKey = discKeys[di];
              var paramVal = payload[paramKey];
              if (!paramVal || typeof paramVal !== "object") {
                continue;
              }

              var discInfo = eventDiscriminators[paramKey];

              // Build a lowercase lookup of the received object's keys.
              var valKeysOrig = Object.keys(paramVal);
              var valKeysLower = {};
              for (var ki = 0; ki < valKeysOrig.length; ki++) {
                valKeysLower[valKeysOrig[ki].toLowerCase()] = true;
              }

              // Skip if the discriminator is already present.
              if (valKeysLower[discInfo.property.toLowerCase()]) {
                continue;
              }

              var mappingKeys = Object.keys(discInfo.mapping);
              var bestMatch = null;
              var bestScore = -1;
              for (var mi = 0; mi < mappingKeys.length; mi++) {
                var candidateProps = discInfo.mapping[mappingKeys[mi]];
                var score = 0;
                for (var ci = 0; ci < candidateProps.length; ci++) {
                  if (valKeysLower[candidateProps[ci].toLowerCase()]) {
                    score++;
                  }
                }

                // Prefer the candidate whose properties are the closest match.
                if (score > bestScore || (score === bestScore && bestMatch !== null && candidateProps.length < discInfo.mapping[bestMatch].length)) {
                  bestScore = score;
                  bestMatch = mappingKeys[mi];
                }
              }

              if (bestMatch !== null && bestScore > 0) {
                // Inject discriminator as the first property for clarity.
                var reordered = {};
                reordered[discInfo.property] = bestMatch;
                for (var vi = 0; vi < valKeysOrig.length; vi++) {
                  reordered[valKeysOrig[vi]] = paramVal[valKeysOrig[vi]];
                }

                payload[paramKey] = reordered;
              }
            }
          } catch (e) {
            console.error("[SignalR OpenAPI] Discriminator inference error:", e);
          }
        }

        var entry = {
          timestamp: new Date().toISOString(),
          event: eventName,
          payload: payload,
        };

        if (!_eventLogs[hubPath]) {
          _eventLogs[hubPath] = [];
        }

        _eventLogs[hubPath].push(entry);

        // Notify listeners
        if (_eventListeners[hubPath]) {
          _eventListeners[hubPath].forEach(function (fn) { fn(); });
        }
      });
    });
  };

  // Get or create a HubConnection for the given hub path
  var _getOrCreateHub = function (hubPath) {
    if (_hubs[hubPath] && _hubs[hubPath].state === signalR.HubConnectionState.Connected) {
      return Promise.resolve(_hubs[hubPath]);
    }

    var token = _getAccessToken();
    var options = {};
    if (token) {
      options.accessTokenFactory = function () { return token; };
    }

    var connection = new signalR.HubConnectionBuilder()
      .withUrl(hubPath, options)
      .withAutomaticReconnect()
      .build();

    _hubs[hubPath] = connection;

    // Track connection state changes for UI updates
    connection.onreconnecting(function () {
      if (_eventListeners[hubPath]) {
        _eventListeners[hubPath].forEach(function (fn) { fn(); });
      }
    });

    connection.onreconnected(function () {
      if (_eventListeners[hubPath]) {
        _eventListeners[hubPath].forEach(function (fn) { fn(); });
      }
    });

    connection.onclose(function () {
      if (_eventListeners[hubPath]) {
        _eventListeners[hubPath].forEach(function (fn) { fn(); });
      }
    });

    // Subscribe to client events once connected
    _subscribeClientEvents(hubPath, connection);

    return connection.start().then(function () {
      return connection;
    });
  };

  // Parse the x-signalr extension from an operation
  var _getSignalRExtension = function (operation) {
    var extensions = operation.get("x-signalr");
    if (!extensions) {
      return null;
    }

    if (extensions.toJS) {
      return extensions.toJS();
    }

    return extensions;
  };

  // Check if a path is a SignalR operation (starts with /hubs/)
  var _isSignalRPath = function (path) {
    return path.indexOf("/hubs/") === 0;
  };

  // Extract the hub route from a path like /hubs/Chat/SendMessage
  var _getHubRoute = function (signalrExt) {
    return signalrExt.hubPath || ("/" + signalrExt.hub.toLowerCase());
  };

   // Parse request body from the SwaggerUI OAS3 state.
  // The executeRequest wrapper intercepts before the original action reads
  // the request body, so we must read it from the OAS3 selectors directly.
  // Handles both application/json (raw JSON textarea) and
  // application/x-www-form-urlencoded (individual form fields).
  var _parseRequestBody = function (pathName, method, parameterCount, flattenedBody) {
    var oas3Selectors = system.oas3Selectors;
    if (!oas3Selectors) {
      return [];
    }

    var requestBody = oas3Selectors.requestBodyValue(pathName, method);
    if (!requestBody) {
      return [];
    }

    // For application/json, requestBody is a raw JSON string from the textarea.
    // For application/x-www-form-urlencoded, it is an Immutable Map of field
    // names to objects like { value: "...", errors: [...] }.
    var body = requestBody;
    if (body.toJS) {
      body = body.toJS();
    }

    if (typeof body === "string") {
      try {
        body = JSON.parse(body);
      } catch (e) {
        return [body];
      }
    }

    // SwaggerUI stores form-urlencoded field values as wrapper objects:
    // { fieldName: { value: "val", errors: [] } }. Detect this format by
    // checking if every value is an object with a "value" property and
    // unwrap to extract the raw values.
    if (body && typeof body === "object" && !Array.isArray(body)) {
      var keys = Object.keys(body);
      var isWrapped = keys.length > 0 && keys.every(function (k) {
        var v = body[k];
        return v && typeof v === "object" && !Array.isArray(v) && "value" in v && "errors" in v;
      });

      if (isWrapped) {
        var unwrapped = {};
        for (var i = 0; i < keys.length; i++) {
          var rawValue = body[keys[i]].value;
          // Form fields are always strings. Coerce numeric strings to
          // numbers and boolean strings to booleans so the SignalR JSON
          // protocol can deserialize them correctly.
          if (typeof rawValue === "string") {
            if (rawValue === "true") {
              rawValue = true;
            } else if (rawValue === "false") {
              rawValue = false;
            } else if (rawValue !== "" && !isNaN(Number(rawValue))) {
              rawValue = Number(rawValue);
            }
          }

          unwrapped[keys[i]] = rawValue;
        }

        body = unwrapped;
      }
    }

    // When a single complex object parameter is flattened, pass the entire
    // body as a single argument rather than spreading its properties.
    if (flattenedBody && body && typeof body === "object" && !Array.isArray(body)) {
      return [body];
    }

    // Extract parameter values in order from the parsed object
    if (body && typeof body === "object" && !Array.isArray(body)) {
      return Object.values(body);
    }

    return Array.isArray(body) ? body : [body];
  };

  // Format the stream items array as response text
  var _formatStreamResponse = function (items, state) {
    var result = {
      state: state,
      count: items.length,
      items: items,
    };
    return JSON.stringify(result, null, 2);
  };

  // React component for client event log panel
  function SignalREventLog(props) {
    var React = system.React;
    var hubPath = props.hubPath;
    var eventName = props.eventName;

    var stateHook = React.useState(0);
    var forceUpdate = stateHook[1];

    var logs = (_eventLogs[hubPath] || []).filter(function (entry) {
      return entry.event === eventName;
    });

    var isConnected = _hubs[hubPath] && _hubs[hubPath].state === signalR.HubConnectionState.Connected;

    var connectAndListen = function () {
      _getOrCreateHub(hubPath).then(function () {
        forceUpdate(function (n) { return n + 1; });
      });
    };

    var clearLog = function () {
      if (_eventLogs[hubPath]) {
        _eventLogs[hubPath] = _eventLogs[hubPath].filter(function (entry) {
          return entry.event !== eventName;
        });
      }

      forceUpdate(function (n) { return n + 1; });
    };

    // Register for updates to trigger re-render
    React.useEffect(function () {
      var listener = function () { forceUpdate(function (n) { return n + 1; }); };

      if (!_eventListeners[hubPath]) {
        _eventListeners[hubPath] = [];
      }

      _eventListeners[hubPath].push(listener);

      return function () {
        var idx = _eventListeners[hubPath].indexOf(listener);
        if (idx >= 0) {
          _eventListeners[hubPath].splice(idx, 1);
        }
      };
    }, [hubPath]);

    return React.createElement("div", { className: "opblock-body" },
      React.createElement("div", { className: "signalr-event-panel", style: { padding: "10px 20px" } },
        React.createElement("div", { style: { display: "flex", alignItems: "center", marginBottom: "10px" } },
          React.createElement("span", {
            className: "signalr-status " + (isConnected ? "signalr-status--connected" : "signalr-status--disconnected"),
          }, isConnected ? "Connected" : "Disconnected"),
          !isConnected && React.createElement("button", {
            className: "btn",
            style: { marginLeft: "10px", fontSize: "12px", padding: "4px 10px" },
            onClick: connectAndListen,
          }, "Connect & Listen"),
          logs.length > 0 && React.createElement("button", {
            className: "btn",
            style: { marginLeft: "10px", fontSize: "12px", padding: "4px 10px" },
            onClick: clearLog,
          }, "Clear Log"),
          React.createElement("span", {
            style: { marginLeft: "10px", fontSize: "12px", color: "#888" },
          }, logs.length + " event(s) received")
        ),
        logs.length === 0
          ? React.createElement("p", {
              style: { color: "#888", fontStyle: "italic", fontSize: "13px" },
            }, isConnected
              ? "Listening for \"" + eventName + "\" events..."
              : "Connect to start receiving events.")
          : React.createElement("div", { className: "signalr-event-log" },
              logs.map(function (entry, i) {
                return React.createElement("div", {
                  key: i,
                  className: "signalr-event-entry",
                  style: {
                    borderBottom: "1px solid #e8e8e8",
                    padding: "6px 0",
                    fontFamily: "monospace",
                    fontSize: "12px",
                  },
                },
                  React.createElement("span", {
                    style: { color: "#888", marginRight: "8px" },
                  }, entry.timestamp.split("T")[1].replace("Z", "")),
                  React.createElement("pre", {
                    style: {
                      display: "inline",
                      background: "#f5f5f5",
                      padding: "2px 6px",
                      borderRadius: "3px",
                    },
                  }, JSON.stringify(entry.payload, null, 2))
                );
              })
            )
      )
    );
  }

  return {
    statePlugins: {
      spec: {
        wrapSelectors: {
          // Hide "Try it out" for client events (GET operations)
          allowTryItOutFor: function (ori) {
            return function (_, path, method) {
              if (_isSignalRPath(path) && method === "get") {
                return false;
              }
              return ori(_, path, method);
            };
          },
        },
        wrapActions: {
          // Intercept execute requests for SignalR operations
          executeRequest: function (oriAction, system) {
            return function (args) {
              var specActions = system.specActions;
              var pathName = args.pathName;
              var method = args.method;

              if (!_isSignalRPath(pathName)) {
                return oriAction(args);
              }

              // Get x-signalr extension data
              var operation = args.operation;
              var signalrExt = _getSignalRExtension(operation);
              if (!signalrExt) {
                return oriAction(args);
              }

              var hubPath = _getHubRoute(signalrExt);
              var methodName = signalrExt.method;
              var isStream = signalrExt.stream === true;
              var parameterCount = signalrExt.parameterCount || 0;
              var flattenedBody = signalrExt.flattenedBody === true;

              // Prevent re-execution while a stream is active
              if (isStream && _streams[pathName]) {
                return null;
              }

              // Parse parameters from request body (read from OAS3 selectors)
              var paramValues = _parseRequestBody(pathName, method, parameterCount, flattenedBody);

              // For polymorphic sub-endpoints, inject the discriminator value
              // into the body object so SignalR can deserialize the correct type.
              // System.Text.Json requires the discriminator property to appear
              // FIRST in the JSON object for polymorphic deserialization.
              if (signalrExt.discriminatorProperty && signalrExt.discriminatorValue) {
                if (paramValues.length === 1 && paramValues[0] && typeof paramValues[0] === "object") {
                  var original = paramValues[0];
                  var reordered = {};
                  reordered[signalrExt.discriminatorProperty] = signalrExt.discriminatorValue;
                  var originalKeys = Object.keys(original);
                  for (var ki = 0; ki < originalKeys.length; ki++) {
                    if (originalKeys[ki] !== signalrExt.discriminatorProperty) {
                      reordered[originalKeys[ki]] = original[originalKeys[ki]];
                    }
                  }

                  paramValues[0] = reordered;
                }
              }

              // Set the request in state so LiveResponse can render it.
              var requestInfo = { url: hubPath, method: isStream ? "STREAM" : "INVOKE" };
              specActions.setRequest(pathName, method, requestInfo);
              specActions.setMutatedRequest(pathName, method, requestInfo);

              // Helper to set the response in SwaggerUI state.
              var _setResponse = function (status, text) {
                specActions.setResponse(pathName, method, {
                  url: hubPath,
                  status: status,
                  headers: { "x-signalr-method": methodName },
                  text: text,
                });
              };

              // Connect and invoke
              _getOrCreateHub(hubPath)
                .then(function (hub) {
                  if (isStream) {
                    // Initialize stream history
                    _streamItems[pathName] = [];
                    _setResponse(200, _formatStreamResponse([], "streaming"));

                    var subscription = hub.stream.apply(hub, [methodName].concat(paramValues))
                      .subscribe({
                        next: function (item) {
                          _streamItems[pathName].push(item);
                          _setResponse(200, _formatStreamResponse(_streamItems[pathName], "streaming"));
                        },
                        complete: function () {
                          var items = _streamItems[pathName] || [];
                          _setResponse(200, _formatStreamResponse(items, "completed"));
                          delete _streams[pathName];
                        },
                        error: function (err) {
                          var items = _streamItems[pathName] || [];
                          _setResponse(500, _formatStreamResponse(items, "error: " + err.toString()));
                          delete _streams[pathName];
                        },
                      });

                    _streams[pathName] = subscription;
                  } else {
                    // Regular invoke
                    hub.invoke.apply(hub, [methodName].concat(paramValues))
                      .then(function (result) {
                        var status = result != null ? 200 : 204;
                        _setResponse(status, result != null ? JSON.stringify(result, null, 2) : "");
                      })
                      .catch(function (err) {
                        _setResponse(500, err.toString());
                      });
                  }
                })
                .catch(function (err) {
                  _setResponse(500, "Connection failed: " + err.toString());
                });

              return null;
            };
          },
          // Clean up stream subscriptions on clear
          clearRequest: function (oriAction, system) {
            return function (path, method) {
              if (_streams[path]) {
                _streams[path].dispose();
                delete _streams[path];
              }
              delete _streamItems[path];
              return oriAction(path, method);
            };
          },
        },
      },
    },

    wrapComponents: {
      // Change method labels for SignalR operations
      OperationSummary: function (Original, system) {
        return function (props) {
          var React = system.React;
          var path = props.operationProps.get("path");

          if (_isSignalRPath(path)) {
            var method = props.operationProps.get("method").toLowerCase();
            if (method === "get") {
              props.operationProps = props.operationProps.set("method", "event");
            } else {
              // Check if this is a streaming operation
              var specJson = system.specSelectors.specJson().toJS();
              var opSpec = specJson.paths && specJson.paths[path] && specJson.paths[path][method];
              var ext = opSpec && opSpec["x-signalr"];
              if (ext && ext.stream === true) {
                props.operationProps = props.operationProps.set("method", "stream");
              } else {
                props.operationProps = props.operationProps.set("method", "invoke");
              }
            }

            // Strip "Async" suffix from the display path if configured
            var configs = system.getConfigs ? system.getConfigs() : {};
            var stripAsync = configs.signalRStripAsyncSuffix !== false;
            if (stripAsync && path.match(/Async(\/[^\/]+)?$/)) {
              var displayPath = path.replace(/Async(\/[^\/]+)?$/, "$1");
              props.operationProps = props.operationProps.set("path", displayPath);
            }
          }

          return React.createElement(Original, props);
        };
      },
      // Hide curl command for SignalR operations
      curl: function (Original, system) {
        return function (props) {
          var React = system.React;
          var url = props.request ? props.request.get("url") : "";

          if (_isSignalRPath(url)) {
            return null;
          }

          return React.createElement(Original, props);
        };
      },
      // Hide "No parameters" message for SignalR operations while
      // preserving the "Try it out" button that lives in this component.
      parameters: function (Original, system) {
        return function (props) {
          var React = system.React;
          var pathMethod = props.pathMethod || [];
          var path = pathMethod.get ? pathMethod.get(0) : pathMethod[0] || "";

          if (_isSignalRPath(path)) {
            var params = props.parameters;
            var count = params ? (params.size != null ? params.size : params.length) : 0;
            if (count === 0) {
              return React.createElement("div", { className: "signalr-no-params" },
                React.createElement(Original, props)
              );
            }
          }

          return React.createElement(Original, props);
        };
      },
      // Replace the Execute button for streaming operations
      execute: function (Original, system) {
        return function (props) {
          var React = system.React;
          var specActions = system.specActions;
          var path = props.path;
          var method = props.method;

          if (!_isSignalRPath(path)) {
            return React.createElement(Original, props);
          }

          var isStreaming = !!_streams[path];

          if (isStreaming) {
            // Show "Stop Stream" button instead
            return React.createElement(
              "button",
              {
                className: "btn execute opblock-control__btn signalr-stop-stream",
                onClick: function () {
                  if (_streams[path]) {
                    _streams[path].dispose();
                    var items = _streamItems[path] || [];
                    delete _streams[path];
                    specActions.setResponse(path, method, {
                      url: path,
                      status: 200,
                      headers: { "x-signalr-stream": "cancelled" },
                      text: _formatStreamResponse(items, "cancelled"),
                    });
                  }
                },
              },
              "Stop Stream"
            );
          }

          // Render the original Execute button (it triggers executeRequest correctly)
          return React.createElement(Original, props);
        };
      },
      // Render client event log for GET/event operations
      responses: function (Original, system) {
        return function (props) {
          var React = system.React;
          var path = props.path;
          var method = props.method;

          // Only customize for SignalR client events (GET operations)
          if (!_isSignalRPath(path) || method !== "get") {
            return React.createElement(Original, props);
          }

          // Get the x-signalr extension
          var specJson = system.specSelectors.specJson().toJS();
          var opSpec = specJson.paths && specJson.paths[path] && specJson.paths[path][method];
          var ext = opSpec && opSpec["x-signalr"];
          if (!ext || !ext.clientEvent) {
            return React.createElement(Original, props);
          }

          var hubPath = ext.hubPath || ("/" + ext.hub.toLowerCase());
          var eventName = ext.method;

          // Create event log component
          return React.createElement(SignalREventLog, {
            hubPath: hubPath,
            eventName: eventName,
            path: path,
          });
        };
      },
    },
  };
};

// The plugin is registered via Swashbuckle's ConfigObject.Plugins,
// which resolves this global function name during SwaggerUIBundle initialization.
