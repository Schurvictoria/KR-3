using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrdersService.Data;
using OrdersService.Services;
using Shared.Models;
using RabbitMQ.Client;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext и сервисы
builder.Services.AddDbContext<OrdersDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IOrderService, OrderService>();

// Настройка RabbitMQ из env-переменных
builder.Services.AddSingleton<IConnection>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var factory = new ConnectionFactory
    {
        HostName = cfg["RabbitMQ:Host"]     ?? "rabbitmq",
        Port     = int.Parse(cfg["RabbitMQ:Port"] ?? "5672"),
        UserName = cfg["RabbitMQ:User"]     ?? "guest",
        Password = cfg["RabbitMQ:Password"] ?? "guest",
        DispatchConsumersAsync = true
    };
    return factory.CreateConnection();
});
// Сервис, который пишет в outbox
builder.Services.AddScoped<OrderOutboxService>();

// Web API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.Run(); 