using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Pedidos.Api.Domain;

namespace Pedidos.IntegrationTests;

public class PedidosEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PedidosEndpointTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Post_pedidos_devuelve_pedido_calculado()
    {
        // Arrange
        var request = new Pedido
        {
            Cliente = new Cliente { Tipo = TipoCliente.Vip },
            Productos = [new Producto { Precio = 100m, Cantidad = 2 }]
        };
        // Act
        var response = await _client.PostAsJsonAsync("/pedidos", request);
        var pedido = await response.Content.ReadFromJsonAsync<Pedido>();
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(pedido);
        Assert.NotEqual(Guid.Empty, pedido.Id);
        Assert.Equal(200m, pedido.Subtotal);
        Assert.Equal(20m, pedido.Descuento);
        Assert.Equal(32.40m, pedido.Impuesto);
        Assert.Equal(212.40m, pedido.Total);
    }

    [Fact]
    public async Task Get_pedidos_devuelve_pedido_previamente_creado()
    {
        // Arrange
        var request = new Pedido
        {
            Cliente = new Cliente { Tipo = TipoCliente.Regular },
            Productos = [new Producto { Precio = 25m, Cantidad = 4 }]
        };
        var creado = await _client.PostAsJsonAsync("/pedidos", request);
        var guardado = await creado.Content.ReadFromJsonAsync<Pedido>();
        // Act
        var response = await _client.GetAsync($"/pedidos/{guardado!.Id}");
        var recuperado = await response.Content.ReadFromJsonAsync<Pedido>();
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(recuperado);
        Assert.Equal(guardado.Id, recuperado.Id);
        Assert.Equal(guardado.Total, recuperado.Total);
    }

    [Fact]
    public async Task Get_pedidos_inexistente_devuelve_404()
    {
        // Arrange
        var id = Guid.NewGuid();
        // Act
        var response = await _client.GetAsync($"/pedidos/{id}");
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_pedidos_con_cantidad_negativa_devuelve_400()
    {
        // Arrange
        var request = new Pedido
        {
            Cliente = new Cliente { Tipo = TipoCliente.Regular },
            Productos = [new Producto { Precio = 10m, Cantidad = -1 }]
        };
        // Act
        var response = await _client.PostAsJsonAsync("/pedidos", request);
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
