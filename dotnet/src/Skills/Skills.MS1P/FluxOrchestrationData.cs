// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.SemanticKernel.SemanticFunctions;
using static Microsoft.SemanticKernel.Skills.FirstPartyPlugin.MicrosoftAiPluginManifest.FunctionConfig;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

public class FluxOrchestrationData : IOrchestrationData
{
    public string Type => "flux";

    public IDictionary<StateKey, Details> StateDetails { get; } = new Dictionary<StateKey, Details>();

    public class Details
    {
        public string Instructions { get; set; } = string.Empty;
        public string Examples { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public static FluxOrchestrationData FromFunctionConfig(MicrosoftAiPluginManifest.FunctionConfig config)
    {
        FluxOrchestrationData data = new();
        foreach (KeyValuePair<StateKey, State> state in config.States)
        {
            data.StateDetails.Add(state.Key, new Details()
            {
                Description = state.Value.Description,
                Examples = state.Value.Examples,
                Instructions = state.Value.Instructions
            });
        }
        return data;

    }
}
