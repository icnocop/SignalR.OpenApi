// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A test enum for verifying enum schema generation.
/// </summary>
public enum TestStatus
{
    /// <summary>Pending status.</summary>
    Pending = 0,

    /// <summary>Active status.</summary>
    Active = 1,

    /// <summary>Completed status.</summary>
    Completed = 2,

    /// <summary>Failed status.</summary>
    Failed = 3,
}
