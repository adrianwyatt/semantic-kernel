﻿// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.SemanticKernel.SkillDefinition;

/// <summary>
/// Attribute required to register native functions into the kernel.
/// The registration is required by the prompt templating engine and by the pipeline generator (aka planner).
/// The quality of the description affects the planner ability to reason about complex tasks.
/// The description is used both with LLM prompts and embedding comparisons.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SKFunctionAttribute : Attribute
{
    /// <summary>
    /// Function description, to be used by the planner to auto-discover functions.
    /// </summary>
    public string Description { get; }

    public bool IsSafe { get; }

    /// <summary>
    /// Tag a C# function as a native function available to SK.
    /// </summary>
    /// <param name="description">Function description, to be used by the planner to auto-discover functions.</param>
    /// <param name="isSafe">Whether the function is safe to execute. False when the function may attempt to perform an action that will modify data.</param>
    public SKFunctionAttribute(string description, bool isSafe = true)
    {
        this.Description = description;
    }

    
}
