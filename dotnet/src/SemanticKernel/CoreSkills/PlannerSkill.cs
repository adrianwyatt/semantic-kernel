// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Orchestration.Extensions;
using Microsoft.SemanticKernel.Planning.Planners;
using Microsoft.SemanticKernel.SkillDefinition;

namespace Microsoft.SemanticKernel.CoreSkills;

/// <summary>
/// <para>Semantic skill that creates and executes plans.</para>
/// <para>
/// Usage:
/// var kernel = SemanticKernel.Build(ConsoleLogger.Log);
/// kernel.ImportSkill("planner", new PlannerSkill(kernel));
/// </para>
/// </summary>
public class PlannerSkill
{
    /// <summary>
    /// Parameter names.
    /// <see cref="ContextVariables"/>
    /// </summary>
    public static class Parameters
    {
        /// <summary>
        /// The number of buckets to create
        /// </summary>
        public const string BucketCount = "bucketCount";

        /// <summary>
        /// The prefix to use for the bucket labels
        /// </summary>
        public const string BucketLabelPrefix = "bucketLabelPrefix";

        /// <summary>
        /// The relevancy threshold when filtering registered functions.
        /// </summary>
        public const string RelevancyThreshold = "relevancyThreshold";

        /// <summary>
        /// The maximum number of relevant functions as result of semantic search to include in the plan creation request.
        /// </summary>
        public const string MaxRelevantFunctions = "MaxRelevantFunctions";

        /// <summary>
        /// The list of skills to exclude from the plan creation request.
        /// </summary>
        public const string ExcludedSkills = "excludedSkills";

        /// <summary>
        /// The list of functions to exclude from the plan creation request.
        /// </summary>
        public const string ExcludedFunctions = "excludedFunctions";

        /// <summary>
        /// The list of functions to include in the plan creation request.
        /// </summary>
        public const string IncludedFunctions = "includedFunctions";
    }

    /// <summary>
    /// The name to use when creating semantic functions that are restricted from the PlannerSkill plans
    /// </summary>
    private const string RestrictedSkillName = "PlannerSkill_Excluded";

    /// <summary>
    /// the bucket semantic function, which takes a list of items and buckets them into a number of buckets
    /// </summary>
    private readonly ISKFunction _bucketFunction;

    /// <summary>
    /// the kernel to use
    /// </summary>
    private readonly IKernel _kernel;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlannerSkill"/> class.
    /// </summary>
    /// <param name="kernel"> The kernel to use </param>
    /// <param name="maxTokens"> The maximum number of tokens to use for the semantic functions </param>
    public PlannerSkill(IKernel kernel, int maxTokens = 1024)
    {
        this._kernel = kernel;

        // TODO Remove this.
        this._bucketFunction = kernel.CreateSemanticFunction(
            promptTemplate: SemanticFunctionConstants.BucketFunctionDefinition,
            skillName: RestrictedSkillName,
            maxTokens: maxTokens,
            temperature: 0.0);
    }

