# DBMS Agent for Semantic Kernel

[![Build & Test](https://github.com/kbeaugrand/SemanticKernel.Agents.DatabaseAgent/actions/workflows/build_tests.yml/badge.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.DatabaseAgent/actions/workflows/build_test.yml)
[![Create Release](https://github.com/kbeaugrand/SemanticKernel.Agents.DatabaseAgent/actions/workflows/publish.yml/badge.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.DatabaseAgent/actions/workflows/publish.yml)
[![Version](https://img.shields.io/github/v/release/kbeaugrand/SemanticKernel.Agents.DatabaseAgent)](https://img.shields.io/github/v/release/kbeaugrand/SemanticKernel.Agents.DatabaseAgent)
[![License](https://img.shields.io/github/license/kbeaugrand/SemanticKernel.Agents.DatabaseAgent)](https://img.shields.io/github/v/release/kbeaugrand/SemanticKernel.Agents.DatabaseAgent)

## Overview

The Database Agent for Semantic Kernel is a service that provides a database management system (DBMS) for the Semantic Kernel (NL2SQL). The Agent is responsible for managing the storage and retrieval of data from the Semantic Kernel. 
This built on top of the [Microsoft's Semantic Kernel](https://github.com/microsoft/semantic-kernel) and leverages the [Microsoft's Kernel Memory](https://github.com/microsoft/kernel-memory) service to memorize database schema and relationships to provide a more efficient and accurate database management system.

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation

To use the Database Agent for Semantic Kernel, you must first install the package from NuGet.
```bash
dotnet add package SemanticKernel.Agents.DatabaseAgent
```

### Usage

To use the Database Agent for Semantic Kernel, you must first create an instance of the `DatabaseAgent` class and provide the necessary configuration settings.

```csharp
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using SemanticKernel.Agents.DatabaseAgent;

var memory = new KernelMemoryBuilder()
               ... 
                .Build();

var kernelBuilder = Kernel.CreateBuilder()
                ...
                .Build();

kernelBuilder.Services.AddSingleton<DbConnection>((sp) =>
            {
                // Configure the database connection
                return new SqliteConnection(configuration.GetConnectionString("DefaultConnection"));
            });

var kernel = kernelBuilder.Build();

var agent = await DBMSAgentFactory.CreateAgentAsync(kernel, memory);

var chatHistory = new ChatHistory(question, AuthorRole.User);

// execute the NL2SQL query
var responses = await agent.InvokeWithFunctionCallingAsync(chatHistory)
                                .ConfigureAwait(false);
```

## Contributing

We welcome contributions to enhance this project. Please fork the repository and submit a pull request with your proposed changes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Microsoft's Kernel Memory](https://github.com/microsoft/kernel-memory) for providing the foundational AI service.
- The open-source community for continuous support and contributions.
