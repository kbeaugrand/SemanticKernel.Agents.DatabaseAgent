using System.Reflection;

namespace SemanticKernel.Agents.DatabaseAgent.Internals;

internal sealed class EmbeddedPromptProvider : IPromptProvider
{
    public string ReadPrompt(string promptName)
    {
        var resourceStream = Assembly.GetCallingAssembly()
                                             .GetManifestResourceStream($"{Assembly.GetCallingAssembly().GetName().Name}.Prompts.{promptName}.md");

        using var reader = new StreamReader(resourceStream!);
        var text = reader.ReadToEnd();
        return text;
    }
}
