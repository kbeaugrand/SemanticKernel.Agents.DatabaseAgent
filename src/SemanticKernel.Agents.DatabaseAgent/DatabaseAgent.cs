using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Services;
using SemanticKernel.Agents.DatabaseAgent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Microsoft.SemanticKernel.Agents;

public sealed class DatabaseKernelAgent : ChatHistoryAgent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseKernelAgent"/> class.
    /// </summary>
    internal DatabaseKernelAgent()
    {

    }

    /// <summary>
    /// Gets the role used for agent instructions.  Defaults to "system".
    /// </summary>
    /// <remarks>
    /// Certain versions of "O*" series (deep reasoning) models require the instructions
    /// to be provided as "developer" role.  Other versions support neither role and
    /// an agent targeting such a model cannot provide instructions.  Agent functionality
    /// will be dictated entirely by the provided plugins.
    /// </remarks>
    public AuthorRole InstructionsRole { get; init; } = AuthorRole.System;

    /// <summary>
    /// Provides a name for the agent, even if it's the identifier.
    /// (since <see cref="Agent.Name"/> allows null)
    /// </summary>
    /// <param name="agent">The target agent</param>
    /// <returns>The agent name as a non-empty string</returns>
    public string GetName() => this.Name ?? this.Id;

    /// <summary>
    /// Provides the display name of the agent.
    /// </summary>
    /// <param name="agent">The target agent</param>
    /// <remarks>
    /// Currently, it's intended for telemetry purposes only.
    /// </remarks>
    public string GetDisplayName() => !string.IsNullOrWhiteSpace(this.Name) ? this.Name! : "UnnamedAgent";

    /// <inheritdoc/>
    public override async IAsyncEnumerable<AgentResponseItem<ChatMessageContent>> InvokeAsync(
        ICollection<ChatMessageContent> messages,
        AgentThread? thread = null,
        AgentInvokeOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistoryAgentThread = await this.EnsureThreadExistsWithMessagesAsync(
            messages,
            thread,
            () => new ChatHistoryAgentThread(),
            cancellationToken).ConfigureAwait(false);

        // Invoke Chat Completion with the updated chat history.
        var chatHistory = new ChatHistory();
        await foreach (var existingMessage in chatHistoryAgentThread.GetMessagesAsync(cancellationToken).ConfigureAwait(false))
        {
            chatHistory.Add(existingMessage);
        }

        string agentName = this.GetDisplayName();
        var invokeResults = this.InternalInvokeAsync(
            agentName,
            chatHistory,
            (m) => this.NotifyThreadOfNewMessage(chatHistoryAgentThread, m, cancellationToken),
            options?.KernelArguments,
            options?.Kernel,
            options?.AdditionalInstructions,
            cancellationToken);

        // Notify the thread of new messages and return them to the caller.
        await foreach (var result in invokeResults.ConfigureAwait(false))
        {
            // Do not add function call related messages as they will already be included in the chat history.
            if (!result.Items.Any(i => i is FunctionCallContent || i is FunctionResultContent))
            {
                await this.NotifyThreadOfNewMessage(chatHistoryAgentThread, result, cancellationToken).ConfigureAwait(false);
            }

            yield return new(result, chatHistoryAgentThread);
        }
    }

    /// <inheritdoc/>
    [Obsolete("Use InvokeAsync with AgentThread instead.")]
    protected override IAsyncEnumerable<ChatMessageContent> InvokeAsync(
        ChatHistory history,
        KernelArguments? arguments = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        string agentName = this.GetDisplayName();

        return this.InternalInvokeAsync(agentName, history, (m) => Task.CompletedTask, arguments, kernel, null, cancellationToken);
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<AgentResponseItem<StreamingChatMessageContent>> InvokeStreamingAsync(
        ICollection<ChatMessageContent> messages,
        AgentThread? thread = null,
        AgentInvokeOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistoryAgentThread = await this.EnsureThreadExistsWithMessagesAsync(
            messages,
            thread,
            () => new ChatHistoryAgentThread(),
            cancellationToken).ConfigureAwait(false);

        // Invoke Chat Completion with the updated chat history.
        var chatHistory = new ChatHistory();
        await foreach (var existingMessage in chatHistoryAgentThread.GetMessagesAsync(cancellationToken).ConfigureAwait(false))
        {
            chatHistory.Add(existingMessage);
        }

        string agentName = this.GetDisplayName();
        var invokeResults = this.InternalInvokeStreamingAsync(
            agentName,
            chatHistory,
            (newMessage) => this.NotifyThreadOfNewMessage(chatHistoryAgentThread, newMessage, cancellationToken),
            options?.KernelArguments,
            options?.Kernel,
            options?.AdditionalInstructions,
            cancellationToken);

        await foreach (var result in invokeResults.ConfigureAwait(false))
        {
            yield return new(result, chatHistoryAgentThread);
        }
    }

    /// <inheritdoc/>
    [Obsolete("Use InvokeStreamingAsync with AgentThread instead.")]
    protected override IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(
        ChatHistory history,
        KernelArguments? arguments = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        string agentName = this.GetDisplayName();

        return this.InternalInvokeStreamingAsync(
                agentName,
                history,
                (newMessage) => Task.CompletedTask,
                arguments,
                kernel,
                null,
                cancellationToken);
    }

    /// <inheritdoc/>
    [Experimental("SKEXP0110")]
    protected override Task<AgentChannel> RestoreChannelAsync(string channelState, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    internal static (IChatCompletionService service, PromptExecutionSettings? executionSettings) GetChatCompletionService(Kernel kernel, KernelArguments? arguments)
    {
        (IChatCompletionService chatCompletionService, PromptExecutionSettings? executionSettings) =
            kernel.ServiceSelector.SelectAIService<IChatCompletionService>(
                kernel,
                arguments?.ExecutionSettings,
                arguments ?? []);

        executionSettings ??= new OpenAIPromptExecutionSettings
        {
            ResponseFormat = "json_object",
        };

        return (chatCompletionService, executionSettings);
    }

    #region private

    private async Task<ChatHistory> SetupAgentChatHistoryAsync(
        IReadOnlyList<ChatMessageContent> history,
        KernelArguments? arguments,
        Kernel kernel,
        string? additionalInstructions,
        CancellationToken cancellationToken)
    {
        ChatHistory chat = new ChatHistory();
        string? instructions = await this.RenderInstructionsAsync(kernel, arguments, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(instructions))
        {
            chat.Add(new ChatMessageContent(this.InstructionsRole, instructions) { AuthorName = this.Name });
        }

        if (!string.IsNullOrWhiteSpace(additionalInstructions))
        {
            chat.Add(new ChatMessageContent(AuthorRole.System, additionalInstructions) { AuthorName = this.Name });
        }

        chat.Add(new ChatMessageContent(AuthorRole.System, $$$"""
                # Output Format

                The output should be a JSON object structured as follows:

                ```json
                {
                    "thinking": "Your step-by-step thought process and reasoning based on the query result.",
                    "answer": "Your final answer in natural language, addressing the question explicitly."
                }
                ```

                # Examples

                **Example 1**

                _Query_: What is the capital of France?  
                _Query Result_: 
                | Country       | Capital     |
                |---------------|-------------|
                | France        | Paris       |
                | Germany       | Berlin      |
                | Spain         | Madrid      |
                | Italy         | Rome        |
                | Portugal      | Lisbon      |
                | Netherlands   | Amsterdam   |
                | Belgium       | Brussels    |
                | Switzerland   | Bern        |
                | Austria       | Vienna      |
                

                _Output_:  
                ```json
                {
                    "thinking": "The query result indicates that the capital of France is listed as 'Paris.' Therefore, based on this data, the capital of France is Paris.",
                    "answer": "The capital of France is Paris."
                }
                ```

                **Example 2**

                _Query_: What is the population of New York City?  
                _Query Result_: 
                | City           | Population |
                |----------------|------------|
                | New York City  | 8,419,600  |
                | Los Angeles    | 3,979,576  |
                | Chicago        | 2,693,976  |
                | Houston        | 2,303,482  |
                | Phoenix        | 1,563,025  |
                

                _Output_:  
                ```json
                {
                    "thinking": "The query result specifies that the population of New York City is 8,419,600. This directly answers the question about its population.",
                    "answer": "The population of New York City is 8,419,600."
                }
                ```
                """) { AuthorName = this.Name });

        chat.AddRange(history);

        return chat;
    }

    private async IAsyncEnumerable<ChatMessageContent> InternalInvokeAsync(
        string agentName,
        ChatHistory history,
        Func<ChatMessageContent, Task> onNewToolMessage,
        KernelArguments? arguments = null,
        Kernel? kernel = null,
        string? additionalInstructions = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        kernel ??= this.Kernel;

        var plugin = kernel.CreatePluginFromType<DatabasePlugin>();

        var data = await plugin["ExecuteQuery"].InvokeAsync(kernel, new KernelArguments
                                                    {
                                                        {"prompt", history.Last(c => c.Role == AuthorRole.User).Content! }
                                                    }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        history.Insert(0, new ChatMessageContent(AuthorRole.System, "Here are the results from the database:") { AuthorName = this.Name });
        history.Insert(1, new ChatMessageContent(AuthorRole.System, data.GetValue<string>()) { AuthorName = this.Name });

        (IChatCompletionService chatCompletionService, PromptExecutionSettings? executionSettings) = GetChatCompletionService(kernel, arguments);

        ChatHistory chat = await this.SetupAgentChatHistoryAsync(history, arguments, kernel, additionalInstructions, cancellationToken).ConfigureAwait(false);

        int messageCount = chat.Count;

        Type serviceType = chatCompletionService.GetType();

        IReadOnlyList<ChatMessageContent> messages =
            await chatCompletionService.GetChatMessageContentsAsync(
                chat,
                executionSettings,
                kernel,
                cancellationToken).ConfigureAwait(false);

        // Capture mutated messages related function calling / tools
        for (int messageIndex = messageCount; messageIndex < chat.Count; messageIndex++)
        {
            ChatMessageContent message = chat[messageIndex];

            message.AuthorName = this.Name;

            history.Add(message);
            await onNewToolMessage(message).ConfigureAwait(false);
        }

        foreach (ChatMessageContent message in messages)
        {
            message.AuthorName = this.Name;

            try
            {
                message.Content = JsonSerializer.Deserialize<AgentResponse>(message.Content!)!.Answer;
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "Failed to deserialize agent response content to AgentResponse. Content: {Content}", message.Content);
            }

            yield return message;
        }
    }

    private async IAsyncEnumerable<StreamingChatMessageContent> InternalInvokeStreamingAsync(
        string agentName,
        ChatHistory history,
        Func<ChatMessageContent, Task> onNewMessage,
        KernelArguments? arguments = null,
        Kernel? kernel = null,
        string? additionalInstructions = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        kernel ??= this.Kernel;

        (IChatCompletionService chatCompletionService, PromptExecutionSettings? executionSettings) = GetChatCompletionService(kernel, arguments);

        ChatHistory chat = await this.SetupAgentChatHistoryAsync(history, arguments, kernel, additionalInstructions, cancellationToken).ConfigureAwait(false);

        int messageCount = chat.Count;

        Type serviceType = chatCompletionService.GetType();

        IAsyncEnumerable<StreamingChatMessageContent> messages =
            chatCompletionService.GetStreamingChatMessageContentsAsync(
                chat,
                executionSettings,
                kernel,
                cancellationToken);

        AuthorRole? role = null;
        StringBuilder builder = new();
        await foreach (StreamingChatMessageContent message in messages.ConfigureAwait(false))
        {
            role = message.Role;
            message.Role ??= AuthorRole.Assistant;
            message.AuthorName = this.Name;

            builder.Append(message.ToString());

            yield return message;
        }

        // Capture mutated messages related function calling / tools
        for (int messageIndex = messageCount; messageIndex < chat.Count; messageIndex++)
        {
            ChatMessageContent message = chat[messageIndex];

            message.AuthorName = this.Name;

            await onNewMessage(message).ConfigureAwait(false);
            history.Add(message);
        }

        // Do not duplicate terminated function result to history
        if (role != AuthorRole.Tool)
        {
            await onNewMessage(new(role ?? AuthorRole.Assistant, builder.ToString()) { AuthorName = this.Name }).ConfigureAwait(false);
            history.Add(new(role ?? AuthorRole.Assistant, builder.ToString()) { AuthorName = this.Name });
        }
    }

    #endregion
}
