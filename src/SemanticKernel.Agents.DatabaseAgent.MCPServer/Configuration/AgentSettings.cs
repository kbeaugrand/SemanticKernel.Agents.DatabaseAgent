using SemanticKernel.Agents.DatabaseAgent.QualityAssurance.Filters;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;

internal class AgentSettings
{
    public required KernelSettings Kernel { get; set; }

    public required QualityAssuranceFilterOptions QualityAssurance { get; set; }

    public required TransportSettings Transport { get; set; }
}

internal sealed class TransportSettings
{
    internal enum TransportType
    {
        Stdio,
        Http
    }

    public TransportType Kind { get; set; } = TransportType.Stdio;
}
