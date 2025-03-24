namespace SemanticKernel.Agents.DatabaseAgent.Filters;

public interface IQueryExecutionFilter
{
    public Task OnQueryExecutionAsync(QueryExecutionContext context, Func<QueryExecutionContext, Task> next);
}
