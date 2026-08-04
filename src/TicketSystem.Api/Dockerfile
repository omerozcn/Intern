# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so dependency layers cache independently of source edits.
COPY TicketSystem.Api.csproj ./
RUN dotnet restore TicketSystem.Api.csproj

COPY . ./
RUN dotnet publish TicketSystem.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# The aspnet image ships neither curl nor wget, so the compose healthcheck has
# nothing to call /health with. Installed before dropping privileges.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Run as the non-root user the base image already provides.
USER $APP_UID

COPY --from=build /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "TicketSystem.Api.dll"]
