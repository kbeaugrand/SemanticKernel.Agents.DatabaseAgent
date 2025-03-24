using SemanticKernel.Agents.DatabaseAgent.Filters;
using SemanticKernel.Agents.DatabaseAgent.QualityAssurance.Filters;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
    public static IServiceCollection UseDatabaseAgentQualityAssurance(
        this IServiceCollection services,
        Action<QualityAssuranceFilterOptions>? opts = null)
    {
        if (opts is null)
        {
            services.Configure<QualityAssuranceFilterOptions>(_ => { });
        }
        else
        {
            services.Configure(opts);
        }

        services.AddTransient<IQueryExecutionFilter, QueryRelevancyFilter>();

        return services;
    }
}
