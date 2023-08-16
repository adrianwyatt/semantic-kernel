// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using static Microsoft.SemanticKernel.Skills.FirstPartyPlugin.FluxPluginManifest.PluginFunction;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

public record Runtime
{
    [DataMember(Name = "run_for")]
    [JsonPropertyName("run_for")]
    public IEnumerable<string>? RunFor { get; set; }

    [DataMember(Name = "type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "None";
}

public record LowCodeRuntime : Runtime
{
    public const string TypeValue = "lowcode";

    [DataMember(Name = "url")]
    [JsonPropertyName("url")]
    public Uri Url { get; set; }

    [DataMember(Name = "template")]
    [JsonPropertyName("template")]
    public IEnumerable<string> Template { get; set; } = new List<string>();

    [DataMember(Name = "progress_style")]
    [JsonPropertyName("progress_style")]
    public ProgressStyle? ProgressStyle { get; set; }

    [DataMember(Name = "icon_url")]
    [JsonPropertyName("icon_url")]
    public Uri? IconUrl { get; set; }

    [DataMember(Name = "timeout")]
    [JsonPropertyName("timeout")]
    public string? Timeout { get; set; }
}

public record OpenApiRuntime : Runtime
{
    public const string TypeValue = "openapi";

    [DataMember(Name = "url")]
    [JsonPropertyName("url")]
    public Uri Url { get; set; }

    [DataMember(Name = "progress_style")]
    [JsonPropertyName("progress_style")]
    public ProgressStyle? ProgressStyle { get; set; }

    [DataMember(Name = "icon_url")]
    [JsonPropertyName("icon_url")]
    public Uri? IconUrl { get; set; }

    [DataMember(Name = "timeout")]
    [JsonPropertyName("timeout")]
    public string? Timeout { get; set; }

    // TODO auth
}
