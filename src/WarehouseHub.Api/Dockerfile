FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so the restore layer is cached until dependencies change
COPY src/WarehouseHub.Domain/WarehouseHub.Domain.csproj src/WarehouseHub.Domain/
COPY src/WarehouseHub.Contracts/WarehouseHub.Contracts.csproj src/WarehouseHub.Contracts/
COPY src/WarehouseHub.Messaging/WarehouseHub.Messaging.csproj src/WarehouseHub.Messaging/
COPY src/WarehouseHub.Application/WarehouseHub.Application.csproj src/WarehouseHub.Application/
COPY src/WarehouseHub.Infrastructure/WarehouseHub.Infrastructure.csproj src/WarehouseHub.Infrastructure/
COPY src/WarehouseHub.Api/WarehouseHub.Api.csproj src/WarehouseHub.Api/
RUN dotnet restore src/WarehouseHub.Api/WarehouseHub.Api.csproj

COPY src/ src/
RUN dotnet publish src/WarehouseHub.Api/WarehouseHub.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "WarehouseHub.Api.dll"]
