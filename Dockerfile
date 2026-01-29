# Build stage - Windows Server Core
FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY *.csproj ./
COPY lib/ ./lib/
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet publish ExamplePlugin.csproj -c Release -o /app/publish --no-restore

# Runtime stage - Windows Server Core
FROM mcr.microsoft.com/dotnet/aspnet:8.0-windowsservercore-ltsc2022 AS runtime
WORKDIR /app

# Copy published app (includes most runtime dependencies)
COPY --from=build /app/publish .

# Copy all required DLLs - these provide compatibility with BeyondTrust SDK
COPY lib/ .
COPY DLL/ .

# Disable console mode when running in container
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "ExamplePlugin.dll"]
