// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using SemanticKernel.IntegrationTests.Fakes;
using SemanticKernel.IntegrationTests.TestSettings;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.IntegrationTests.CoreSkills;

public sealed class PlannerSkillTests : IDisposable
{
    public PlannerSkillTests(ITestOutputHelper output)
    {
        this._logger = new XunitLogger<object>(output);
        this._testOutputHelper = new RedirectOutput(output);

        // Load configuration
        this._configuration = new ConfigurationBuilder()
            .AddJsonFile(path: "testsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(path: "testsettings.development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<PlannerSkillTests>()
            .Build();
    }

    [Theory]
    [InlineData("Write a poem or joke and send it in an e-mail to Kai.", "_GLOBAL_FUNCTIONS_", "SendEmailAsync", "_GLOBAL_FUNCTIONS_", "GetEmailAddressAsync")]
    public async Task CreatePlanDefaultTestAsync(string prompt, params string[] expectedSkillsAndFunctions)
    {
        // Arrange
        AzureOpenAIConfiguration? azureOpenAIConfiguration = this._configuration.GetSection("AzureOpenAI").Get<AzureOpenAIConfiguration>();
        Assert.NotNull(azureOpenAIConfiguration);

        IKernel target = Kernel.Builder
            .WithLogger(this._logger)
            .Configure(config =>
            {
                config.AddAzureTextCompletionService(
                    serviceId: azureOpenAIConfiguration.ServiceId,
                    deploymentName: azureOpenAIConfiguration.DeploymentName,
                    endpoint: azureOpenAIConfiguration.Endpoint,
                    apiKey: azureOpenAIConfiguration.ApiKey);

                config.SetDefaultTextCompletionService(azureOpenAIConfiguration.ServiceId);
            })
            .Build();

        var writerSkill = TestHelpers.GetSkill("WriterSkill", target);

        var emailSkill = target.ImportSkill(new EmailSkillFake());

        var plannerSKill = target.ImportSkill(new PlannerSkill(target));

        // Act
        ContextVariables variables = new(prompt);
        SKContext actual = await target.RunAsync(variables, plannerSKill["CreatePlan"]);

        // Assert
        Assert.NotNull(actual);
        Assert.True(actual.TryGetPlan(out Plan? plan));
        Assert.NotNull(plan);
        Assert.Equal(prompt, plan.Description);

        // Check that the plan contains the expected skills and functions.
        for (int i = 0; i < expectedSkillsAndFunctions.Length; i += 2)
        {
            string? skillName = expectedSkillsAndFunctions[i];
            string? functionName = expectedSkillsAndFunctions[i + 1];
            Assert.Contains(plan.Steps,
                s => s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase) && s.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData("Write a poem or joke and send it in an e-mail to Kai.", "_GLOBAL_FUNCTIONS_", "SendEmailAsync", "_GLOBAL_FUNCTIONS_", "GetEmailAddressAsync")]
    public async Task CreatePlanWithEmbeddingsTestAsync(string prompt, params string[] expectedSkillsAndFunctions)
    {
        // Arrange
        AzureOpenAIConfiguration? azureOpenAIConfiguration = this._configuration.GetSection("AzureOpenAI").Get<AzureOpenAIConfiguration>();
        Assert.NotNull(azureOpenAIConfiguration);

        AzureOpenAIConfiguration? azureOpenAIEmbeddingsConfiguration = this._configuration.GetSection("AzureOpenAIEmbeddings").Get<AzureOpenAIConfiguration>();
        Assert.NotNull(azureOpenAIEmbeddingsConfiguration);

        IKernel target = Kernel.Builder
            .WithLogger(this._logger)
            .Configure(config =>
            {
                config.AddAzureTextCompletionService(
                    serviceId: azureOpenAIConfiguration.ServiceId,
                    deploymentName: azureOpenAIConfiguration.DeploymentName,
                    endpoint: azureOpenAIConfiguration.Endpoint,
                    apiKey: azureOpenAIConfiguration.ApiKey);

                config.AddAzureTextEmbeddingGenerationService(
                    serviceId: azureOpenAIEmbeddingsConfiguration.ServiceId,
                    deploymentName: azureOpenAIEmbeddingsConfiguration.DeploymentName,
                    endpoint: azureOpenAIEmbeddingsConfiguration.Endpoint,
                    apiKey: azureOpenAIEmbeddingsConfiguration.ApiKey);

                config.SetDefaultTextCompletionService(azureOpenAIConfiguration.ServiceId);
            })
            .WithMemoryStorage(new VolatileMemoryStore())
            .Build();

        // Import all sample skills available for demonstration purposes.
        TestHelpers.ImportSampleSkills(target);

        var emailSkill = target.ImportSkill(new EmailSkillFake());

        var plannerSKill = target.ImportSkill(new PlannerSkill(target));

        // Act
        ContextVariables variables = new(prompt);
        variables.Set(PlannerSkill.Parameters.ExcludedSkills, "IntentDetectionSkill,FunSkill,CodingSkill");
        variables.Set(PlannerSkill.Parameters.ExcludedFunctions, "EmailTo");
        variables.Set(PlannerSkill.Parameters.IncludedFunctions, "Continue");
        variables.Set(PlannerSkill.Parameters.MaxRelevantFunctions, "19");
        variables.Set(PlannerSkill.Parameters.RelevancyThreshold, "0.5");
        SKContext actual = await target.RunAsync(variables, plannerSKill["CreatePlan"]).ConfigureAwait(true);

        // Assert
        Assert.Empty(actual.LastErrorDescription);
        Assert.False(actual.ErrorOccurred);

        this._logger.LogTrace("RESULT: {0}", actual.Result);

        Assert.True(actual.TryGetPlan(out Plan? plan));
        Assert.NotNull(plan);
        Assert.NotEmpty(plan.Steps);
        Assert.Equal(prompt, plan.Description);

        // loop through params and check if they are in the plan
        for (int i = 0; i < expectedSkillsAndFunctions.Length; i += 2)
        {
            string? skillName = expectedSkillsAndFunctions[i];
            string? functionName = expectedSkillsAndFunctions[i + 1];
            Assert.Contains(plan.Steps,
                s => s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase) && s.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
        }
    }

    private readonly XunitLogger<object> _logger;
    private readonly RedirectOutput _testOutputHelper;
    private readonly IConfigurationRoot _configuration;

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PlannerSkillTests()
    {
        this.Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._logger.Dispose();
            this._testOutputHelper.Dispose();
        }
    }
}
