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

# Copy project file and restore dependencies
COPY src/Services/GitHub/CodingAgent.Services.GitHub/CodingAgent.Services.GitHub.csproj src/Services/GitHub/CodingAgent.Services.GitHub/
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
	dotnet restore src/Services/GitHub/CodingAgent.Services.GitHub/CodingAgent.Services.GitHub.csproj --force --no-cache

# Copy everything and build
COPY src/SharedKernel/CodingAgent.SharedKernel/ src/SharedKernel/CodingAgent.SharedKernel/
COPY src/Services/GitHub/CodingAgent.Services.GitHub/ src/Services/GitHub/CodingAgent.Services.GitHub/
WORKDIR /src/src/Services/GitHub/CodingAgent.Services.GitHub
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
	dotnet build CodingAgent.Services.GitHub.csproj -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
	dotnet publish CodingAgent.Services.GitHub.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5003
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingAgent.Services.GitHub.dll"]
