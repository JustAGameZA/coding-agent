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
COPY src/Services/Browser/CodingAgent.Services.Browser/CodingAgent.Services.Browser.csproj src/Services/Browser/CodingAgent.Services.Browser/
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
    dotnet restore src/Services/Browser/CodingAgent.Services.Browser/CodingAgent.Services.Browser.csproj --force --no-cache

# Copy everything and build
COPY src/SharedKernel/CodingAgent.SharedKernel/ src/SharedKernel/CodingAgent.SharedKernel/
COPY src/Services/Browser/CodingAgent.Services.Browser/ src/Services/Browser/CodingAgent.Services.Browser/
WORKDIR /src/src/Services/Browser/CodingAgent.Services.Browser
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
    dotnet build CodingAgent.Services.Browser.csproj -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
    dotnet publish CodingAgent.Services.Browser.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install Playwright dependencies for Chromium and Firefox
RUN apt-get update && apt-get install -y \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libcairo2 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgbm1 \
    libnspr4 \
    libnss3 \
    libpango-1.0-0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxkbcommon0 \
    libxrandr2 \
    libxss1 \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Install Playwright browsers (will be done at runtime)
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

EXPOSE 5004

ENTRYPOINT ["dotnet", "CodingAgent.Services.Browser.dll"]
