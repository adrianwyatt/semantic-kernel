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
using static Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models.FluxPluginManifest;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

/// <summary>
/// TODO
/// </summary>
public partial class FirstPartyPluginFunction : ISKFunction
{
    private delegate Task<SKContext> ExecuteAsyncDelegate(SKContext context, CompleteRequestSettings? settings = null, CancellationToken cancellationToken = default);

    private readonly ILogger _logger;

    public PluginFunction PluginFunction { get; }

    public IOrchestrationData OrchestrationData { get; }

    private readonly RuntimeRecord _runtime;

    public string Name { get; }

    public string SkillName { get; }

    public string Description { get; }

    public bool IsSemantic { get; }

    public CompleteRequestSettings RequestSettings { get; } = new();

    public IList<ParameterView> Parameters { get; } = new List<ParameterView>(); // TODO populate

    private Dictionary<Type, ExecuteAsyncDelegate> _runtimeHandlers; // TODO Find runtime handlers dynamically

    private readonly IKernel _kernel;

    public FirstPartyPluginFunction(
        PluginFunction pluginFunction,
        string skillName,
        string description,
        IOrchestrationData orchestrationData,
        RuntimeRecord runtime,
        IKernel kernel,
        ILogger? logger = null)
    {
        this._logger = logger ?? NullLogger.Instance;
        this.PluginFunction = pluginFunction;
        this.OrchestrationData = orchestrationData;

        this.SkillName = skillName;
        this.Description = description;
        this.Name = pluginFunction.Name;
        this._runtime = runtime;
        this._kernel = kernel;

        this._runtimeHandlers = new Dictionary<Type, ExecuteAsyncDelegate>()
        {
            { typeof(OpenApiRuntimeRecord), this.OpenApiExecuteAsync }
        };
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

        if (!this._runtimeHandlers.TryGetValue(this._runtime.GetType(), out ExecuteAsyncDelegate? runtimeHandlerAsync))
        {
            throw new InvalidOperationException($"Runtime '{this._runtime.GetType().Name}' is not supported.");
        }

        return runtimeHandlerAsync(context, settings, cancellationToken);
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

    #region Runtime Handlers
    private async Task<SKContext> OpenApiExecuteAsync(SKContext context, CompleteRequestSettings? settings = null, CancellationToken cancellationToken = default)
    {
        OpenApiRuntimeRecord openApiRuntime = this._runtime as OpenApiRuntimeRecord
            ?? throw new InvalidOperationException("Runtime is not of type OpenApiRuntimeRecord.");

        OpenApiSkillExecutionParameters openApiParameters = new();
        // TODO populate execution parameters.

        // Import the openAPI functions
        IDictionary<string, ISKFunction> openApiFunctions = await this._kernel.ImportAIPluginAsync(
            skillName: this.SkillName,
            uri: openApiRuntime.Url,
            executionParameters: openApiParameters,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Find the function to execute
        if (!openApiFunctions.TryGetValue(this.Name, out ISKFunction? openApiFunction))
        {
            throw new InvalidOperationException($"Function '{this.Name}' not found in OpenAPI plugin ({openApiRuntime.Url}).");
        }

        return await openApiFunction.InvokeAsync(context, settings, cancellationToken).ConfigureAwait(false);
    }
    #endregion
}
