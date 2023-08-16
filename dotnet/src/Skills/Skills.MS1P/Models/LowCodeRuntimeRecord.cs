﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using static Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models.FluxPluginManifest;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models;

public record LowCodeRuntimeRecord : RuntimeRecord
{
    public const string TypeValue = "LOWCODE";

    [DataMember(Name = "url")]
    [JsonPropertyName("url")]
    public Uri Url { get; set; }

    [DataMember(Name = "template")]
    [JsonPropertyName("template")]
    public IEnumerable<string> Template { get; set; } = new List<string>();

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
}
