// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models;

/// <summary>
/// An augmented manifest for an AI plugin to incorporate specifics of how plugins integrate with planning/orchestration.
/// </summary>
public record FluxPluginModel
{
    [DataMember(Name = "name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [DataMember(Name = "description")]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [DataMember(Name = "schema_version")]
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    [DataMember(Name = "namespace")]
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [DataMember(Name = "contact_email")]
    [JsonPropertyName("contact_email")]
    public string ContactEmail { get; set; } = string.Empty;

    [DataMember(Name = "legal_info_url")]
    [JsonPropertyName("legal_info_url")]
    public Uri? LegalInfoUrl { get; set; }

    [DataMember(Name = "logo_url")]
    [JsonPropertyName("logo_url")]
    public Uri? LogoUrl { get; set; } = new Uri("https://www.microsoft.com/favicon.ico");

    [DataMember(Name = "functions")]
    [JsonPropertyName("functions")]
    public IEnumerable<PluginFunction> Functions { get; set; } = new List<PluginFunction>();

    [DataMember(Name = "runtimes")]
    [JsonPropertyName("runtimes")]
    public IEnumerable<JsonNode> Runtimes { get; set; } = new List<JsonNode>();

    public record PluginFunction
    {
        [DataMember(Name = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [DataMember(Name = "parameters")]
        [JsonPropertyName("parameters")]
        public FunctionParameters? Parameters { get; set; }

        [DataMember(Name = "returns")]
        [JsonPropertyName("returns")]
        public JsonNode? Returns { get; set; }

        [DataMember(Name = "states")]
        [JsonPropertyName("states")]
        public IDictionary<StateKey, State> States { get; set; } = new Dictionary<StateKey, State>();
    }

    public enum ProgressStyle
    {
        None,
        ShowUsage,
        ShowUsageWithInput,
        ShowUsageWithInputAndOutput
    }

    public record FunctionParameters
    {
        [DataMember(Name = "type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [DataMember(Name = "properties")]
        [JsonPropertyName("properties")]
        public IDictionary<string, JsonNode> Properties { get; set; } = new Dictionary<string, JsonNode>();

        [DataMember(Name = "required")]
        [JsonPropertyName("required")]
        public IEnumerable<string> Required { get; set; } = new List<string>();
    }

    public enum StateKey
    {
        Reasoning,
        Responding,
        Disengaging,
    }

    public record State
    {
        [DataMember(Name = "description")]
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [DataMember(Name = "instructions")]
        [JsonPropertyName("instructions")]
        public string Instructions { get; set; } = string.Empty;

        [DataMember(Name = "examples")]
        [JsonPropertyName("examples")]
        public string Examples { get; set; } = string.Empty;
    }
}
