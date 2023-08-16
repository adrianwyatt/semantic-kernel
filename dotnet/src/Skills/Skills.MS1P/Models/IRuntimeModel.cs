// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models;

public interface IRuntimeModel
{
    [DataMember(Name = "run_for")]
    [JsonPropertyName("run_for")]
    IEnumerable<string>? RunFor { get; }

    [DataMember(Name = "type")]
    [JsonPropertyName("type")]
    string Type { get; }
}
