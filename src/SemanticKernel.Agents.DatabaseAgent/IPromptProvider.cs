namespace SemanticKernel.Agents.DatabaseAgent;

public class AgentPromptConstants
{
    public const string AgentDescriptionGenerator = nameof(AgentDescriptionGenerator);
    public const string AgentInstructionsGenerator = nameof(AgentInstructionsGenerator);
    public const string AgentNameGenerator = nameof(AgentNameGenerator);
    public const string ExplainTable = nameof(ExplainTable);
    public const string WriteSQLQuery = nameof(WriteSQLQuery);
    public const string ExtractTableName = nameof(ExtractTableName);
}

public interface IPromptProvider
{
    public string ReadPrompt(string promptName);
}