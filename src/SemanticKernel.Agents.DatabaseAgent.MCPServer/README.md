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
dotnet modelcontextprotocol-database-agent --Provider <provider> --ConnectionString <connection-string>
```


The following options are available:

- `--Provider`: The provider to use for the database connection. The supported providers are `sqlite`, `mysql`, and `sqlserver`.
- `--ConnectionString`: The connection string to use for the database connection.

## License

This project is licensed under the MIT License. See the [LICENSE](../../LICENSE.md) file for details.