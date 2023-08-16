// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models;
using Microsoft.SemanticKernel.Skills.OpenAPI.Extensions;
using static Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models.FluxPluginModel;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

/// <summary>
/// TODO
/// </summary>
public partial class FirstPartyPluginFunction : ISKFunction
{
    private delegate Task<SKContext> ExecuteAsyncDelegate(SKContext context, CompleteRequestSettings? settings = null, CancellationToken cancellationToken = default);

    public PluginFunction PluginFunction { get; }

    public IOrchestrationModel OrchestrationData { get; }

    private readonly IRuntimeModel _runtime;

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string SkillName { get; }

    /// <inheritdoc/>
    public string Description { get; }

    /// <inheritdoc/>
    public bool IsSemantic { get; }

    /// <inheritdoc/>
    public CompleteRequestSettings RequestSettings { get; } = new(); // TODO required when semantic

    private readonly ILogger _logger;

    private readonly IList<ParameterView> _parameters;

    private readonly Dictionary<Type, ExecuteAsyncDelegate> _runtimeHandlers;

    private readonly IKernel _kernel;

    public FirstPartyPluginFunction(
        PluginFunction pluginFunction,
        string skillName,
        string description,
        IOrchestrationModel orchestrationData,
        IRuntimeModel runtime,
        IList<ParameterView> parameters,
        IKernel kernel,
        ILogger? logger = null)
    {
        this._logger = logger ?? NullLogger.Instance;
        this._runtime = runtime;
        this._kernel = kernel;
        this._parameters = parameters;

        this.PluginFunction = pluginFunction;
        this.OrchestrationData = orchestrationData;

        this.SkillName = skillName;
        this.Description = description;
        this.Name = pluginFunction.Name;

        // Map runtime types to handlers
        // TODO Find runtime handlers dynamically
        this._runtimeHandlers = new Dictionary<Type, ExecuteAsyncDelegate>()
        {
            { typeof(OpenApiRuntimeModel), this.OpenApiExecuteAsync }
        };
    }

    /// <inheritdoc/>
    public Task<SKContext> InvokeAsync(SKContext context, CompleteRequestSettings? settings = null, CancellationToken cancellationToken = default)
    {
        // Get state string from context
        if (!context.Variables.TryGetValue("state", out string? state))
        {
            throw new InvalidOperationException("State not found in context variables.");
        }

        // Parse state string to StateKey
        if (!Enum.TryParse<StateKey>(state, true, out StateKey stateKey))
        {
            throw new InvalidOperationException($"State '{state}' is not a valid state.");
        }

        // Get runtime handler
        if (!this._runtimeHandlers.TryGetValue(this._runtime.GetType(), out ExecuteAsyncDelegate? runtimeHandlerAsync))
        {
            throw new InvalidOperationException($"Runtime '{this._runtime.GetType().Name}' is not supported.");
        }

        return runtimeHandlerAsync(context, settings, cancellationToken);
    }

    /// <inheritdoc/>
    public ISKFunction SetDefaultSkillCollection(IReadOnlySkillCollection skills)
        => this;

    /// <inheritdoc/>
    public ISKFunction SetAIService(Func<ITextCompletion> serviceFactory)
        => this; // TODO check if semantic - runtimes?

    /// <inheritdoc/>
    public ISKFunction SetAIConfiguration(CompleteRequestSettings settings)
        => this; // TODO check if semantic - runtimes?

    /// <inheritdoc/>
    public FunctionView Describe()
        => new OrchestrationFunctionView
        {
            IsSemantic = this.IsSemantic,
            Name = this.Name,
            SkillName = this.SkillName,
            Description = this.Description,
            Parameters = this._parameters,
            OrchestrationData = this.OrchestrationData
        };

    private Task<SKContext> OpenApiExecuteAsync(SKContext context, CompleteRequestSettings? settings = null, CancellationToken cancellationToken = default)
    {
        OpenApiRuntime openApiRuntime = new OpenApiRuntime(
            this._runtime as OpenApiRuntimeModel ?? throw new InvalidOperationException("Runtime is not of type OpenApiRuntimeRecord."),
            new OpenApiSkillExecutionParameters(),
            this,
            this._kernel);

        return openApiRuntime.InvokeAsync(context, settings, cancellationToken);
    }
}
