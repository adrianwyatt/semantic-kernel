// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.SkillDefinition;

namespace Microsoft.SemanticKernel.Orchestration;

/// <summary>
/// Standard Semantic Kernel callable plan.
/// Plan is used to create trees of <see cref="ISKFunction"/>s.
/// </summary>
public sealed class Plan : ISKFunction
{
    /// <summary>
    /// Converter for <see cref="ContextVariables"/> to/from JSON.
    /// </summary>
    public class ContextVariablesConverter : JsonConverter<ContextVariables>
    {
        /// <summary>
        /// Read the JSON and convert to ContextVariables
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert from.</param>
        /// <param name="options">The JSON serializer options.</param>
        /// <returns>The deserialized <see cref="ContextVariables"/>.</returns>
        public override ContextVariables Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var keyValuePairs = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<string, string>>>(ref reader, options);
            var context = new ContextVariables();

            foreach (var kvp in keyValuePairs!)
            {
                context.Set(kvp.Key, kvp.Value);
            }

            return context;
        }

        public override void Write(Utf8JsonWriter writer, ContextVariables value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }

    /// <summary>
    /// State of the plan
    /// </summary>
    [JsonPropertyName("state")]
    [JsonConverter(typeof(ContextVariablesConverter))]
    public ContextVariables State { get; } = new();

    /// <summary>
    /// Steps of the plan
    /// </summary>
    [JsonPropertyName("steps")]
    public IReadOnlyList<Plan> Steps => this._steps.AsReadOnly();
    // This doesn't work with json deserialization

    /// <summary>
    /// Named parameters for the function
    /// </summary>
    [JsonPropertyName("named_parameters")]
    [JsonConverter(typeof(ContextVariablesConverter))]
    public ContextVariables NamedParameters { get; set; } = new();

    /// <summary>
    /// Named outputs for the function
    /// </summary>
    [JsonPropertyName("named_outputs")]
    [JsonConverter(typeof(ContextVariablesConverter))]
    public ContextVariables NamedOutputs { get; set; } = new();

    /// <summary>
    /// Gets whether the plan has a next step.
    /// </summary>
    [JsonIgnore]
    public bool HasNextStep => this.NextStepIndex < this.Steps.Count;

    #region ISKFunction implementation

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    [JsonPropertyName("skill_name")]
    public string SkillName { get; set; } = string.Empty;

    /// <inheritdoc/>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IsSemantic { get; internal set; } = false;

    /// <inheritdoc/>
    [JsonIgnore]
    public CompleteRequestSettings RequestSettings { get; internal set; } = new();

