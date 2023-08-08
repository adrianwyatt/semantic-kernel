// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

/// <summary>
/// An augmented manifest for an AI plugin to incorporate specifics of how plugins integrate with planning/orchestration.
/// </summary>
public record MicrosoftAiPluginManifest
{
    /// <summary>
    /// Schema version of the manifest.
    /// </summary>
    [DataMember(Name = "schema_version")]
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the plugin.
    /// </summary>
    [DataMember(Name = "name_for_human")]
    [JsonPropertyName("name_for_human")]
    public string NameForHuman { get; set; } = string.Empty;

    /// <summary>
    /// Model-optimized name of the plugin.
    /// </summary>
    [DataMember(Name = "name_for_model")]
    [JsonPropertyName("name_for_model")]
    public string NameForModel { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the plugin.
    /// </summary>
    [DataMember(Name = "description_for_human")]
    [JsonPropertyName("description_for_human")]
    public string DescriptionForHuman { get; set; } = string.Empty;

    /// <summary>
    /// Model-optimized description of the plugin.
    /// </summary>
    [DataMember(Name = "description_for_model")]
    [JsonPropertyName("description_for_model")]
    public string DescriptionForModel { get; set; } = string.Empty;

    /// <summary>
    /// URL to the logo to use for display.
    /// </summary>
    [DataMember(Name = "logo_url")]
    [JsonPropertyName("logo_url")]
    public Uri? LogoUrl { get; set; } = new Uri("https://www.microsoft.com/favicon.ico");

    /// <summary>
    /// Email contact for safety/moderation, support, and deactivation.
    /// </summary>
    [DataMember(Name = "contact_email")]
    [JsonPropertyName("contact_email")]
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// URL to the legal information for this manifest.
    /// </summary>
    [DataMember(Name = "legal_info_url")]
    [JsonPropertyName("legal_info_url")]
    public Uri? LegalInfoUrl { get; set; }

    /// <summary>
    /// The list of functions that are defined in your plugin.
    /// </summary>
    [DataMember(Name = "functions")]
    [JsonPropertyName("functions")]
    public IEnumerable<FunctionConfig> FunctionConfigs { get; set; } = new List<FunctionConfig>();

    /// <summary>
    /// The list of runtimes that handle executing your plugin functions.
    /// </summary>
    [DataMember(Name = "runtimes")]
    [JsonPropertyName("runtimes")]
    public IEnumerable<object> Runtimes { get; set; } = new List<object>();

    public record FunctionConfig
    {
        /// <summary>
        /// Optional identifier.
        /// </summary>
        [DataMember(Name = "id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The name the model will use to target the plugin (no spaces allowed, only letters and numbers).
        /// </summary>
        [DataMember(Name = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The data needed to determine how the model will interact with your function in each state.
        /// Typical data are the description, instructions, and examples for the model.
        /// </summary>
        [DataMember(Name = "states")]
        [JsonPropertyName("states")]
        public IDictionary<StateKey, State> States { get; set; } = new Dictionary<StateKey, State>();

        public enum StateKey
        {
            /// <summary>
            /// The reasoning state for the model.
            /// This is when the model can call functions and do computations.
            /// </summary>
            Reasoning,

            /// <summary>
            /// The responding state for the model.
            /// This is when the model can generate text that will be shown to the user.
            /// The model cannot invoke functions in the responding state.
            /// </summary>
            Responding,

            /// <summary>
            /// If the model detects adverserial or unsafe dialogue, it can enter the
            /// disengaging state in which case it will stop responding.
            /// </summary>
            Disengaging,
        }

        public record State
        {
            /// <summary>
            /// The description better tailored to the model, such as token context length considerations or keyword usage for improved plugin prompting.
            /// </summary>
            [DataMember(Name = "description")]
            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            /// <summary>
            /// The instructions for when/how the model should invoke the function.
            /// </summary>
            [DataMember(Name = "instructions")]
            [JsonPropertyName("instructions")]
            public string Instructions { get; set; } = string.Empty;

            /// <summary>
            /// Any examples that need to be provided to the model for your function.
            /// </summary>
            [DataMember(Name = "examples")]
            [JsonPropertyName("examples")]
            public string Examples { get; set; } = string.Empty;
        }
    }
}
