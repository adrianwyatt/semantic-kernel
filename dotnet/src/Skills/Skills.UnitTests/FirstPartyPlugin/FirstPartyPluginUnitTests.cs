// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.SemanticKernel;
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

        var foo = await kernel.ImportFirstPartyPluginAsync(
            "skillName",
            "C:\\src\\adrianwyatt-semantic-kernel\\dotnet\\src\\Skills\\Skills.MS1P\\flux_semantic_kernel.json");
    }
}
