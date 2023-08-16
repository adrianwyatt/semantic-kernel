// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.SkillDefinition;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

public class OrchestrationFunctionView : FunctionView
{
    public IOrchestrationData? OrchestrationData { get; set; }
}
