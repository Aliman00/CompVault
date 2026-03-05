# Byggsteg
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopier kun prosjektfiler først for bedre lag-caching
# Dette gjør at restore-laget kun rebuildes når dependencies endres
COPY ["CompVault.Backend/CompVault.Backend.csproj", "CompVault.Backend/"]
COPY ["CompVault.Shared/CompVault.Shared.csproj", "CompVault.Shared/"]

# Restore dependencies (denne operasjonen caches hvis csproj ikke endres)
RUN dotnet restore "CompVault.Backend/CompVault.Backend.csproj"

# Kopier all kildekode
COPY CompVault.Backend/ CompVault.Backend/
COPY CompVault.Shared/ CompVault.Shared/

# Bygg
WORKDIR /src/CompVault.Backend
RUN dotnet build "CompVault.Backend.csproj" -c Release -o /app/build

# Publiseringssteg
FROM build AS publish
RUN dotnet publish "CompVault.Backend.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

# Kjøresteg - bruk minimal runtime-image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# OCI-metadata som kobler Docker-imaget til GitHub-repoet
LABEL org.opencontainers.image.source=https://github.com/aliman00/CompVault
LABEL org.opencontainers.image.description="CompVault Backend – ASP.NET Core Web API"
LABEL org.opencontainers.image.licenses=MIT

# Opprett non-root user for security
RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser && \
    chown -R appuser:appuser /app

# Kopier de publiserte filene
COPY --from=publish --chown=appuser:appuser /app/publish .

# Switch til non-root user
USER appuser

# Eksponér porter
EXPOSE 8080
EXPOSE 8081

# Helsesjekk
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CompVault.Backend.dll"]
