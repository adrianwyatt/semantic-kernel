// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models;
using Microsoft.SemanticKernel.Skills.OpenAPI.Extensions;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

/// <summary>
/// TODO
/// </summary>
public partial class OpenApiRuntime
{
    private readonly IKernel _kernel;
    private readonly FirstPartyPluginFunction _function;
    private readonly OpenApiRuntimeModel _model;
    private readonly OpenApiSkillExecutionParameters _openApiParameters;

    public OpenApiRuntime(
        OpenApiRuntimeModel model,
        OpenApiSkillExecutionParameters openApiParameters,
        FirstPartyPluginFunction function,
        IKernel kernel)
    {
        this._model = model;
        this._function = function;
        this._kernel = kernel;
        this._openApiParameters = openApiParameters ?? new();
    }

    public async Task<SKContext> InvokeAsync(SKContext context, CompleteRequestSettings? settings = null, CancellationToken cancellationToken = default)
    {
        // Import the openAPI functions
        IDictionary<string, ISKFunction> openApiFunctions = await this._kernel.ImportAIPluginAsync(
            skillName: this._function.SkillName,
            uri: this._model.Url,
            executionParameters: this._openApiParameters,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Find the function to execute
        if (!openApiFunctions.TryGetValue(this._function.Name, out ISKFunction? openApiFunction))
        {
            throw new InvalidOperationException($"Function '{this._function.Name}' not found in OpenAPI plugin ({this._model.Url}).");
        }

        return await openApiFunction.InvokeAsync(context, settings, cancellationToken).ConfigureAwait(false);
    }
}
