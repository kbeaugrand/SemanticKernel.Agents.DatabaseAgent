namespace SemanticKernel.Agents.DatabaseAgent;

public sealed class DatabasePluginOptions
{
    public int TopK { get; set; } = 5;

    public int? MaxTokens { get; set; } = 4096;

    public double? Temperature { get; set; } = .1E-9;

    public double? TopP { get; set; } = .1E-9;

    public double? MinScore { get; set; } = 0.5;
}
