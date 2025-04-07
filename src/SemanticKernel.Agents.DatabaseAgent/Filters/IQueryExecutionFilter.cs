namespace SemanticKernel.Agents.DatabaseAgent.Filters;

public interface IQueryExecutionFilter
{
    public Task<(bool filtered, string message)> OnQueryExecutionAsync(QueryExecutionContext context, Func<QueryExecutionContext, Task<(bool, string)>> next);
}
