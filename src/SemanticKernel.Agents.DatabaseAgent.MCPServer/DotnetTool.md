# Database Agent MCP Server

## Install the MCP Server as a .NET Core Tool

To install the MCP server as .NET Core Tool, you first should install the .NET Core SDK. 
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