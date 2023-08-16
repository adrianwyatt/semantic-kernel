// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.FirstPartyPlugin.Models;
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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of invocable functions</returns>
    public static Task<IDictionary<string, ISKFunction>> ImportFirstPartyPluginAsync(
        this IKernel kernel,
        string skillName,
        string filePath,
        CancellationToken cancellationToken = default)
        => kernel.ImportFirstPartyPluginAsync(skillName, File.OpenRead(filePath), cancellationToken);

    /// <summary>
    /// Imports an Microsoft first-party AI plugin
    /// </summary>
    /// <param name="kernel">Semantic Kernel instance.</param>
    /// <param name="skillName">Skill name.</param>
    /// <param name="stream">Stream of the AI plugin manifest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of invocable functions</returns>
    public static async Task<IDictionary<string, ISKFunction>> ImportFirstPartyPluginAsync(
        this IKernel kernel,
        string skillName,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(kernel);
        Verify.ValidSkillName(skillName);

        Dictionary<string, ISKFunction> result = new();

        using var reader = new StreamReader(stream);
        string json = await reader.ReadToEndAsync().ConfigureAwait(false);

        // Get the aiPlugins properties from fileJson
        JsonNode? rootNode = JsonNode.Parse(
            json: json,
            documentOptions: new JsonDocumentOptions() { CommentHandling = JsonCommentHandling.Skip });

        JsonArray? optionsOne = rootNode?["options"]?.AsArray();
        JsonNode? optionsTwo = optionsOne?[0]?["options"]; // TODO look for options elements
        JsonArray? aiPlugins = optionsTwo?["AiPlugins"]?.AsArray();

        if (aiPlugins == null)
        {
            return new Dictionary<string, ISKFunction>();
        }

        // Parse the manifest and create the SK functions
        foreach (JsonNode? aiPlugin in aiPlugins)
        {
            if (aiPlugin == null)
            {
                continue;
            }

            FluxPluginModel? manifest = JsonSerializer.Deserialize<FluxPluginModel>(aiPlugin.ToJson());
            if (manifest == null)
            {
                throw new InvalidDataException("Unable to deserialize the manifest");
            }

            // Parse the runtimes and set a default (if any).
            List<IRuntimeModel> runtimes = ParseRuntimes(manifest);
            IRuntimeModel? defaultRuntime = GetDefaultRuntime(runtimes);

            // Construct SK function
            foreach (FluxPluginModel.PluginFunction pluginFunction in manifest.Functions)
            {
                // Find the runtime type for this function, or use the default if there isn't one set explicitly.
                IRuntimeModel? functionRuntime = runtimes
                    .Where(r => r.RunFor != null && r.RunFor.Contains(pluginFunction.Name, StringComparer.OrdinalIgnoreCase))
                    .FirstOrDefault()
                    ?? defaultRuntime;

                if (functionRuntime == null)
                {
                    throw new InvalidDataException($"Unable to find a runtime for function '{pluginFunction.Name}'.");
                }

                List<ParameterView> parameters = ParseParameters(pluginFunction);

                FirstPartyPluginFunction function = new(
                    pluginFunction: pluginFunction,
                    skillName: manifest.Namespace,
                    description: manifest.Description,
                    orchestrationData: FluxOrchestrationModel.FromFunctionConfig(pluginFunction),
                    runtime: functionRuntime,
                    parameters: parameters,
                    kernel: kernel
                    );

                result.Add(function.PluginFunction.Name, function);
            }
        }

        return result;
    }

    private static List<ParameterView> ParseParameters(FluxPluginModel.PluginFunction pluginFunction)
    {
        List<ParameterView> parameters = new();
        if (pluginFunction.Parameters != null)
        {
            foreach (KeyValuePair<string, JsonNode> parameterProperty in pluginFunction.Parameters.Properties)
            {
                // TODO support 'enum' restricted values
                parameters.Add(new ParameterView(
                    name: parameterProperty.Key,
                    description: parameterProperty.Value["description"]?.ToString() ?? string.Empty,
                    defaultValue: parameterProperty.Value["default"]?.ToString() ?? string.Empty,
                    new ParameterViewType(parameterProperty.Value["type"]?.ToString() ?? string.Empty)));
            }
        }

        return parameters;
    }

    private static IRuntimeModel? GetDefaultRuntime(List<IRuntimeModel> runtimes)
    {
        IRuntimeModel? defaultRuntime = null;
        IRuntimeModel[] defaultRuntimeCandidates = runtimes.Where(r => r.RunFor == null || !r.RunFor.Any()).ToArray();
        if (defaultRuntimeCandidates != null && defaultRuntimeCandidates.Length == 1)
        {
            defaultRuntime = defaultRuntimeCandidates.Single();
        }

        return defaultRuntime;
    }

    private static List<IRuntimeModel> ParseRuntimes(FluxPluginModel manifest)
    {
        List<IRuntimeModel> runtimes = new();
        foreach (JsonNode runtime in manifest.Runtimes)
        {
            string? runtimeType = runtime["type"]?.ToString();
            if (string.IsNullOrWhiteSpace(runtimeType))
            {
                throw new InvalidOperationException("Runtime type not set.");
            }

            switch (runtimeType!.ToLowerInvariant())
            {
                case OpenApiRuntimeModel.TypeValue:
                    OpenApiRuntimeModel? openApiRuntime = JsonSerializer.Deserialize<OpenApiRuntimeModel>(runtime.ToJson());
                    if (openApiRuntime == null)
                    {
                        throw new InvalidDataException($"Unable to deserialize the '{runtimeType}' runtime.");
                    }
                    runtimes.Add(openApiRuntime);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported runtime type: {runtimeType}.");
            }
        }

        return runtimes;
    }
}
