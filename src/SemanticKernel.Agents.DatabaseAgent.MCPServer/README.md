# Database Agent MCP Server

The Database Agent MCP Server is a server that listens for incoming connections from the Database Agent and processes the messages sent by the Database Agent. The Database Agent MCP Server is responsible for processing the messages sent by the Database Agent and executing the appropriate actions based on the message type.

## Installation

To install the MCP server, you first should install the .NET Core SDK. 
You can download the .NET Core SDK from the following link: https://dotnet.microsoft.com/download

After installing the .NET Core SDK, you can install the MCP server tool by running the following command:

```bash
dotnet tool install --global SemanticKernel.Agents.DatabaseAgent.MCPServer
```

## Usage

To start the MCP server, you can run the following command:

```bash
modelcontextprotocol-database-agent --*options*
```

### Options

The following options are available:

. `--agent:QualityAssurance:EnableQueryRelevancyFilter`  
    - **Description**: Enables or disables the query relevancy filter in the quality assurance process.  
    - **Type**: `bool`  
    - **Default**: `true`  
    - **Example**: `--agent:QualityAssurance:EnableQueryRelevancyFilter=false`  

. `--database:ConnectionString`  
    - **Description**: The connection string for connecting to the database.  
    - **Type**: `string`  
    - **Example**: `--database:ConnectionString="Data Source=northwind.db;Mode=ReadWrite"`  

. `--database:Provider`  
    - **Description**: Specifies the database provider (e.g., SQLite, SQL Server, etc.).  
    - **Type**: `string`  
    - **Example**: `--database:Provider=sqlite`  

. `--memory:Kind`  
    - **Description**: Defines the kind of memory to be used for the kernel (e.g., Volatile, Persistent).  
    - **Type**: `string`  
    - **Example**: `--memory:Kind=Volatile`  
   
. `--memory:Path`  
    - **Description**: Specifies the path to the memory files.
    - **Type**: `string`  
    - **Example**: `--memory:Path=xxx`  

. `--memory:Completion`  
    - **Description**: Specifies the model used for the completion task.  
    - **Type**: `string`  
    - **Example**: `--memory:Completion=gpt-4o-mini`  

. `--memory:Embedding`  
    - **Description**: Specifies the embedding model used for text embeddings.  
    - **Type**: `string`  
    - **Example**: `--memory:Embedding=text-embedding-ada-002`  

. `--kernel:Completion`  
    - **Description**: Defines the completion model used by the kernel.  
    - **Type**: `string`  
    - **Example**: `--kernel:Completion=gpt-4o-mini`  

. `--kernel:Embedding`  
    - **Description**: Specifies the embedding model for the kernel's embedding operations.  
    - **Type**: `string`  
    - **Example**: `--kernel:Embedding=text-embedding-ada-002`  

. `--services:gpt-4o-mini:Type`  
    - **Description**: Specifies the type of service (e.g., AzureOpenAI, OpenAI).  
    - **Type**: `string`  
    - **Example**: `--services:gpt-4o-mini:Type=AzureOpenAI`  

. `--services:gpt-4o-mini:Endpoint`  
     - **Description**: The endpoint URL for the GPT-4o-mini service.  
     - **Type**: `string`  
     - **Example**: `--services:gpt-4o-mini:Endpoint="https://xxx.openai.azure.com/"`  

. `--services:gpt-4o-mini:Auth`  
     - **Description**: Specifies the authentication method for the GPT-4o-mini service.  
     - **Type**: `string`  
     - **Example**: `--services:gpt-4o-mini:Auth=APIKey`  

. `--services:gpt-4o-mini:APIKey`  
     - **Description**: The API key used for authentication to the GPT-4o-mini service.  
     - **Type**: `string`  
     - **Example**: `--services:gpt-4o-mini:APIKey="xxx"`  

. `--services:gpt-4o-mini:Deployment`  
     - **Description**: Specifies the deployment name for the GPT-4o-mini service.  
     - **Type**: `string`  
     - **Example**: `--services:gpt-4o-mini:Deployment="gpt-4o-mini"`  

. `--services:text-embedding-ada-002:Type`  
     - **Description**: Specifies the type of service for the text embedding (e.g., AzureOpenAI).  
     - **Type**: `string`  
     - **Example**: `--services:text-embedding-ada-002:Type=AzureOpenAI`  

. `--services:text-embedding-ada-002:Endpoint`  
     - **Description**: The endpoint URL for the text-embedding-ada-002 service.  
     - **Type**: `string`  
     - **Example**: `--services:text-embedding-ada-002:Endpoint="https://xxx.openai.azure.com/"`  

. `--services:text-embedding-ada-002:Auth`  
     - **Description**: Specifies the authentication method for the text-embedding-ada-002 service.  
     - **Type**: `string`  
     - **Example**: `--services:text-embedding-ada-002:Auth=APIKey`  

. `--services:text-embedding-ada-002:APIKey`  
     - **Description**: The API key used for authentication to the text-embedding-ada-002 service.  
     - **Type**: `string`  
     - **Example**: `--services:text-embedding-ada-002:APIKey="xxx"`  

. `--services:text-embedding-ada-002:Deployment`  
     - **Description**: Specifies the deployment name for the text-embedding-ada-002 service.  
     - **Type**: `string`  
     - **Example**: `--services:text-embedding-ada-002:Deployment="text-embedding-ada-002"`

## Supported database providers

The following database providers are supported:

- `sqlite`: SQLite database provider
- `sqlserver`: SQL Server database provider
- `mysql`: MySQL database provider
- `postgresql`: PostgreSQL database provider
- `oracle`: Oracle database provider
- `oledb`: OLE DB database provider`

## Contributing

Contributions are welcome! For more information, please see the [CONTRIBUTING](../../CONTRIBUTING.md) file.

## License

This project is licensed under the MIT License. See the [LICENSE](../../LICENSE.md) file for details.