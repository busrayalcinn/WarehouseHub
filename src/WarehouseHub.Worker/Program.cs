using WarehouseHub.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<OrderConfirmedConsumer>();

var host = builder.Build();
host.Run();
