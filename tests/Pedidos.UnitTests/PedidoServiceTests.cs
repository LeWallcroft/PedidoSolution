using Pedidos.Api.Services;

namespace Pedidos.UnitTests;

public class PedidoServiceTests
{
    [Fact]
    public void test_calcular_subtotal_pedido_vacio()
    {
        // Arrange
        var service = new PedidoService();
        var productos = Array.Empty<Pedidos.Api.Domain.Producto>();

        // Act
        var subtotal = service.CalcularSubtotal(productos);

        // Assert
        Assert.Equal(0m, subtotal);
    }
}
