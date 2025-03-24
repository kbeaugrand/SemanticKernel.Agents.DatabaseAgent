using Microsoft.SemanticKernel;

namespace SemanticKernel.Agents.DatabaseAgent.Filters;

public class QueryExecutionContext
{
    public Kernel Kernel { get; init; }

    public string TableDefinitions {  get; init; }

    public string Prompt { get; init; }

    public string SQLQuery { get; init; }

    public CancellationToken CancellationToken { get; init; }

    internal QueryExecutionContext(Kernel kernel, string prompt, string tableDefinitions, string sqlQuery, CancellationToken cancellationToken)
    {
        Kernel = kernel;
        Prompt = prompt;
        TableDefinitions = tableDefinitions;
        SQLQuery = sqlQuery;
        CancellationToken = cancellationToken;
    }
}
