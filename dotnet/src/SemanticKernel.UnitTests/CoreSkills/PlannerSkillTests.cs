// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Orchestration.Extensions;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.SkillDefinition;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.UnitTests.CoreSkills;

public class PlannerSkillTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    private const string FunctionFlowResultText = @"<plan>
  <function.math.simplify input=""x^2 = 2"" />
</plan>
    ";

    private const string GoalText = "Solve the equation x^2 = 2.";

    public PlannerSkillTests(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;
        this._testOutputHelper.WriteLine("Tests initialized");
    }

    [Fact]
    public void ItCanBeInstantiated()
    {
        // Arrange
        var kernel = KernelBuilder.Create();
        var factory = new Mock<Func<IKernel, ITextCompletion>>();
        kernel.Config.AddTextCompletionService("test", factory.Object);

        // Act - Assert no exception occurs
        _ = new PlannerSkill(kernel);
    }

    [Fact]
    public void ItCanBeImported()
    {
        // Arrange
        var kernel = KernelBuilder.Create();
        var factory = new Mock<Func<IKernel, ITextCompletion>>();
        kernel.Config.AddTextCompletionService("test", factory.Object);

        // Act - Assert no exception occurs e.g. due to reflection
        _ = kernel.ImportSkill(new PlannerSkill(kernel), "planner");
    }

    [Fact]
    public async Task ItCanCreateSkillPlanAsync()
    {
        // Arrange
        var functions = new List<(string name, string skillName, string description, bool isSemantic, string result)>()
        {
            (string.Empty, "PlannerSkill_Excluded", "_functionFlowFunction", true, FunctionFlowResultText),
            ("simplify", "math", "Solve an equation", true, "x = ±√2"),
        };
        this.CreateKernelAndFunctionCreateMocks(functions, out var kernel);

        var plannerSkill = new PlannerSkill(kernel);
        var context = this.CreateSKContext(kernel);

        // Act
        context = await plannerSkill.CreatePlanAsync(GoalText, context);

        // Assert
        var plan = Plan.FromJson(context.Result, context);
        Assert.NotNull(plan);
        Assert.Equal(GoalText, plan.Description);
        Assert.Equal(string.Empty, plan.State.ToString());
        Assert.Equal(1, plan.Steps.Count);
        Assert.Equal("simplify", plan.Steps[0].Name);
        Assert.Equal("math", plan.Steps[0].SkillName);
    }

    [Fact]
    public async Task ItCanExecutePlanInputAsync()
    {
        // Arrange
        var resultString = "x = ±√2";
        this.CreateKernelAndFunctionExecuteMocks(resultString, out var kernel, out var mockFunction);

        var plan = new Plan(GoalText);
        plan.AddSteps(new Plan(mockFunction.Object));

        var plannerSkill = new PlannerSkill(kernel);
        var context = this.CreateSKContext(kernel, new ContextVariables(plan.ToJson()));

        Assert.Equal(GoalText, plan.Description);
        Assert.Equal(string.Empty, plan.State.ToString());

        plan = Plan.FromJson(context.Result, context);

        Assert.Equal(GoalText, plan.Description);
        Assert.Equal(string.Empty, plan.State.ToString());

        // Act
        context = await plannerSkill.ExecutePlanAsync(context);

        // Assert
        plan = Plan.FromJson(context.Result, context);
        Assert.NotNull(plan);
        Assert.Equal(GoalText, plan.Description);
        Assert.Equal(resultString, plan.State.ToString());
    }

    [Fact]
    public async Task ItCanExecutePlanAsync()
    {
        // Arrange
        var resultString = "x = ±√2";
        this.CreateKernelAndFunctionExecuteMocks(resultString, out var kernel, out var mockFunction);

        var plan = new Plan(GoalText);
        plan.AddSteps(new Plan(mockFunction.Object));

        var plannerSkill = new PlannerSkill(kernel);
        var variables = new ContextVariables();
        variables.UpdateWithPlanEntry(plan);
        var context = this.CreateSKContext(kernel, variables);

        Assert.Equal(GoalText, plan.Description);
        Assert.Equal(string.Empty, plan.State.ToString());

        plan = Plan.FromJson(context.Result, context);

        Assert.Equal(GoalText, plan.Description);
        Assert.Equal(string.Empty, plan.State.ToString());

        // Act
        context = await plannerSkill.ExecutePlanAsync(context);

        // Assert
        plan = Plan.FromJson(context.Result, context);
        Assert.NotNull(plan);
        Assert.Equal(GoalText, plan.Description);
        Assert.Equal(resultString, plan.State.ToString());
    }

    private Mock<IKernel> CreateKernelMock(
        out Mock<ISemanticTextMemory> semanticMemoryMock,
        out Mock<IReadOnlySkillCollection> mockSkillCollection,
        out Mock<ILogger> mockLogger)
    {
        semanticMemoryMock = new Mock<ISemanticTextMemory>();
        mockSkillCollection = new Mock<IReadOnlySkillCollection>();
        mockLogger = new Mock<ILogger>();

        var kernelMock = new Mock<IKernel>();
        kernelMock.SetupGet(k => k.Skills).Returns(mockSkillCollection.Object);
        kernelMock.SetupGet(k => k.Log).Returns(mockLogger.Object);
        kernelMock.SetupGet(k => k.Memory).Returns(semanticMemoryMock.Object);

        return kernelMock;
    }

    private SKContext CreateSKContext(
        IKernel kernel,
        ContextVariables? variables = null,
        CancellationToken cancellationToken = default)
    {
        return new SKContext(variables ?? new ContextVariables(), kernel.Memory, kernel.Skills, kernel.Log, cancellationToken);
    }

    private static Mock<ISKFunction> CreateMockFunction(FunctionView functionView, string result = "")
    {
        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.Describe()).Returns(functionView);
        mockFunction.Setup(x => x.Name).Returns(functionView.Name);
        mockFunction.Setup(x => x.SkillName).Returns(functionView.SkillName);
        return mockFunction;
    }

    private void CreateKernelAndFunctionExecuteMocks(string resultString, out IKernel kernel, out Mock<ISKFunction> mockFunction)
    {
        var kernelMock = this.CreateKernelMock(out _, out var skillMock, out _);
        kernel = kernelMock.Object;
        mockFunction = new Mock<ISKFunction>();
        var result = this.CreateSKContext(kernel);
        result.Variables.Update(resultString);
        mockFunction.Setup(f => f.InvokeAsync(
                It.IsAny<SKContext?>(), It.IsAny<CompleteRequestSettings?>(),
                It.IsAny<ILogger?>(),
                It.IsAny<CancellationToken?>()))
            .ReturnsAsync(result);

        skillMock.Setup(s => s.HasSemanticFunction(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        skillMock.Setup(s => s.GetSemanticFunction(It.IsAny<string>(), It.IsAny<string>())).Returns(mockFunction.Object);
        kernelMock.Setup(k => k.RunAsync(It.IsAny<ContextVariables>(), It.IsAny<ISKFunction>())).ReturnsAsync(this.CreateSKContext(kernel));
        kernelMock.Setup(k => k.RegisterSemanticFunction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SemanticFunctionConfig>()))
            .Returns(mockFunction.Object);
    }

    private void CreateKernelAndFunctionCreateMocks(List<(string name, string skillName, string description, bool isSemantic, string result)> functions,
        out IKernel kernel)
    {
        var kernelMock = this.CreateKernelMock(out _, out var skills, out _);
        kernel = kernelMock.Object;

        // For Create
        kernelMock.Setup(k => k.CreateNewContext()).Returns(this.CreateSKContext(kernel));

        var functionsView = new FunctionsView();
        foreach (var (name, skillName, description, isSemantic, resultString) in functions)
        {
            var functionView = new FunctionView(name, skillName, description, new List<ParameterView>(), isSemantic, true);
            var mockFunction = CreateMockFunction(functionView);
            functionsView.AddFunction(functionView);

            var result = this.CreateSKContext(kernel);
            result.Variables.Update(resultString);
            mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, null, null))
                .ReturnsAsync(result);

            if (string.IsNullOrEmpty(name))
            {
                kernelMock.Setup(x => x.RegisterSemanticFunction(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<SemanticFunctionConfig>()
                )).Returns(mockFunction.Object);
            }
            else
            {
                if (isSemantic)
                {
                    skills.Setup(x => x.GetSemanticFunction(It.Is<string>(s => s == skillName), It.Is<string>(s => s == name)))
                        .Returns(mockFunction.Object);
                    skills.Setup(x => x.HasSemanticFunction(It.Is<string>(s => s == skillName), It.Is<string>(s => s == name))).Returns(true);
                }
                else
                {
                    skills.Setup(x => x.GetNativeFunction(It.Is<string>(s => s == skillName), It.Is<string>(s => s == name)))
                        .Returns(mockFunction.Object);
                    skills.Setup(x => x.HasNativeFunction(It.Is<string>(s => s == skillName), It.Is<string>(s => s == name))).Returns(true);
                }
            }
        }

        skills.Setup(x => x.GetFunctionsView(It.IsAny<bool>(), It.IsAny<bool>())).Returns(functionsView);
    }
}
