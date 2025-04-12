#!/bin/sh

export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/opt/simba/spark/lib

exec /usr/bin/dotnet SemanticKernel.Agents.DatabaseAgent.MCPServer.dll "$@"