    #endregion ISKFunction implementation

    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a goal description.
    /// </summary>
    /// <param name="goal">The goal of the plan used as description.</param>
    public Plan(string goal)
    {
        this.Description = goal;
        this.SkillName = this.GetType().FullName;
        this.Name = goal;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a goal description and steps.
    /// </summary>
    /// <param name="goal">The goal of the plan used as description.</param>
    /// <param name="steps">The steps to add.</param>
    public Plan(string goal, params ISKFunction[] steps) : this(goal)
    {
        this.AddSteps(steps);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a goal description and steps.
    /// </summary>
    /// <param name="goal">The goal of the plan used as description.</param>
    /// <param name="steps">The steps to add.</param>
    public Plan(string goal, params Plan[] steps) : this(goal)
    {
        this.AddSteps(steps);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a function.
    /// </summary>
    /// <param name="function">The function to execute.</param>
    public Plan(ISKFunction function)
    {
        this.SetFunction(function);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a function and steps.
    /// </summary>
    /// <param name="name">The name of the plan.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="description">The description of the plan.</param>
    /// <param name="nextStepIndex">The index of the next step.</param>
    /// <param name="state">The state of the plan.</param>
    /// <param name="namedParameters">The named parameters of the plan.</param>
    /// <param name="namedOutputs">The named outputs of the plan.</param>
    /// <param name="steps">The steps of the plan.</param>
    [JsonConstructor]
    public Plan(string name, string skillName, string description, int nextStepIndex, ContextVariables state, ContextVariables namedParameters, ContextVariables namedOutputs,
        IReadOnlyList<Plan> steps)
    {
        this.Name = name;
        this.SkillName = skillName;
        this.Description = description;
        this.NextStepIndex = nextStepIndex;
        this.State = state;
        this.NamedParameters = namedParameters;
        this.NamedOutputs = namedOutputs;
        this._steps.Clear();
        this.AddSteps(steps.ToArray());
    }

    /// <summary>
    /// Adds one or more existing plans to the end of the current plan as steps.
    /// </summary>
    /// <param name="steps">The plans to add as steps to the current plan.</param>
    /// <remarks>
    /// When you add a plan as a step to the current plan, the steps of the added plan are executed after the steps of the current plan have completed.
    /// </remarks>
    public void AddSteps(params Plan[] steps)
    {
        this._steps.AddRange(steps);
    }

    /// <summary>
    /// Adds one or more new steps to the end of the current plan.
    /// </summary>
    /// <param name="steps">The steps to add to the current plan.</param>
    /// <remarks>
    /// When you add a new step to the current plan, it is executed after the previous step in the plan has completed. Each step can be a function call or another plan.
    /// </remarks>
    public void AddSteps(params ISKFunction[] steps)
    {
        this._steps.AddRange(steps.Select(step => new Plan(step)));
    }

    /// <summary>
    /// Runs the next step in the plan using the provided kernel instance and variables.
    /// </summary>
    /// <param name="kernel">The kernel instance to use for executing the plan.</param>
    /// <param name="variables">The variables to use for the execution of the plan.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the execution of the plan.</param>
    /// <returns>A task representing the asynchronous execution of the plan's next step.</returns>
    /// <remarks>
    /// This method executes the next step in the plan using the specified kernel instance and context variables. The context variables contain the necessary information for executing the plan, such as the memory, skills, and logger. The method returns a task representing the asynchronous execution of the plan's next step.
    /// </remarks>
    public Task<Plan> RunNextStepAsync(IKernel kernel, ContextVariables variables, CancellationToken cancellationToken = default)
    {
        var context = new SKContext(
            variables,
            kernel.Memory,
            kernel.Skills,
            kernel.Log,
            cancellationToken);
        return this.InvokeNextStepAsync(context);
    }

    #region ISKFunction implementation

    /// <inheritdoc/>
    public FunctionView Describe()
    {
        // TODO - Eventually, we should be able to describe a plan and it's expected inputs/outputs
        return this.Function?.Describe() ?? new();
    }

    /// <inheritdoc/>
    public Task<SKContext> InvokeAsync(string input, SKContext? context = null, CompleteRequestSettings? settings = null, ILogger? log = null,
        CancellationToken? cancel = null)
    {
        context ??= new SKContext(new ContextVariables(input), null!, null, log ?? NullLogger.Instance, cancel ?? CancellationToken.None);
        return this.InvokeAsync(context, settings, log, cancel);
    }

    /// <inheritdoc/>
    public async Task<SKContext> InvokeAsync(SKContext? context = null, CompleteRequestSettings? settings = null, ILogger? log = null,
        CancellationToken? cancel = null)
    {
        context ??= new SKContext(new ContextVariables(), null!, null, log ?? NullLogger.Instance, cancel ?? CancellationToken.None);

        if (this.Function is not null)
        {
            var result = await this.Function.InvokeAsync(context, settings, log, cancel);

            if (result.ErrorOccurred)
            {
                result.Log.LogError(
                    result.LastException,
                    "Something went wrong in plan step {0}.{1}:'{2}'", this.SkillName, this.Name, context.LastErrorDescription);
                return result;
            }

            context.Variables.Update(result.Result.ToString());
        }
        else
        {
            // loop through steps and execute until completion
            while (this.HasNextStep)
            {
                var functionContext = context;

                AddVariablesToContext(this.State, functionContext);

                await this.InvokeNextStepAsync(functionContext);

                context.Variables.Update(this.State.ToString());
            }
        }

        return context;
    }

    /// <inheritdoc/>
    public ISKFunction SetDefaultSkillCollection(IReadOnlySkillCollection skills)
    {
        return this.Function is null
            ? throw new NotImplementedException()
            : this.Function.SetDefaultSkillCollection(skills);
    }

    /// <inheritdoc/>
    public ISKFunction SetAIService(Func<ITextCompletion> serviceFactory)
    {
        return this.Function is null
            ? throw new NotImplementedException()
            : this.Function.SetAIService(serviceFactory);
    }

    /// <inheritdoc/>
    public ISKFunction SetAIConfiguration(CompleteRequestSettings settings)
    {
        return this.Function is null
            ? throw new NotImplementedException()
            : this.Function.SetAIConfiguration(settings);
    }

    #endregion ISKFunction implementation

    /// <summary>
    /// Invoke the next step of the plan
    /// </summary>
    /// <param name="context">Context to use</param>
    /// <returns>The updated plan</returns>
    /// <exception cref="KernelException">If an error occurs while running the plan</exception>
    public async Task<Plan> InvokeNextStepAsync(SKContext context)
    {
        if (this.HasNextStep)
        {
            var step = this.Steps[this.NextStepIndex];

            var functionVariables = this.GetNextStepVariables(context.Variables, step);
            var functionContext = new SKContext(functionVariables, context.Memory, context.Skills, context.Log, context.CancellationToken);

            var keysToIgnore = functionVariables.Select(x => x.Key).ToList(); // when will there be new keys added? that's output kind of?

            var result = await step.InvokeAsync(functionContext);

            if (result.ErrorOccurred)
            {
                throw new KernelException(KernelException.ErrorCodes.FunctionInvokeError,
                    $"Error occurred while running plan step: {context.LastErrorDescription}", context.LastException);
            }

            #region Update State

            foreach (var key in functionVariables.Select(x => x.Key))
            {
                if (!keysToIgnore.Contains(key, StringComparer.InvariantCultureIgnoreCase) && functionVariables.Get(key, out var value))
                {
                    this.State.Set(key, value);
                }
            }

            this.State.Update(result.Result.Trim());

            foreach (var item in step.NamedOutputs)
            {
                if (string.IsNullOrEmpty(item.Key) || item.Key.ToUpperInvariant() == "INPUT" || string.IsNullOrEmpty(item.Value))
                {
                    continue;
                }

                if (item.Key.ToUpperInvariant() == "RESULT")
                {
                    this.State.Set(item.Value, result.Result.Trim());
                }
                else if (result.Variables.Get(item.Key, out var value))
                {
                    this.State.Set(item.Value, value);
                }
            }
            // if (string.IsNullOrEmpty(sequentialPlan.OutputKey))
            // {
            //     _ = this.State.Update(result.Result.Trim());
            // }
            // else
            // {
            //     this.State.Set(sequentialPlan.OutputKey, result.Result.Trim());
            // }

            // _ = this.State.Update(result.Result.Trim());
            // if (!string.IsNullOrEmpty(sequentialPlan.OutputKey))
            // {
            //     this.State.Set(sequentialPlan.OutputKey, result.Result.Trim());
            // }

            // if (!string.IsNullOrEmpty(sequentialPlan.ResultKey))
            // {
            //     _ = this.State.Get(SkillPlan.ResultKey, out var resultsSoFar);
            //     this.State.Set(SkillPlan.ResultKey,
            //         string.Join(Environment.NewLine + Environment.NewLine, resultsSoFar, result.Result.Trim()));
            // }

            #endregion Update State

            this.NextStepIndex++;
        }

        return this;
    }

    /// <summary>
    /// Get JSON representation of the plan.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    /// <summary>
    /// To help with reading plans from <see cref="Orchestration.ContextVariables"/>.
    /// </summary>
    /// <param name="json">JSON string representation of aPlan</param>
    /// <param name="context">The context to use for function registrations.</param>
    /// <returns>An instance of a Plan object.</returns>
    public static Plan FromJson(string json, SKContext context)
    {
        var plan = JsonSerializer.Deserialize<Plan>(json, new JsonSerializerOptions() { IncludeFields = true }) ?? new Plan(string.Empty);

        plan = GetRegisteredFunctions(plan, context);

        return plan;
    }

    private static Plan GetRegisteredFunctions(Plan plan, SKContext context)
    {
        if (plan.Steps.Count == 0)
        {
            if (context.IsFunctionRegistered(plan.SkillName, plan.Name, out var skillFunction))
            {
                Verify.NotNull(skillFunction, nameof(skillFunction));
                plan.SetFunction(skillFunction);
            }
        }
        else
        {
            foreach (var step in plan.Steps)
            {
                GetRegisteredFunctions(step, context);
            }
        }

        return plan;
    }

    private ContextVariables GetNextStepVariables(ContextVariables variables, Plan step)
    {
        // If passing to a PLAN, do not use this.Description
        var defaultInput = step.Steps.Count > 0 ? string.Empty : this.Description ?? string.Empty;

        // Initialize function-scoped ContextVariables
        // Default input should be the Input from the SKContext, or the Input from the Plan.State, or the Plan.Goal
        var planInput = string.IsNullOrEmpty(variables.Input) ? this.State.Input : variables.Input;
        var functionInput = string.IsNullOrEmpty(planInput) ? defaultInput : planInput;
        var functionVariables = new ContextVariables(functionInput);

        // When I execute a plan, it has a State, ContextVariables, and a Goal

        // Priority for functionVariables is:
        // - NamedParameters (pull from State by a key value)
        // - Parameters (pull from ContextVariables by name match, backup from State by name match)

        var functionParameters = step.Describe();
        foreach (var param in functionParameters.Parameters)
        {
            if (variables.Get(param.Name, out var value) && !string.IsNullOrEmpty(value))
            {
                functionVariables.Set(param.Name, value);
            }
            else if (this.State.Get(param.Name, out value) && !string.IsNullOrEmpty(value))
            {
                functionVariables.Set(param.Name, value);
            }
        }

        foreach (var item in step.NamedParameters)
        {
            if (item.Value.StartsWith("$", StringComparison.InvariantCultureIgnoreCase))
            {
                var attrValues = item.Value.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                var attrValueList = new List<string>();
                foreach (var attrValue in attrValues)
                {
                    var attr = attrValue.TrimStart('$');
                    if (variables.Get(attr, out var value) && !string.IsNullOrEmpty(value))
                    {
                        attrValueList.Add(value);
                    }
                    else if (this.State.Get(attr, out value) && !string.IsNullOrEmpty(value))
                    {
                        attrValueList.Add(value);
                    }
                }

                functionVariables.Set(item.Key, string.Concat(attrValueList));
            }
            else
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    functionVariables.Set(item.Key, item.Value);
                }
                else if (variables.Get(item.Value, out var value) && !string.IsNullOrEmpty(value))
                {
                    functionVariables.Set(item.Key, value);
                }
                else if (this.State.Get(item.Value, out value) && !string.IsNullOrEmpty(value))
                {
                    functionVariables.Set(item.Key, value);
                }
            }
        }

        return functionVariables;
    }

    private void SetFunction(ISKFunction function)
    {
        this.Function = function;
        this.Name = function.Name;
        this.SkillName = function.SkillName;
        this.Description = function.Description;
        this.IsSemantic = function.IsSemantic;
        this.RequestSettings = function.RequestSettings;
    }

    /// <summary>
    /// Internal constant string representing the plan key.
    /// </summary>
    internal const string PlanKey = "PLAN__PLAN__KEY";

    [JsonPropertyName("next_step_index")]
    public int NextStepIndex { get; internal set; } = 0;

    private ISKFunction? Function { get; set; } = null;

    private readonly List<Plan> _steps = new();

    private static void AddVariablesToContext(ContextVariables vars, SKContext context)
    {
        // Loop through State and add anything missing to functionContext
        foreach (var item in vars)
        {
            if (!context.Variables.ContainsKey(item.Key))
            {
                context.Variables.Set(item.Key, item.Value);
            }
        }
    }
}
