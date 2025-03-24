using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SemanticKernel.Agents.DatabaseAgent.Filters;

namespace SemanticKernel.Agents.DatabaseAgent.QualityAssurance.Filters;

public class QueryRelevancyFilter : IQueryExecutionFilter
{
    private ILogger<QueryRelevancyFilter> Logger { get; }

    private QualityAssuranceFilterOptions Options { get; }

    public QueryRelevancyFilter(IOptions<QualityAssuranceFilterOptions> options, ILoggerFactory? loggerFactory = null)
    {
        Logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<QueryRelevancyFilter>();
        Options = options.Value;
    }

    public async Task OnQueryExecutionAsync(QueryExecutionContext context, Func<QueryExecutionContext, Task> next)
    {
        if (!this.Options.EnableQueryRelevancyFilter)
        {
            await next(context);
            return;
        }

        QueryRelevancyEvaluator evaluator = new QueryRelevancyEvaluator(context.Kernel);

        var relevancy = await evaluator.EvaluateAsync(context.Prompt, context.TableDefinitions, context.SQLQuery);

        if (relevancy < Options.QueryRelevancyThreshold)
        {
            Logger.LogWarning("Query relevancy is below threshold. Skipping query execution.");
            return;
        }

        await next(context);
    }
}
