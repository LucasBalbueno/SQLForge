FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY server/src/SQLForge.Api/SQLForge.Api.csproj server/src/SQLForge.Api/
RUN dotnet restore server/src/SQLForge.Api/SQLForge.Api.csproj
COPY server/src/SQLForge.Api/ server/src/SQLForge.Api/
COPY client/ client/
RUN dotnet publish server/src/SQLForge.Api/SQLForge.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=10000 \
    PORT=10000
EXPOSE 10000
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SQLForge.Api.dll"]
