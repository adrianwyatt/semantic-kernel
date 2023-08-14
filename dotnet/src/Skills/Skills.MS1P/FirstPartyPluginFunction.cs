// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using static Microsoft.SemanticKernel.Skills.FirstPartyPlugin.MicrosoftAiPluginManifest;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

/// <summary>
/// TODO
/// </summary>
public class FirstPartyPluginFunction : IFirstPartyPluginFunction, ISKFunction
{
    private readonly ILogger _logger;

    private ISKFunction _sKFunction;

    public FunctionConfig Config { get; }

    public IOrchestrationData? OrchestrationData { get; }

    public string Name => this._sKFunction.Name;

    public string SkillName => this._sKFunction.SkillName;

    public string Description => this._sKFunction.Description;

    public bool IsSemantic => this._sKFunction.IsSemantic;

    public CompleteRequestSettings RequestSettings => throw new NotImplementedException();

    public FirstPartyPluginFunction(
        FunctionConfig config,
        string skillName,
        string description,
        IOrchestrationData? orchestrationData = null,
        ILogger? logger = null)
    {
        this._logger = logger ?? NullLogger.Instance;
        this.Config = config;
        this.OrchestrationData = orchestrationData;

        this._sKFunction = SKFunction.FromNativeFunction(this.ExecuteAsync,
            skillName: skillName,
            description: description,
            functionName: config.Name,
            logger: logger);
    }

    public SKContext ExecuteAsync(SKContext context)
    {
        // TODO look at runtimes.
        // 
        throw new NotImplementedException();
    }

    public Task<SKContext> InvokeAsync(SKContext context, CompleteRequestSettings? settings = null, CancellationToken cancellationToken = default)
        => this._sKFunction.InvokeAsync(context, settings, cancellationToken);

    public ISKFunction SetDefaultSkillCollection(IReadOnlySkillCollection skills)
        => this._sKFunction.SetDefaultSkillCollection(skills);

    public ISKFunction SetAIService(Func<ITextCompletion> serviceFactory)
        => this._sKFunction.SetAIService(serviceFactory);

    public ISKFunction SetAIConfiguration(CompleteRequestSettings settings)
        => this._sKFunction.SetAIConfiguration(settings);

    public FunctionView Describe()
        => this._sKFunction.Describe();
}
