// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using static Microsoft.SemanticKernel.Skills.FirstPartyPlugin.FluxPluginManifest;
using static Microsoft.SemanticKernel.Skills.FirstPartyPlugin.FluxPluginManifest.PluginFunction;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

/// <summary>
/// TODO
/// </summary>
public class FirstPartyPluginFunction : /*IFirstPartyPluginFunction,*/ ISKFunction
{
    private readonly ILogger _logger;

    public PluginFunction PluginFunction { get; }

    public IOrchestrationData OrchestrationData { get; }

    public Runtime Runtime { get; }

    public string Name { get; }

    public string SkillName { get; }

    public string Description { get; }

    public bool IsSemantic { get; }

    public CompleteRequestSettings RequestSettings { get; } = new();

    public IList<ParameterView> Parameters { get; } = new List<ParameterView>(); // TODO populate

    public FirstPartyPluginFunction(
        PluginFunction pluginFunction,
        string skillName,
        string description,
        IOrchestrationData orchestrationData,
        ILogger? logger = null)
    {
        this._logger = logger ?? NullLogger.Instance;
        this.PluginFunction = pluginFunction;
        this.OrchestrationData = orchestrationData;

        this.SkillName = skillName;
        this.Description = description;
        this.Name = pluginFunction.Name;
    }

    public Task<SKContext> InvokeAsync(SKContext context, CompleteRequestSettings? settings = null, CancellationToken cancellationToken = default)
    {
        // Get state from context
        if (!context.Variables.TryGetValue("state", out string? state))
        {
            throw new InvalidOperationException("State not found in context variables.");
        }

        if (!Enum.TryParse<StateKey>(state, true, out StateKey stateKey))
        {
            throw new InvalidOperationException($"State '{state}' is not a valid state.");
        }

        // Find which runtime is responsible for this function
        foreach (var runtime in this.PluginFunction.Runtimes)
        {

        }

        throw new NotImplementedException();
    }

    public ISKFunction SetDefaultSkillCollection(IReadOnlySkillCollection skills)
        => this;

    public ISKFunction SetAIService(Func<ITextCompletion> serviceFactory)
    {
        // TODO check if semantic - runtimes?
        return this;
    }

    public ISKFunction SetAIConfiguration(CompleteRequestSettings settings)
    {
        // TODO check if semantic - runtimes?
        return this;
    }
    public FunctionView Describe()
    {
        // todo extend function view to contain orchestration data
        return new OrchestrationFunctionView
        {
            IsSemantic = this.IsSemantic,
            Name = this.Name,
            SkillName = this.SkillName,
            Description = this.Description,
            Parameters = this.Parameters,
            OrchestrationData = this.OrchestrationData
        };
    }
}
