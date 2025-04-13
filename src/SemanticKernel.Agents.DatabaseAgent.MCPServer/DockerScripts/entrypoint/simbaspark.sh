#!/bin/bash

export LD_LIBRARY_PATH=/opt/simba/spark/lib/64:$LD_LIBRARY_PATH

/usr/bin/dotnet SemanticKernel.Agents.DatabaseAgent.MCPServer.dll
