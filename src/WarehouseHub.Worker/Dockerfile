FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/WarehouseHub.Contracts/WarehouseHub.Contracts.csproj src/WarehouseHub.Contracts/
COPY src/WarehouseHub.Messaging/WarehouseHub.Messaging.csproj src/WarehouseHub.Messaging/
COPY src/WarehouseHub.Worker/WarehouseHub.Worker.csproj src/WarehouseHub.Worker/
RUN dotnet restore src/WarehouseHub.Worker/WarehouseHub.Worker.csproj

COPY src/ src/
RUN dotnet publish src/WarehouseHub.Worker/WarehouseHub.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "WarehouseHub.Worker.dll"]
