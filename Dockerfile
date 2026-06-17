# --- Build stage (SDK .NET 8) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj trước để tận dụng cache layer restore.
COPY src/ImageVault.Domain/ImageVault.Domain.csproj src/ImageVault.Domain/
COPY src/ImageVault.Application/ImageVault.Application.csproj src/ImageVault.Application/
COPY src/ImageVault.Infrastructure/ImageVault.Infrastructure.csproj src/ImageVault.Infrastructure/
COPY src/ImageVault.Api/ImageVault.Api.csproj src/ImageVault.Api/
RUN dotnet restore src/ImageVault.Api/ImageVault.Api.csproj

# Copy phần còn lại và publish.
COPY src/ src/
RUN dotnet publish src/ImageVault.Api/ImageVault.Api.csproj -c Release -o /app /p:UseAppHost=false

# --- Runtime stage (ASP.NET .NET 8) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl cho Docker healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ImageVault.Api.dll"]
