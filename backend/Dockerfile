# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Backend.sln ./
COPY Backend.Api/Backend.Api.csproj Backend.Api/
COPY Backend.Tests/Backend.Tests.csproj Backend.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build
RUN dotnet build -c Release --no-restore

# Test stage
FROM build AS test
WORKDIR /src
ENTRYPOINT ["dotnet", "test", "-c", "Release", "--no-build", "--verbosity", "normal"]

# Publish stage
FROM build AS publish
WORKDIR /src/Backend.Api
RUN dotnet publish -c Release --no-build -o /app/publish

# Production stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS production
WORKDIR /app
EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Backend.Api.dll"]
