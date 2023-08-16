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

            FluxPluginManifest? manifest = JsonSerializer.Deserialize<FluxPluginManifest>(aiPlugin.ToJson());
            if (manifest == null)
            {
                // TODO Log an error or warning instead
                throw new InvalidDataException("Unable to deserialize the manifest");
            }

            // Deserialize the runtimes
            List<RuntimeRecord> runtimes = new();
            foreach (JsonNode runtime in manifest.Runtimes)
            {
                string? runtimeType = runtime["type"]?.ToString();
                if (string.IsNullOrWhiteSpace(runtimeType))
                {
                    throw new InvalidOperationException("Runtime type not set.");
                }

                switch (runtimeType!.ToUpperInvariant())
                {
                    case OpenApiRuntimeRecord.TypeValue:
                        OpenApiRuntimeRecord? openApiRuntime = JsonSerializer.Deserialize<OpenApiRuntimeRecord>(runtime.ToJson());
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

            // If there is single runtime with no "run_for" property, set that as the default.
            RuntimeRecord[] defaultRuntimeCandidates = runtimes.Where(r => r.RunFor == null || !r.RunFor.Any()).ToArray();
            RuntimeRecord? defaultRuntime = null;
            if (defaultRuntimeCandidates != null && defaultRuntimeCandidates.Length == 1)
            {
                defaultRuntime = defaultRuntimeCandidates.Single();
            }

            // Construct SK function
            foreach (FluxPluginManifest.PluginFunction pluginFunction in manifest.Functions)
            {
                // Find the runtime type for this function, or use the default if there isn't one set explicitly.
                RuntimeRecord? functionRuntime = runtimes
                    .Where(r => r.RunFor != null && r.RunFor.Contains(pluginFunction.Name, StringComparer.OrdinalIgnoreCase))
                    .FirstOrDefault()
                    ?? defaultRuntime;

                if (functionRuntime == null)
                {
                    throw new InvalidOperationException($"Unable to find a runtime for function '{pluginFunction.Name}'.");
                }

                FirstPartyPluginFunction function = new(
                    pluginFunction: pluginFunction,
                    skillName: manifest.Namespace,
                    description: manifest.Description,
                    orchestrationData: FluxOrchestrationData.FromFunctionConfig(pluginFunction),
                    runtime: functionRuntime,
                    kernel: kernel
                    );
                result.Add(function.PluginFunction.Name, function);
            }
        }

        return result;
    }
}
