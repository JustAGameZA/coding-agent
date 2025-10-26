# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Services/Ollama/CodingAgent.Services.Ollama/CodingAgent.Services.Ollama.csproj", "Services/Ollama/CodingAgent.Services.Ollama/"]
COPY ["SharedKernel/CodingAgent.SharedKernel/CodingAgent.SharedKernel.csproj", "SharedKernel/CodingAgent.SharedKernel/"]
RUN dotnet restore "Services/Ollama/CodingAgent.Services.Ollama/CodingAgent.Services.Ollama.csproj"

# Copy all files and build
COPY . .
WORKDIR "/src/Services/Ollama/CodingAgent.Services.Ollama"
RUN dotnet build "CodingAgent.Services.Ollama.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CodingAgent.Services.Ollama.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingAgent.Services.Ollama.dll"]
