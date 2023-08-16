// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models;

public record RuntimeRecord
{
    [DataMember(Name = "run_for")]
    [JsonPropertyName("run_for")]
    public IEnumerable<string>? RunFor { get; set; }

    [DataMember(Name = "type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "NONE";
}
