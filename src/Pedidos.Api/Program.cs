using Pedidos.Api.Domain;
using Pedidos.Api.Repositories;
using Pedidos.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<PedidoService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/pedidos", (Pedido entrada, PedidoService servicio) =>
{
    try
    {
        var pedido = servicio.CrearPedido(entrada.Cliente, entrada.Productos);
        return Results.Created($"/pedidos/{pedido.Id}", pedido);
    }
    catch (ArgumentOutOfRangeException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.MapGet("/pedidos/{id:guid}", (Guid id, PedidoService servicio) =>
{
    var pedido = servicio.ObtenerPedido(id);
    return pedido is null ? Results.NotFound() : Results.Ok(pedido);
});

app.Run();

public partial class Program { }
