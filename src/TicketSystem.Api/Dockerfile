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

# Run as the non-root user the base image already provides.
USER $APP_UID

COPY --from=build /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "TicketSystem.Api.dll"]
