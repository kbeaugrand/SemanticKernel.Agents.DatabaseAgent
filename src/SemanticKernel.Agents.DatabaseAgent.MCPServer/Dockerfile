# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble AS base
WORKDIR /app

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /
COPY . .
RUN dotnet restore "./src/SemanticKernel.Plugins.DatabaseAgent.sln"
RUN dotnet build "./src/SemanticKernel.Plugins.DatabaseAgent.sln" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR ./src/SemanticKernel.Agents.DatabaseAgent.MCPServer
RUN dotnet publish "./SemanticKernel.Agents.DatabaseAgent.MCPServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final

ARG RUNTIME_SDK_INSTALLER=void
ARG VERSION=
ARG OS=
ARG BUILD=

# Install dependencies
RUN apt-get update && \
	apt-get install -y --no-install-recommends wget unzip dos2unix \
	&& rm -rf /var/lib/apt/lists/*

COPY ./src/SemanticKernel.Agents.DatabaseAgent.MCPServer/DockerScripts/install/${RUNTIME_SDK_INSTALLER}.sh /tmp/
RUN chmod +x /tmp/${RUNTIME_SDK_INSTALLER}.sh && \
	/tmp/${RUNTIME_SDK_INSTALLER}.sh ${VERSION} ${OS} ${BUILD} && \
	rm -rf /tmp/${RUNTIME_SDK_INSTALLER}.sh

COPY --from=publish /app/publish .
#COPY ./src/SemanticKernel.Agents.DatabaseAgent.MCPServer/DockerScripts/entrypoint/${RUNTIME_SDK_INSTALLER}.sh /app/
#
#RUN mv /app/${RUNTIME_SDK_INSTALLER}.sh /app/entrypoint.sh && \
    #dos2unix /app/entrypoint.sh && \
    #chmod +x /app/entrypoint.sh
	#
USER $APP_UID
WORKDIR /app
ENTRYPOINT ["/usr/bin/dotnet", "SemanticKernel.Agents.DatabaseAgent.MCPServer.dll"]