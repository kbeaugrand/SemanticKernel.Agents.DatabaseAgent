using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Extensions
{
    internal static class IKernelBuilderExtension
    {
        internal static IKernelBuilder AddCompletionServiceFromConfiguration(this IKernelBuilder builder, IConfiguration configuration, string serviceName)
        {
            var service = configuration.GetSection($"services:{serviceName}");

            switch (service["Type"])
            {
                case "AzureOpenAI":
                    var config = service.Get<AzureOpenAIConfig>();
                    return builder.AddAzureOpenAIChatCompletion(config.Deployment, config.Endpoint, config.APIKey);
                default:
                    throw new ArgumentException($"Unknown service type: '{service["Type"]}' for {serviceName}");
            }

        }

        internal static IKernelBuilder AddTextEmbeddingFromConfiguration(this IKernelBuilder builder, IConfiguration configuration, string serviceName)
        {
            var service = configuration.GetSection($"services:{serviceName}");

            switch (service["Type"])
            {
                case "AzureOpenAI":
                    var config = service.Get<AzureOpenAIConfig>();

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    return builder.AddAzureOpenAITextEmbeddingGeneration(config.Deployment, config.Endpoint, config.APIKey);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                default:
                    throw new ArgumentException("Unknown service type");
            }
        }
    }
}
