# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy lib folder with ECM SDK DLLs
COPY lib/ ./lib/

# Copy project file and restore
COPY *.csproj ./
RUN dotnet restore

# Copy source and publish
COPY . .
RUN dotnet publish ExamplePlugin.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Create non-root user
RUN useradd -m -u 1000 ecm && chown -R ecm:ecm /app
USER ecm

# Copy published app
COPY --from=build /app/publish .

# Copy appsettings.json
COPY appsettings.json .

ENTRYPOINT ["dotnet", "VaultECMPlugin.dll"]
