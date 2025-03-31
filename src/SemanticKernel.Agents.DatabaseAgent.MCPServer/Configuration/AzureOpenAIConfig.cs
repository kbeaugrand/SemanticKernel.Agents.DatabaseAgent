using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration
{
    internal class AzureOpenAIConfig
    {
        [Required]
        public string Deployment { get; set; } = string.Empty;

        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public string APIKey { get;  set; }
    }
}
