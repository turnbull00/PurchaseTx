# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Restore first, in its own layer, so `dotnet restore` is cached across builds
# unless api.csproj itself changes.
COPY api/api.csproj api/
RUN dotnet restore api/api.csproj

COPY api/ api/
RUN dotnet publish api/api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .

# Official .NET images define a non-root "app" user (uid via $APP_UID); run as
# that instead of root.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=3000
EXPOSE 3000
ENTRYPOINT ["dotnet", "api.dll"]
