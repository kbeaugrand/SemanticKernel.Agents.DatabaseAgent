# Database Agent MCP Server

## Install the MCP Server as a Docker Image

The database agent MCP server can be run as a Docker image. This allows you to run the server in a containerized environment, making it easy to deploy and manage to expose it SSE (Server-Sent Events) and HTTP endpoints. 

To run the MCP server as a Docker image, you can use the following command:
```bash

docker run -it --rm \
  -p 8080:8080 \
  -e DATABASE_PROVIDER=sqlite \
  -e DATABASE_CONNECTION_STRING="Data Source=northwind.db;Mode=ReadWrite" \
  -e MEMORY_KIND=Volatile \
  -e KERNEL_COMPLETION=gpt-4o-mini \
  -e KERNEL_EMBEDDING=text-embedding-ada-002 \
  -e SERVICES_GPT_4O_MINI_TYPE=AzureOpenAI \
  -e SERVICES_GPT_4O_MINI_ENDPOINT=https://xxx.openai.azure.com/ \
  -e SERVICES_GPT_4O_MINI_AUTH=APIKey \
  -e SERVICES_GPT_4O_MINI_API_KEY=xxx \
  -e SERVICES_GPT_4O_MINI_DEPLOYMENT=gpt-4o-mini \
  -e SERVICES_TEXT_EMBEDDING_ADA_002_TYPE=AzureOpenAI \
  -e SERVICES_TEXT_EMBEDDING_ADA_002_ENDPOINT=https://xxx.openai.azure.com/ \
  -e SERVICES_TEXT_EMBEDDING_ADA_002_AUTH=APIKey \
  -e SERVICES_TEXT_EMBEDDING_ADA_002_API_KEY=xxx \
  -e SERVICES_TEXT_EMBEDDING_ADA_002_DEPLOYMENT=text-embedding-ada-002 \
  ghcr.io/kbeaugrand/database-mcp-server
```