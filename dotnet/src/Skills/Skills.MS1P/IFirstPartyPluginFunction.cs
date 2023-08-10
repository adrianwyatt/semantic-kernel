// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

public interface IFirstPartyPluginFunction
{
    public IOrchestrationData OrchestrationData { get; set; }
}
