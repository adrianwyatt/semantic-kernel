// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using static Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models.FluxPluginModel;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models;

public record OpenApiRuntimeModel : IRuntimeModel
{
    public const string TypeValue = "openapi";

    [DataMember(Name = "type")]
    [JsonPropertyName("type")]
    public string Type { get; } = TypeValue;

    [DataMember(Name = "run_for")]
    [JsonPropertyName("run_for")]
    public IEnumerable<string>? RunFor { get; }

    [DataMember(Name = "url")]
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = new Uri("http://localhost");

    [IgnoreDataMember]
    [JsonIgnore]
    public ProgressStyle ProgressStyle { get; set; } = ProgressStyle.None;

    [DataMember(EmitDefaultValue = false, Name = "progress_style")]
    [JsonPropertyName("progress_style")]
    public string ProgressStyleString
    {
        get => this.ProgressStyle.ToString();
        set
        {
            if (Enum.TryParse<ProgressStyle>(value, out ProgressStyle result))
            {
                this.ProgressStyle = result;
            }
            else
            {
                this.ProgressStyle = ProgressStyle.None;
            }
        }
    }

    [DataMember(Name = "icon_url")]
    [JsonPropertyName("icon_url")]
    public Uri? IconUrl { get; set; }

    [DataMember(Name = "timeout")]
    [JsonPropertyName("timeout")]
    public string? Timeout { get; set; }

    // TODO auth
}
