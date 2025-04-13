using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Extensions
{
    internal static class IKernelBuilderExtension
    {
        internal static IKernelBuilder AddCompletionServiceFromConfiguration(this IKernelBuilder builder, IConfiguration configuration, string serviceName, ILoggerFactory loggerFactory)
        {           
            var logger = loggerFactory.CreateLogger(nameof(IKernelBuilderExtension));

            logger.LogInformation("Adding completion service from configuration: {serviceName}", serviceName);

            var service = configuration.GetSection($"services:{serviceName}");

            if (string.IsNullOrEmpty(service["Type"]))
            {
                logger.LogError("Service type is not specified for {serviceName}", serviceName);
                return builder;
            }

            switch (service["Type"])
            {
                case "AzureOpenAI":
                    var azureConfig = service.Get<AzureOpenAIConfig>();
                    return builder.AddAzureOpenAIChatCompletion(azureConfig.Deployment, azureConfig.Endpoint, azureConfig.APIKey);
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                case "Ollama":
                    var ollamaConfig = service.Get<OllamaConfig>();
                    return builder.AddOllamaChatCompletion(ollamaConfig.ModelId, new Uri(ollamaConfig.Endpoint), null);
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                default:
                    throw new ArgumentException($"Unknown service type: '{service["Type"]}' for {serviceName}");
            }

        }
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        internal static IKernelBuilder AddTextEmbeddingFromConfiguration(this IKernelBuilder builder, IConfiguration configuration, string serviceName, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger(nameof(IKernelBuilderExtension));

            logger.LogInformation("Adding text embedding service from configuration: {serviceName}", serviceName);

            var service = configuration.GetSection($"services:{serviceName}");

            if (string.IsNullOrEmpty(service["Type"]))
            {
                logger.LogError("Service type is not specified for {serviceName}", serviceName);
                return builder;
            }

            switch (service["Type"])
            {
                case "AzureOpenAI":
                    var azureConfig = service.Get<AzureOpenAIConfig>();

                    return builder.AddAzureOpenAITextEmbeddingGeneration(azureConfig.Deployment, azureConfig.Endpoint, azureConfig.APIKey);
                case "Ollama":
                    var ollamaConfig = service.Get<OllamaConfig>();
                    return builder.AddOllamaTextEmbeddingGeneration(ollamaConfig.ModelId, new Uri(ollamaConfig.Endpoint), null);
                default:
                    throw new ArgumentException("Unknown service type");
            }
        }
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    }
}
