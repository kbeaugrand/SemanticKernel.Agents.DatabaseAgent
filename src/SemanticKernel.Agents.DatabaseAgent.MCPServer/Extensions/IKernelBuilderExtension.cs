using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;
using System.ClientModel;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Extensions
{
    internal static class IKernelBuilderExtension
    {
        internal static IKernelBuilder AddCompletionServiceFromConfiguration(this IKernelBuilder builder, IConfiguration configuration, string serviceName, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger(nameof(IKernelBuilderExtension));

            logger.LogInformation("Adding completion service from configuration: {serviceName}", serviceName);

            var service = configuration.GetSection($"services:{serviceName}");

            if (!service.Exists())
            {
                throw new ArgumentException($"Service configuration section is missing for {serviceName}");
            }

            if (string.IsNullOrEmpty(service["Type"]))
            {
                throw new ArgumentException($"Service type is not specified for {serviceName}");
            }

            switch (service["Type"])
            {
                case "AzureOpenAI":
                    var azureConfig = service.Get<AzureOpenAIConfig>();
                    var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(azureConfig.TimeoutInSeconds);
                    return builder.AddAzureOpenAIChatCompletion(azureConfig.Deployment, azureConfig.Endpoint, azureConfig.APIKey, httpClient: client);
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                case "Ollama":
                    var ollamaConfig = service.Get<OllamaConfig>();
                    var ollamaClient = new HttpClient()
                    {
                        BaseAddress = new Uri(ollamaConfig.Endpoint)
                    };
                    ollamaClient.Timeout = TimeSpan.FromSeconds(ollamaConfig.TimeoutInSeconds);
                    return builder.AddOllamaChatCompletion(ollamaConfig.ModelId, httpClient: ollamaClient);
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

            if (!service.Exists())
            {
                throw new ArgumentException($"Service configuration section is missing for {serviceName}");
            }

            if (string.IsNullOrEmpty(service["Type"]))
            {
                throw new ArgumentException($"Service type is not specified for {serviceName}");
            }

            switch (service["Type"])
            {
                case "AzureOpenAI":
                    var azureConfig = service.Get<AzureOpenAIConfig>();

                    return builder.AddAzureOpenAIEmbeddingGenerator(azureConfig.Deployment, azureConfig.Endpoint, azureConfig.APIKey);
                case "Ollama":
                    var ollamaConfig = service.Get<OllamaConfig>();
                    return builder.AddOllamaEmbeddingGenerator(ollamaConfig.ModelId, new Uri(ollamaConfig.Endpoint), null);
                default:
                    throw new ArgumentException("Unknown service type");
            }
        }
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    }
}
