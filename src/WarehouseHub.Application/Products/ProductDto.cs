namespace WarehouseHub.Application.Products;

public record ProductDto(Guid Id, string Sku, string Name, decimal UnitPrice, int StockQuantity);
