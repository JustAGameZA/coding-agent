# Build stage
# syntax=docker/dockerfile:1.6
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILDKIT_INLINE_CACHE=1
WORKDIR /src

# Copy solution-level files that may affect restore
COPY global.json ./
COPY Directory.Build.props ./

# Copy SharedKernel
COPY src/SharedKernel/CodingAgent.SharedKernel/CodingAgent.SharedKernel.csproj src/SharedKernel/CodingAgent.SharedKernel/

# Copy gateway csproj and restore dependencies
COPY src/Gateway/CodingAgent.Gateway/CodingAgent.Gateway.csproj src/Gateway/CodingAgent.Gateway/
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
	dotnet restore src/Gateway/CodingAgent.Gateway/CodingAgent.Gateway.csproj --force --no-cache

# Copy everything and build
COPY src/SharedKernel/CodingAgent.SharedKernel/ src/SharedKernel/CodingAgent.SharedKernel/
COPY src/Gateway/CodingAgent.Gateway/ src/Gateway/CodingAgent.Gateway/
WORKDIR /src/src/Gateway/CodingAgent.Gateway
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
	dotnet build CodingAgent.Gateway.csproj -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
	dotnet publish CodingAgent.Gateway.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5000
EXPOSE 5500
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingAgent.Gateway.dll"]
