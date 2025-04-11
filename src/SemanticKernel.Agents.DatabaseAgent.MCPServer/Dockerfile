# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:8.0-noble AS base
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
ARG VERSION=2.9.1
ARG OS=Debian-64bit
ARG BUILD=1001

# Install dependencies
RUN apt-get update && \
	apt-get install -y --no-install-recommends wget unzip \
	&& rm -rf /var/lib/apt/lists/*

COPY ./src/SemanticKernel.Agents.DatabaseAgent.MCPServer/DockerScripts/${RUNTIME_SDK_INSTALLER}.sh /tmp/
RUN chmod +x /tmp/${RUNTIME_SDK_INSTALLER}.sh && \
	/tmp/${RUNTIME_SDK_INSTALLER}.sh ${VERSION} ${OS} ${BUILD} && \
	rm -rf /tmp/${RUNTIME_SDK_INSTALLER}.sh

# Set the environment variable to Production
ENV Agent__Transport__Kind=Http
ENV Agent__Transport__Port=8080

USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SemanticKernel.Agents.DatabaseAgent.MCPServer.dll"]