    /// <summary>
    /// When the output of a function is too big, parse the output into a number of buckets.
    /// </summary>
    /// <param name="input"> The input from a function that needs to be parsed into buckets. </param>
    /// <param name="context"> The context to use </param>
    /// <returns> The context with the bucketed results </returns>
    [SKFunction("When the output of a function is too big, parse the output into a number of buckets.")]
    [SKFunctionName("BucketOutputs")]
    [SKFunctionInput(Description = "The output from a function that needs to be parse into buckets.")]
    [SKFunctionContextParameter(Name = Parameters.BucketCount, Description = "The number of buckets.", DefaultValue = "")]
    [SKFunctionContextParameter(
        Name = Parameters.BucketLabelPrefix,
        Description = "The target label prefix for the resulting buckets. " +
                      "Result will have index appended e.g. bucketLabelPrefix='Result' => Result_1, Result_2, Result_3",
        DefaultValue = "Result")]
    public async Task<SKContext> BucketOutputsAsync(string input, SKContext context)
    {
        var bucketsAdded = 0;

        var bucketVariables = new ContextVariables(input);
        if (context.Variables.Get(Parameters.BucketCount, out var bucketCount))
        {
            bucketVariables.Set(Parameters.BucketCount, bucketCount);
        }

        // {'buckets': ['Result 1\nThis is the first result.', 'Result 2\nThis is the second result. It's doubled!', 'Result 3\nThis is the third and final result. Truly astonishing.']}
        var result = await this._bucketFunction.InvokeAsync(new SKContext(bucketVariables, context.Memory, context.Skills, context.Log,
            context.CancellationToken));

        try
        {
            // May need additional formatting here.
            var resultString = result.Result
                .Replace("\\n", "\n")
                .Replace("\n", "\\n");

            var resultObject = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(resultString);

            if (context.Variables.Get(Parameters.BucketLabelPrefix, out var bucketLabelPrefix) &&
                resultObject?.ContainsKey("buckets") == true)
            {
                foreach (var item in resultObject["buckets"])
                {
                    var bucketLabel = $"{bucketLabelPrefix}_{++bucketsAdded}";
                    context.Variables.Set($"{bucketLabel}", item);
                }
            }

            return context;
        }
        catch (Exception e) when (
            e is JsonException or
                InvalidCastException or
                NotSupportedException or
                ArgumentNullException)
        {
            context.Log.LogWarning("Error parsing bucket outputs: {0}", e.Message);
            return context.Fail($"Error parsing bucket outputs: {e.Message}");
        }
    }

    /// <summary>
    /// Create a plan using registered functions to accomplish a goal.
    /// </summary>
    /// <param name="goal"> The goal to accomplish. </param>
    /// <param name="context"> The context to use </param>
    /// <returns> The context with the plan </returns>
    /// <remarks>
    /// The plan is stored in the context as a string. The plan is also stored in the context as a Plan object.
    /// </remarks>
    [SKFunction("Create a plan using registered functions to accomplish a goal.")]
    [SKFunctionName("CreatePlan")]
    [SKFunctionInput(Description = "The goal to accomplish.")]
    [SKFunctionContextParameter(Name = Parameters.RelevancyThreshold, Description = "The relevancy threshold when filtering registered functions.",
        DefaultValue = "")]
    [SKFunctionContextParameter(Name = Parameters.MaxRelevantFunctions,
        Description = "Limits the number of relevant functions as result of semantic search included in the plan creation request.", DefaultValue = "100")]
    [SKFunctionContextParameter(Name = Parameters.ExcludedFunctions, Description = "A list of functions to exclude from the plan creation request.",
        DefaultValue = "")]
    [SKFunctionContextParameter(Name = Parameters.ExcludedSkills, Description = "A list of skills to exclude from the plan creation request.",
        DefaultValue = "")]
    [SKFunctionContextParameter(Name = Parameters.IncludedFunctions, Description = "A list of functions to include in the plan creation request.",
        DefaultValue = "")]
    public async Task<SKContext> CreatePlanAsync(string goal, SKContext context)
    {
        PlannerConfig config = context.GetPlannerConfig();

        var planner = new FunctionFlowPlanner(this._kernel, config);
        var plan = await planner.CreatePlanAsync(goal);

        _ = context.Variables.UpdateWithPlanEntry(plan);

        return context;
    }

    /// <summary>
    /// Execute a plan that uses registered functions to accomplish a goal.
    /// </summary>
    /// <param name="context"> The context to use </param>
    /// <returns> The context with the plan </returns>
    /// <remarks>
    /// The plan is stored in the context as a string. The plan is also stored in the context as a Plan object.
    /// </remarks>
    [SKFunction("Execute a plan that uses registered functions to accomplish a goal.")]
    [SKFunctionName("ExecutePlan")]
    public async Task<SKContext> ExecutePlanAsync(SKContext context)
    {
        if (context.TryGetPlan(out var plan))
        {
            plan = await this._kernel.StepAsync(context.Variables, plan);
            _ = context.Variables.UpdateWithPlanEntry(plan);
        }
        else
        {
            context.Fail("No plan found in context.");
        }

        return context;
    }
}
