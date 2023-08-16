// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.FirstPartyPlugin;
using Xunit;

namespace SemanticKernel.Skills.UnitTests.FirstPartyPlugin;
public class FirstPartyPluginUnitTests
{
    [Fact]
    public async Task FooTestAsync()
    {
        IKernel kernel = new KernelBuilder()
            .Build();

        IDictionary<string, ISKFunction> functions = await kernel.ImportFirstPartyPluginAsync(
            "skillName",
            "C:\\src\\adrianwyatt-semantic-kernel\\dotnet\\src\\Skills\\Skills.MS1P\\flux_zillow.json");

        var context = new SKContext();
        context.Variables.Set("state", "reasoning");
        context.Variables.Update("Brainstorm ideas on responsible AI principles.");
        context = await functions.First().Value.InvokeAsync(context);
    }
}
