// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable once CheckNamespace // Extension methods

namespace Microsoft.SemanticKernel.Orchestration.Extensions;

/// <summary>
/// Class that holds extension methods for ContextVariables.
/// </summary>
public static class ContextVariablesExtensions
{
    /// <summary>
    /// Simple extension method to turn a string into a <see cref="ContextVariables"/> instance.
    /// </summary>
    /// <param name="text">The text to transform</param>
    /// <returns>An instance of <see cref="ContextVariables"/></returns>
    public static ContextVariables ToContextVariables(this string text)
    {
        return new ContextVariables(text);
    }

    /// <summary>
    /// Simple extension method to update a <see cref="ContextVariables"/> instance with a Plan instance.
    /// </summary>
    /// <param name="vars">The variables to update</param>
    /// <param name="plan">The Plan to update the <see cref="ContextVariables"/> with</param>
    /// <returns>The updated <see cref="ContextVariables"/></returns>
    public static ContextVariables UpdateWithPlanEntry(this ContextVariables vars, Plan plan)
    {
        vars.Update(plan.ToJson());
        vars.Set(Plan.PlanKey, plan.ToJson());
        return vars;
    }
}
