using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernel.Agents.DatabaseAgent.Internals
{
    internal static class PromptExecutionSettingsHelper
    {
        internal static PromptExecutionSettings GetPromptExecutionSettings<T>() => new OpenAIPromptExecutionSettings
        {
            MaxTokens = 4096,
            Temperature = .1E-9,
            TopP = .1E-9,
            Seed = 0L,
            ResponseFormat = AIJsonUtilities.CreateJsonSchema(typeof(T)),
        };
    }
}
