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

### Example

Here is an example of how to start the MCP server with a SQLite database and Azure OpenAI services:

```bash
modelcontextprotocol-database-agent \
  --database:Provider=sqlite \
  --database:ConnectionString="Data Source=northwind.db;Mode=ReadWrite" \
  --memory:Kind=Volatile \
  --kernel:Completion=gpt-4o-mini \
  --kernel:Embedding=text-embedding-ada-002 \
  --services:gpt-4o-mini:Type=AzureOpenAI \
  --services:gpt-4o-mini:Endpoint=https://xxx.openai.azure.com/ \
  --services:gpt-4o-mini:Auth=APIKey \
  --services:gpt-4o-mini:APIKey=xxx \
  --services:gpt-4o-mini:Deployment=gpt-4o-mini \
  --services:text-embedding-ada-002:Type=AzureOpenAI \
  --services:text-embedding-ada-002:Endpoint=https://xxx.openai.azure.com/ \
  --services:text-embedding-ada-002:Auth=APIKey \
  --services:text-embedding-ada-002:APIKey=xxx \
  --services:text-embedding-ada-002:Deployment=text-embedding-ada-002
```

### Options

The following options are available

#### Global options

`--database:Provider`  
    - **Description**: Specifies the database provider (e.g., SQLite, SQL Server, etc.).  
    - **Type**: `string`  
    - **Example**: `--database:Provider=sqlite` 

`--database:ConnectionString`  
    - **Description**: The connection string for connecting to the database.  
    - **Type**: `string`  
    - **Example**: `--database:ConnectionString="Data Source=northwind.db;Mode=ReadWrite"`  

#### Supported database providers

The following database providers are supported:

- `sqlite`: SQLite database provider
- `sqlserver`: SQL Server database provider
- `mysql`: MySQL database provider
- `postgresql`: PostgreSQL database provider
- `oracle`: Oracle database provider
- `oledb`: OLE DB database provider


#### Memory options

Memory options are used to configure the memory settings for the kernel. 
As a default, the memory is set to `Volatile`, which means that the memory is not persisted and will be lost when the kernel is stopped.

`--memory:Kind`  
    - **Description**: Defines the kind of memory to be used for the kernel (e.g., Volatile).  
    - **Type**: `string`  
    - **Example**: `--memory:Kind=Volatile`

`--memory:Dimensions`
    - **Description**: The number of dimensions for the memory vectors.  This is only used when the memory kind is set to a persistent memory provider.
    - **Type**: `int`  
    - **Example**: `--memory:Dimensions=1536`  

You can also set the memory to persist the data in a database. At the moment, the only supported database provider is `sqlite` and `Qdrant` but more providers will be added in the future.

##### SQLite options

`--memory:ConnectionString`  
    - **Description**: The connection string for connecting to the SQLite database.  
    - **Type**: `string`  
    - **Example**: `--memory:SQLite:ConnectionString="Data Source=northwind.db;Mode=ReadWrite"`

##### Qdrant options

`--memory:Host`
    - **Description**: The host name or IP address of the Qdrant server.  
    - **Type**: `string`  
    - **Example**: `--memory:Host="localhost"` 

`--memory:Port`  
    - **Description**: The port number of the Qdrant server.  
    - **Type**: `int`  
    - **Example**: `--memory:Port=6333` 

`--memory:Https`
    - **Description**: Specifies whether to use HTTPS for the connection.  
    - **Type**: `bool`  
    - **Default**: `false`  
    - **Example**: `--memory:Https=true` 

`--memory:ApiKey`  
    - **Description**: The API key for authenticating with the Qdrant server.  
    - **Type**: `string`  
    - **Example**: `--memory:ApiKey="xxx"`

#### Kernel options

. `--kernel:Completion`  
    - **Description**: Defines the completion model used by the kernel and configured in the services section.  
    - **Type**: `string`  
    - **Example**: `--kernel:Completion=gpt-4o-mini`  

. `--kernel:Embedding`  
    - **Description**: Specifies the embedding model for the kernel's embedding operations and configured in the services section.  
    - **Type**: `string`  
    - **Example**: `--kernel:Embedding=text-embedding-ada-002`  

#### Services options

The services options are used to configure the services that are used by the kernel. At this time Azure Open AI and Ollama are supported as backend but more providers will be added in the future.

`--services:<model>:Type`  
    - **Description**: Specifies the type of service to be used (e.g., AzureOpenAI, Ollama).  
    - **Type**: `string`  
    - **Example**: `--services:gpt-4o-mini:Type=AzureOpenAI`  

##### Azure Open AI

`--services:<model>:Endpoint`  
    - **Description**: The endpoint URL for the service.  
    - **Type**: `string`  
    - **Example**: `--services:gpt-4o-mini:Endpoint="https://xxx.openai.azure.com/"` 

`--services:<model>:Auth`  
    - **Description**: The authentication method for the service (e.g., APIKey).  
    - **Type**: `string`  
    - **Example**: `--services:gpt-4o-mini:Auth=APIKey`  

`--services:<model>:APIKey`  
    - **Description**: The API key for authenticating with the service.  
    - **Type**: `string`  
    - **Example**: `--services:gpt-4o-mini:APIKey="xxx"`  

`--services:<model>:Deployment`  
    - **Description**: The deployment name for the service.  
    - **Type**: `string`  
    - **Example**: `--services:gpt-4o-mini:Deployment="gpt-4o-mini"`

##### Ollama

`--services:<model>:ModelId`  
    - **Description**: The model name for the Ollama service.  
    - **Type**: `string`  
    - **Example**: `--services:qwen2.5-coder:ModelId="qwen2.5-coder:latest"`  

`--services:<model>:Host`  
    - **Description**: The host name or IP address of the Ollama server.  
    - **Type**: `string`  
    - **Example**: `--services:qwen2.5-coder:Endpoint="http://localhost:11434"`  

## Quality assurance

You can set the quality assurance settings by adding these specific configuration options:

`--agent:QualityAssurance:EnableQueryRelevancyFilter`  
    - **Description**: Enables or disables the query relevancy filter in the quality assurance process.  
    - **Type**: `bool`  
    - **Default**: `true`  
    - **Example**: `--agent:QualityAssurance:EnableQueryRelevancyFilter=false`  
    
`--agent:QualityAssurance:QueryRelevancyThreshold`  
    - **Description**: Enables or disables the query relevancy filter in the quality assurance process.  
    - **Type**: `bool`  
    - **Default**: `true`  
    - **Example**: `--agent:QualityAssurance:QueryRelevancyThreshold=0.9`  


## Contributing

Contributions are welcome! For more information, please see the [CONTRIBUTING](../../CONTRIBUTING.md) file.

## License

This project is licensed under the MIT License. See the [LICENSE](../../LICENSE.md) file for details.