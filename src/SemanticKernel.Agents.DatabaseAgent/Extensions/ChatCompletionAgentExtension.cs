using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernel.Agents.DatabaseAgent;

public static class ChatCompletionAgentExtension
{
    public static Task<IReadOnlyList<ChatMessageContent>> InvokeWithFunctionCallingAsync(this ChatCompletionAgent agent, ChatHistory history)
    {
        var chatCompletionService = agent.Kernel.Services.GetRequiredService<IChatCompletionService>();

        if (chatCompletionService is null)
        {
            throw new InvalidOperationException("The chat completion service is not registered in the kernel services.");
        }

        if (chatCompletionService is not OpenAIChatCompletionService && chatCompletionService is not AzureOpenAIChatCompletionService)
        {
            throw new InvalidOperationException("The chat completion service is not an instance of OpenAIChatCompletionService or AzureOpenAIChatCompletionService.");
        }

        return chatCompletionService.GetChatMessageContentsAsync(history, kernel: agent.Kernel, executionSettings: new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true)
        });
    }
}

