using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using static Microsoft.KernelMemory.AzureOpenAIConfig;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Extensions;

internal static class IKernelMemoryBuilderExtension
{
    internal static IKernelMemoryBuilder AddCompletionServiceFromConfiguration(this IKernelMemoryBuilder builder, IConfiguration configuration, string serviceName)
    {
        var service = configuration.GetSection($"services:{serviceName}");

        switch (service["Type"])
        {
            case "AzureOpenAI":
                return builder.WithAzureOpenAITextGeneration(service.Get<AzureOpenAIConfig>());
            default:
                throw new ArgumentException("Unknown service type");
        }
    }

    internal static IKernelMemoryBuilder AddTextEmbeddingFromConfiguration(this IKernelMemoryBuilder builder, IConfiguration configuration, string serviceName)
    {
        var service = configuration.GetSection($"services:{serviceName}");

        switch (service["Type"])
        {
            case "AzureOpenAI":
                return builder.WithAzureOpenAITextEmbeddingGeneration(service.Get<AzureOpenAIConfig>());
            default:
                throw new ArgumentException("Unknown service type");
        }
    }
}
