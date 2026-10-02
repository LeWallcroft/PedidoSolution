using Pedidos.Api.Domain;
using Pedidos.Api.Services;
using Moq;

namespace Pedidos.UnitTests;

public class PedidoServiceTests
{
    [Fact]
    public void test_calcular_subtotal_pedido_vacio()
    {
        // Arrange
        var service = new PedidoService();
        var productos = Array.Empty<Producto>();
        // Act
        var subtotal = service.CalcularSubtotal(productos);
        // Assert
        Assert.Equal(0m, subtotal);
    }

    [Fact]
    public void test_calcular_subtotal_con_productos()
    {
        // Arrange
        var service = new PedidoService();
        var productos = new[] { new Producto { Precio = 12.50m, Cantidad = 2 }, new Producto { Precio = 4m, Cantidad = 3 } };
        // Act
        var subtotal = service.CalcularSubtotal(productos);
        // Assert
        Assert.Equal(37m, subtotal);
    }

    [Fact]
    public void test_cantidad_negativa_debe_ser_rechazada()
    {
        // Arrange
        var service = new PedidoService();
        var productos = new[] { new Producto { Precio = 10m, Cantidad = -1 } };
        // Act
        Action action = () => { service.CalcularSubtotal(productos); };
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void test_descuento_cliente_vip()
    {
        // Arrange
        var service = new PedidoService();
        var cliente = new Cliente { Tipo = TipoCliente.Vip };
        // Act
        var descuento = service.CalcularDescuento(cliente, 200m);
        // Assert
        Assert.Equal(20m, descuento);
    }

    [Fact]
    public void test_descuento_cliente_regular()
    {
        // Arrange
        var service = new PedidoService();
        var cliente = new Cliente { Tipo = TipoCliente.Regular };
        // Act
        var descuento = service.CalcularDescuento(cliente, 200m);
        // Assert
        Assert.Equal(0m, descuento);
    }

    [Theory]
    [InlineData(600, 120)]
    [InlineData(500, 25)]
    [InlineData(400, 20)]
    public void test_descuento_mayorista_segun_monto(decimal subtotal, decimal esperado)
    {
        // Arrange
        var service = new PedidoService();
        var cliente = new Cliente { Tipo = TipoCliente.Mayorista };
        // Act
        var descuento = service.CalcularDescuento(cliente, subtotal);
        // Assert
        Assert.Equal(esperado, descuento);
    }

    [Fact]
    public void test_calcular_impuesto_y_total()
    {
        // Arrange
        var service = new PedidoService();
        var montoConDescuento = 90m;
        // Act
        var impuesto = service.CalcularImpuesto(montoConDescuento);
        var total = service.CalcularTotal(montoConDescuento, impuesto);
        // Assert
        Assert.Equal(16.20m, impuesto);
        Assert.Equal(106.20m, total);
    }

    [Fact]
    public void test_crear_pedido_persiste_el_pedido_con_identificador()
    {
        // Arrange
        var repositorio = new Mock<IPedidoRepository>();
        var service = new PedidoService(repositorio.Object);
        var cliente = new Cliente { Tipo = TipoCliente.Regular };
        var productos = new[] { new Producto { Precio = 50m, Cantidad = 2 } };
        // Act
        var pedido = service.CrearPedido(cliente, productos);
        // Assert
        Assert.NotEqual(Guid.Empty, pedido.Id);
        Assert.Equal(100m, pedido.Subtotal);
        repositorio.Verify(r => r.Guardar(It.Is<Pedido>(p => p.Id == pedido.Id)), Times.Once);
    }

    [Fact]
    public void test_recuperar_pedido_por_identificador()
    {
        // Arrange
        var id = Guid.NewGuid();
        var pedidoEsperado = new Pedido { Id = id };
        var repositorio = new Mock<IPedidoRepository>();
        repositorio.Setup(r => r.ObtenerPorId(id)).Returns(pedidoEsperado);
        var service = new PedidoService(repositorio.Object);
        // Act
        var pedido = service.ObtenerPedido(id);
        // Assert
        Assert.Same(pedidoEsperado, pedido);
    }
}
