using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Services;
using SemanticKernel.Agents.DatabaseAgent.Internals;
using System.Numerics.Tensors;

namespace SemanticKernel.Agents.DatabaseAgent.QualityAssurance;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class QueryRelevancyEvaluator
{
    private readonly Kernel kernel;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private KernelFunction ExtractQuestion => KernelFunctionFactory.CreateFromPrompt(EmbeddedPromptProvider.ReadPrompt("QuestionExtraction"), new OpenAIPromptExecutionSettings
    {
        Temperature = 9.99999993922529E-09,
        TopP = 9.99999993922529E-09,
        Seed = 0L,
    }, "ExtractQuestion");
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    public QueryRelevancyEvaluator(Kernel kernel)
    {
        this.kernel = kernel;
    }

    public async Task<float> EvaluateAsync(string prompt, string tableDefinitions, string query)
    {
        (var chatCompletionService, var executionSettings) = GetChatCompletionService(kernel, null);

        var result = await ExtractQuestion.InvokeAsync(kernel, new KernelArguments(executionSettings)
            {
                { "tablesDefinition", tableDefinitions },
                { "query", query }
            });

        IList<ReadOnlyMemory<float>> embeddings = await kernel.GetRequiredService<ITextEmbeddingGenerationService>()
                                                                .GenerateEmbeddingsAsync([prompt, result.GetValue<string>()!], kernel)
                                                                    .ConfigureAwait(false);

        return TensorPrimitives.CosineSimilarity(embeddings.First().Span, embeddings.Last().Span);
    }

    internal static (IChatCompletionService service, PromptExecutionSettings?) GetChatCompletionService(Kernel kernel, KernelArguments? arguments)
    {
        (IChatCompletionService chatCompletionService, PromptExecutionSettings? executionSettings) =
            kernel.ServiceSelector.SelectAIService<IChatCompletionService>(
                kernel,
                arguments?.ExecutionSettings,
                arguments ?? []);

        return (chatCompletionService, executionSettings);
    }
}

#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
