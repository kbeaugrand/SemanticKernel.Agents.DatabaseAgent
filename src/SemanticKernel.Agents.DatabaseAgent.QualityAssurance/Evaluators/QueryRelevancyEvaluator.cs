using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Services;
using SemanticKernel.Agents.DatabaseAgent.Internals;
using SemanticKernel.Agents.DatabaseAgent.QualityAssurance.Evaluators;
using System.Numerics.Tensors;
using System.Text.Json;

namespace SemanticKernel.Agents.DatabaseAgent.QualityAssurance;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class QueryRelevancyEvaluator
{
    private readonly Kernel kernel;

    private KernelFunction ExtractQuestion => KernelFunctionFactory.CreateFromPrompt(new EmbeddedPromptProvider().ReadPrompt("QuestionExtraction"), new OpenAIPromptExecutionSettings
    {
        Temperature = 9.99999993922529E-09,
        TopP = 9.99999993922529E-09,
        Seed = 0L,
        ResponseFormat = "json_object",
    }, "ExtractQuestion");

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

        var evaluation = JsonSerializer.Deserialize<QueryRelevancyEvaluation>(result.GetValue<string>()!)!;
        var embeddedQueries = new List<string>
        {
            prompt
        };

        embeddedQueries.AddRange(evaluation.Questions);

        IList<ReadOnlyMemory<float>> embeddings = await kernel.GetRequiredService<ITextEmbeddingGenerationService>()
                                                                .GenerateEmbeddingsAsync(embeddedQueries, kernel)
                                                                    .ConfigureAwait(false);

        var promptEmbedding = embeddings.First();

        return embeddings.Skip(1).Select(c => TensorPrimitives.CosineSimilarity(promptEmbedding.Span, c.Span)).Max();
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
