﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Planning.Planners;
using SemanticKernel.IntegrationTests.Fakes;
using SemanticKernel.IntegrationTests.TestSettings;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.IntegrationTests.Planning;

public sealed class FunctionFlowPlannerTests : IDisposable
{
    public FunctionFlowPlannerTests(ITestOutputHelper output)
    {
        this._logger = new XunitLogger<object>(output);
        this._testOutputHelper = new RedirectOutput(output);

        // Load configuration
        this._configuration = new ConfigurationBuilder()
            .AddJsonFile(path: "testsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(path: "testsettings.development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<FunctionFlowPlannerTests>()
            .Build();
    }

    [Theory]
    [InlineData("Write a poem and send it in an e-mail to Kai.", "SendEmailAsync", "_GLOBAL_FUNCTIONS_")]
    public async Task CreatePlanFunctionFlowAsync(string prompt, string expectedFunction, string expectedSkill)
    {
        // Arrange
        IKernel target = this.InitializeKernel();

        var planner = new FunctionFlowPlanner(target);

        // Act
        var plan = await planner.CreatePlanAsync(prompt);
        // Assert
        Assert.Contains(
            plan.Steps,
            step =>
                step.Name.Equals(expectedFunction, StringComparison.OrdinalIgnoreCase) &&
                step.SkillName.Equals(expectedSkill, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Write a poem or joke and send it in an e-mail to Kai.", "SendEmailAsync", "_GLOBAL_FUNCTIONS_")]
    public async Task CreatePlanGoalRelevantAsync(string prompt, string expectedFunction, string expectedSkill)
    {
        // Arrange
        IKernel target = this.InitializeKernel(true);

        var planner = new FunctionFlowPlanner(target, new PlannerConfig() { RelevancyThreshold = 0.70 });

        // Act
        var plan = await planner.CreatePlanAsync(prompt);
        // Assert
        Assert.Contains(
            plan.Steps,
            step =>
                step.Name.Equals(expectedFunction, StringComparison.OrdinalIgnoreCase) &&
                step.SkillName.Equals(expectedSkill, StringComparison.OrdinalIgnoreCase));
    }

    private IKernel InitializeKernel(bool useEmbeddings = false)
    {
        AzureOpenAIConfiguration? azureOpenAIConfiguration = this._configuration.GetSection("AzureOpenAI").Get<AzureOpenAIConfiguration>();
        Assert.NotNull(azureOpenAIConfiguration);

        AzureOpenAIConfiguration? azureOpenAIEmbeddingsConfiguration = this._configuration.GetSection("AzureOpenAIEmbeddings").Get<AzureOpenAIConfiguration>();
        Assert.NotNull(azureOpenAIEmbeddingsConfiguration);

        var builder = Kernel.Builder
            .WithLogger(this._logger)
            .Configure(config =>
            {
                config.AddAzureTextCompletionService(
                    serviceId: azureOpenAIConfiguration.ServiceId,
                    deploymentName: azureOpenAIConfiguration.DeploymentName,
                    endpoint: azureOpenAIConfiguration.Endpoint,
                    apiKey: azureOpenAIConfiguration.ApiKey);

                if (useEmbeddings)
                {
                    config.AddAzureTextEmbeddingGenerationService(
                        serviceId: azureOpenAIEmbeddingsConfiguration.ServiceId,
                        deploymentName: azureOpenAIEmbeddingsConfiguration.DeploymentName,
                        endpoint: azureOpenAIEmbeddingsConfiguration.Endpoint,
                        apiKey: azureOpenAIEmbeddingsConfiguration.ApiKey);
                }

                config.SetDefaultTextCompletionService(azureOpenAIConfiguration.ServiceId);
            });

        if (useEmbeddings)
        {
            builder = builder.WithMemoryStorage(new VolatileMemoryStore());
        }

        var kernel = builder.Build();

        // Import all sample skills available for demonstration purposes.
        TestHelpers.ImportSampleSkills(kernel);

        _ = kernel.ImportSkill(new EmailSkillFake());
        return kernel;
    }

    private readonly ILogger _logger;
    private readonly RedirectOutput _testOutputHelper;
    private readonly IConfigurationRoot _configuration;

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~FunctionFlowPlannerTests()
    {
        this.Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (this._logger is IDisposable ld)
            {
                ld.Dispose();
            }

            this._testOutputHelper.Dispose();
        }
    }
}
