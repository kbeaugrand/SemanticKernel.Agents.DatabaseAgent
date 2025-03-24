# DBMS Agent Quality Assurance for Semantic Kernel

## Overview

The Database Agent for Semantic Kernel is a service that provides a database management system (DBMS) for the Semantic Kernel (NL2SQL). The Agent is responsible for managing the storage and retrieval of data from the Semantic Kernel. 
This built on top of the [Microsoft's Semantic Kernel](https://github.com/microsoft/semantic-kernel) and leverages the [Microsoft's Kernel Memory](https://github.com/microsoft/kernel-memory) service to memorize database schema and relationships to provide a more efficient and accurate database management system.

## Getting Started

Using LLM agents to write and execute its own queries into a database might lead to risks such as unintended data exposure, security vulnerabilities, and inefficient query execution, potentially compromising system integrity and compliance requirements.
To mitigate these risks, the Database Agent for Semantic Kernel provides a set of quality assurance features to ensure the safety and reliability of the queries executed by the agent.

### Additional Configuration

First, you must add the ``QualityAssurance`` package for DatabaseAgent to your project.

```bash
dotnet add package SemanticKernel.Agents.DatabaseAgent.QualityAssurance
```

Next, you must configure the quality insurance settings for the Database Agent.
```csharp
    kernelBuilder.Services.UseDatabaseAgentQualityAssurance(opts =>
                            {
                                opts.EnableQueryRelevancyFilter = true;
                                opts.QueryRelevancyThreshold = .8f;
                            });
```

### Quality Assurance Features

The Database Agent for Semantic Kernel provides the following quality assurance features:
`QueryRelevancyFilter`: Ensures that only relevant queries are executed by the agent. The filter uses LLM to generate the description of the query that is intended to be executed, then compute the cosine similarity between the user prompt and the generated description. If the similarity score is below the threshold, the query is rejected.

### Create a custom quality assurance filter

You can create a custom quality assurance filter by implementing the `IQueryExecutionFilter` interface and registering it with the DI container.
```csharp
kernelBuilder.Services.AddTransient<IQueryExecutionFilter, QueryRelevancyFilter>();

public class CustomQueryExecutionFilter : IQueryExecutionFilter
{
    public async Task OnQueryExecutionAsync(QueryExecutionContext context, Func<QueryExecutionContext, Task> next)
    {
        // Implement custom query execution logic
        return Task.FromResult(true);
    }
}
```

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Contributing

We welcome contributions to enhance this project. Please fork the repository and submit a pull request with your proposed changes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Microsoft's Kernel Memory](https://github.com/microsoft/kernel-memory) for providing the foundational AI service.
- The open-source community for continuous support and contributions.
