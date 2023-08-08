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
using static Microsoft.SemanticKernel.Skills.FirstPartyPlugin.MicrosoftAiPluginManifest.FunctionConfig;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

public interface IFirstPartyPluginFunction : ISKFunction
{

}

/// <summary>
/// TODO
/// </summary>
public record FirstPartyPluginFunction : ISKFunction
{
    private readonly ILogger _logger;

    public FunctionConfig Config { get; private set; }
    private readonly ISKFunction _function;
    public StateKey State { get; set; }

    public string Name => this._function.Name;

    public string SkillName => this._function.SkillName;

    public string Description => this._function.Description;

    public bool IsSemantic => this._function.IsSemantic;

    public CompleteRequestSettings RequestSettings => throw new NotImplementedException();

    public FirstPartyPluginFunction(
        FunctionConfig config,
        ILogger? logger = null)
    {
        this._logger = logger ?? NullLogger.Instance;
        this.Config = config;
        this.State = StateKey.Reasoning; 
        this.Function = SKFunction.FromNativeFunction(
            nativeFunction: this.ExecuteAsync,
            functionName: this.Config.Name,
            logger: this._logger);
    }

    public Task<SKContext> ExecuteAsync(SKContext context)
    {
        throw new NotImplementedException();
    }

    public FunctionView Describe()
    {
        throw new NotImplementedException();
    }

    public Task<SKContext> InvokeAsync(SKContext context, CompleteRequestSettings? settings = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ISKFunction SetDefaultSkillCollection(IReadOnlySkillCollection skills)
    {
        throw new NotImplementedException();
    }

    public ISKFunction SetAIService(Func<ITextCompletion> serviceFactory)
    {
        throw new NotImplementedException();
    }

    public ISKFunction SetAIConfiguration(CompleteRequestSettings settings)
    {
        throw new NotImplementedException();
    }
}
