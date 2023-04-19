// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;

namespace Microsoft.SemanticKernel.Planning.Planners;

/// <summary>
/// A planner that uses the Function Flow semantic function to create a plan.
/// </summary>
public class FunctionFlowPlanner
{
    /// <summary>
    /// Initialize a new instance of the <see cref="FunctionFlowPlanner"/> class.
    /// </summary>
    /// <param name="kernel">The semantic kernel instance.</param>
    /// <param name="config">The planner configuration.</param>
    public FunctionFlowPlanner(IKernel? kernel, PlannerConfig? config = null)
    {
        Verify.NotNull(kernel, $"{this.GetType().FullName} requires a kernel instance.");
        if (config == null)
        {
            config = new PlannerConfig();
            config.ExcludedFunctions.Add(RestrictedSkillName);
        }

        this.Config = config;

        this._functionFlowFunction = kernel.CreateSemanticFunction(
            promptTemplate: SemanticFunctionConstants.FunctionFlowFunctionDefinition,
            skillName: RestrictedSkillName,
            description: "Given a request or command or goal generate a step by step plan to " +
                         "fulfill the request using functions. This ability is also known as decision making and function flow",
            maxTokens: this.Config.MaxTokens,
            temperature: 0.0,
            stopSequences: new[] { "<!--" });

        this._context = kernel.CreateNewContext();
    }

    /// <summary>
    /// Create a plan for a goal.
    /// </summary>
    /// <param name="goal">The goal to create a plan for.</param>
    /// <returns>The plan.</returns>
    public async Task<Plan> CreatePlanAsync(string goal)
    {
        string relevantFunctionsManual = await this._context.GetFunctionsManualAsync(goal, this.Config);
        this._context.Variables.Set("available_functions", relevantFunctionsManual);

        this._context.Variables.Update(goal);

        var planResult = await this._functionFlowFunction.InvokeAsync(this._context);

        string fullPlan = $"<{FunctionFlowParser.GoalTag}>\n{goal}\n</{FunctionFlowParser.GoalTag}>\n{planResult.Result.Trim()}";

        var plan = fullPlan.ToPlanFromXml(this._context);

        return plan;
    }

    protected PlannerConfig Config { get; }

    private readonly SKContext _context;

    /// <summary>
    /// the function flow semantic function, which takes a goal and creates an xml plan that can be executed
    /// </summary>
    private readonly ISKFunction _functionFlowFunction;

    /// <summary>
    /// The name to use when creating semantic functions that are restricted from the PlannerSkill plans
    /// </summary>
    private const string RestrictedSkillName = "PlannerSkill_Excluded"; // todo we've got copies of this now
}
