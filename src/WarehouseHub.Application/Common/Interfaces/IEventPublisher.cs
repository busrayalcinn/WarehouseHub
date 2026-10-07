using WarehouseHub.Contracts;

namespace WarehouseHub.Application.Common.Interfaces;

public interface IEventPublisher
{
    Task PublishOrderConfirmedAsync(OrderConfirmedEvent message, CancellationToken cancellationToken = default);
}
