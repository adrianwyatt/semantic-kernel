// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.OpenAPI.Extensions;
using Microsoft.SemanticKernel.Skills.OpenAPI.Model;
using Microsoft.SemanticKernel.Text;

namespace Microsoft.SemanticKernel.Skills.FirstPartyPlugin;

public static class KernelFirstPartyPluginExtensions
{
    /// <summary>
    /// Imports an Microsoft first-party AI plugin
    /// </summary>
    /// <param name="kernel">Semantic Kernel instance.</param>
    /// <param name="skillName">Skill name.</param>
    /// <param name="filePath">The file path to the AI Plugin</param>
    /// <param name="executionParameters">Skill execution parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of invocable functions</returns>
    public static async Task<IDictionary<string, ISKFunction>> ImportFirstPartyPluginAsync(
        this IKernel kernel,
        string skillName,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(kernel);
        Verify.ValidSkillName(skillName);

        Dictionary<string, ISKFunction> result = new();

        string fileJson = File.ReadAllText(filePath);

        // Get the aiPlugins properties from fileJson
        JsonNode? rootNode = JsonNode.Parse(
            json: fileJson,
            documentOptions: new JsonDocumentOptions() { CommentHandling = JsonCommentHandling.Skip });

        JsonArray? optionsOne = rootNode?["options"]?.AsArray();
        JsonNode? optionsTwo = optionsOne?[0]?["options"]; // TODO look for options elements
        JsonArray? aiPlugins = optionsTwo?["AiPlugins"]?.AsArray();

        if (aiPlugins == null)
        {
            return new Dictionary<string, ISKFunction>();
        }

        // Deserialize each aiPlugin
        foreach (JsonNode? aiPlugin in aiPlugins)
        {
            if (aiPlugin == null)
            {
                continue;
            }

            MicrosoftAiPluginManifest? manifest = JsonSerializer.Deserialize<MicrosoftAiPluginManifest>(aiPlugin.ToJson());
            if (manifest == null)
            {
                // TODO Log an error or warning instead
                throw new InvalidDataException("Unable to deserialize the manifest");
            }

            // Construct SK function
            foreach (var functionConfig in manifest.FunctionConfigs)
            {
                FirstPartyPluginFunction function = new FirstPartyPluginFunction(config); // TODO logger
                result.Add(function.Config.Name, function);
                
            }
            
            
        }

        return result;
    }

    private static ISKFunction RegisterFunction(
        this IKernel kernel,
        string skillName,
        RestApiOperationRunner runner,
        RestApiOperation operation,
        Uri? serverUrlOverride = null,
        CancellationToken cancellationToken = default)
    {
        var restOperationParameters = operation.GetParameters(serverUrlOverride);

        var logger = kernel.Logger ?? NullLogger.Instance;

        async Task<SKContext> ExecuteAsync(SKContext context)
        {
            try
            {
                // Extract function arguments from context
                var arguments = new Dictionary<string, string>();
                foreach (var parameter in restOperationParameters)
                {
                    // A try to resolve argument by alternative parameter name
                    if (!string.IsNullOrEmpty(parameter.AlternativeName) && context.Variables.TryGetValue(parameter.AlternativeName!, out string? value))
                    {
                        arguments.Add(parameter.Name, value);
                        continue;
                    }

                    // A try to resolve argument by original parameter name
                    if (context.Variables.TryGetValue(parameter.Name, out value))
                    {
                        arguments.Add(parameter.Name, value);
                        continue;
                    }

                    if (parameter.IsRequired)
                    {
                        throw new KeyNotFoundException(
                            $"No variable found in context to use as an argument for the '{parameter.Name}' parameter of the '{skillName}.{operation.Id}' Rest function.");
                    }
                }

                var result = await runner.RunAsync(operation, arguments, cancellationToken).ConfigureAwait(false);
                if (result != null)
                {
                    context.Variables.Update(result.ToString());
                }
            }
            catch (Exception ex) when (!ex.IsCriticalException())
            {
                logger.LogWarning(ex, "Something went wrong while rendering the Rest function. Function: {0}.{1}. Error: {2}", skillName, operation.Id,
                    ex.Message);
                throw ex;
            }

            return context;
        }

        var parameters = restOperationParameters
            .Select(p => new ParameterView
            {
                Name = p.AlternativeName ?? p.Name,
                Description = $"{p.Description ?? p.Name}{(p.IsRequired ? " (required)" : string.Empty)}",
                DefaultValue = p.DefaultValue ?? string.Empty
            })
            .ToList();

        var function = SKFunction.FromNativeFunction(
            nativeFunction: ExecuteAsync,
            parameters: parameters,
            description: operation.Description,
            skillName: skillName,
            functionName: ConvertOperationIdToValidFunctionName(operation.Id, logger),
            logger: logger);

        return kernel.RegisterCustomFunction(function);
    }
}
