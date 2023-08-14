// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.SkillDefinition;
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
            foreach (MicrosoftAiPluginManifest.FunctionConfig functionConfig in manifest.FunctionConfigs)
            {
                foreach (MicrosoftAiPluginManifest.FunctionConfig.StateKey functionStateKey in functionConfig.States.Keys)
                {
                    FirstPartyPluginFunction function = new(
                        config: functionConfig,
                        skillName: manifest.NameForHuman, // TODO: use the model name, but current examples don't have those
                        description: manifest.DescriptionForHuman, // TODO: use the model descriptions, but current examples don't have those
                        orchestrationData: FluxOrchestrationData.FromFunctionConfig(functionConfig)
                        );
                    result.Add(function.Config.Name, function);
                }
            }
        }

        return result;
    }
}
