using MediatR;
using Microsoft.AspNetCore.Mvc;
using WarehouseHub.Application.Products;
using WarehouseHub.Application.Products.Commands;
using WarehouseHub.Application.Products.Queries;

namespace WarehouseHub.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetProductsQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetProductByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/stock")]
    public async Task<IActionResult> IncreaseStock(Guid id, IncreaseStockRequest request, CancellationToken ct)
    {
        await sender.Send(new IncreaseStockCommand(id, request.Quantity), ct);
        return NoContent();
    }
}

public record IncreaseStockRequest(int Quantity);
