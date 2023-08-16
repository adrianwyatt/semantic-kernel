// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

public class OrchestrationFunctionView : FunctionView
{
    public IOrchestrationModel? OrchestrationData { get; set; }
}
