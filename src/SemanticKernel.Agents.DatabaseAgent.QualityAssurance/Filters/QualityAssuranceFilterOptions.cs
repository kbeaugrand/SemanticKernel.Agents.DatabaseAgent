namespace SemanticKernel.Agents.DatabaseAgent.QualityAssurance.Filters;

public class QualityAssuranceFilterOptions
{
    public bool EnableQueryRelevancyFilter { get; set; } = true;

    public float QueryRelevancyThreshold { get; set; } = .9f;
}
