// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using static Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models.FluxPluginModel;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models;

public record FluxOrchestrationModel : IOrchestrationModel
{
    public string Type => "flux";

    public IDictionary<StateKey, Details> StateDetails { get; } = new Dictionary<StateKey, Details>();

    public class Details
    {
        public string Instructions { get; set; } = string.Empty;
        public string Examples { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public static FluxOrchestrationModel FromFunctionConfig(PluginFunction pluginFunction)
    {
        FluxOrchestrationModel data = new();
        foreach (KeyValuePair<StateKey, State> state in pluginFunction.States)
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
