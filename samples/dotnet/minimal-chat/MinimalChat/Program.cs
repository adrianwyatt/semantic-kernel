// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;

IKernel kernel = new KernelBuilder()
    //.WithOpenAIChatCompletionService("gpt-3.5-turbo", Environment.GetEnvironmentVariable("OPENAI_APIKEY")!)
    //.WithAzureChatCompletionService("gpt-35-turbo", "https://{YOUR_INSTANCE}.openai.azure.com/", Environment.GetEnvironmentVariable("AZUREOPENAI_APIKEY")!)
    .WithAzureChatCompletionService("gpt-35-turbo", "https://lightspeed-team-shared-openai.openai.azure.com/", Environment.GetEnvironmentVariable("AZUREOPENAI_APIKEY")!)
    .Build();

IChatCompletion chat = kernel.GetService<IChatCompletion>();
ChatHistory history = chat.CreateNewChat("You are a helpful assistant.");

while (true)
{
    Console.Write("Input: ");
    history.AddUserMessage(Console.ReadLine()!);
    history.AddAssistantMessage(await chat.GenerateMessageAsync(history));
    Console.WriteLine($"{history.Last().Role}: {history.Last().Content}");
}
