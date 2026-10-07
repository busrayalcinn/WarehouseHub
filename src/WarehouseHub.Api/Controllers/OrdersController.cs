using MediatR;
using Microsoft.AspNetCore.Mvc;
using WarehouseHub.Application.Orders;
using WarehouseHub.Application.Orders.Commands;
using WarehouseHub.Application.Orders.Queries;

namespace WarehouseHub.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryDto>>> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetOrdersQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetOrderByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        await sender.Send(new ConfirmOrderCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await sender.Send(new CancelOrderCommand(id), ct);
        return NoContent();
    }
}
