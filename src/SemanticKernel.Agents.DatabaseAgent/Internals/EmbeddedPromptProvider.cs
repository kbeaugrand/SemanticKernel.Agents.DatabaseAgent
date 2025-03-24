using Microsoft.KernelMemory.Prompts;
using System.Reflection;

namespace SemanticKernel.Agents.DatabaseAgent.Internals;

internal static class EmbeddedPromptProvider
{
    internal static string ReadPrompt(string promptName)
    {
        var resourceStream = Assembly.GetCallingAssembly()
                                             .GetManifestResourceStream($"{Assembly.GetCallingAssembly().GetName().Name}.Prompts.{promptName}.md");

        using var reader = new StreamReader(resourceStream!);
        var text = reader.ReadToEnd();
        return text;
    }
}
