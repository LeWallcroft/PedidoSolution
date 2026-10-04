using Pedidos.Api.Domain;
using Pedidos.Api.Services;
using Pedidos.Api.Repositories;
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
    public void test_cantidad_cero_no_cambia_el_subtotal()
    {
        // Arrange
        var service = new PedidoService();
        var productos = new[]
        {
            new Producto { Precio = 10m, Cantidad = 2 },
            new Producto { Precio = 50m, Cantidad = 0 }
        };
        // Act
        var subtotal = service.CalcularSubtotal(productos);
        // Assert
        Assert.Equal(20m, subtotal);
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

    [Theory]
    [InlineData(200, 20)]
    [InlineData(0, 0)]
    public void test_cliente_vip_recibe_diez_por_ciento_del_subtotal(decimal subtotal, decimal esperado)
    {
        // Arrange
        var service = new PedidoService();
        var cliente = new Cliente { Tipo = TipoCliente.Vip };
        // Act
        var descuento = service.CalcularDescuento(cliente, subtotal);
        // Assert
        Assert.Equal(esperado, descuento);
    }

    [Theory]
    [InlineData(200, 0)]
    [InlineData(0, 0)]
    public void test_cliente_regular_no_recibe_descuento(decimal subtotal, decimal esperado)
    {
        // Arrange
        var service = new PedidoService();
        var cliente = new Cliente { Tipo = TipoCliente.Regular };
        // Act
        var descuento = service.CalcularDescuento(cliente, subtotal);
        // Assert
        Assert.Equal(esperado, descuento);
    }

    [Theory]
    [InlineData(600, 120)]
    [InlineData(500, 25)]
    [InlineData(400, 20)]
    public void test_mayorista_recibe_veinte_por_ciento_solo_si_supera_500(decimal subtotal, decimal esperado)
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
    public void test_impuesto_es_dieciocho_por_ciento_del_monto_descontado_y_se_suma_al_total()
    {
        // Arrange
        var service = new PedidoService();
        var cliente = new Cliente { Tipo = TipoCliente.Vip };
        var subtotal = 100m;
        // Act
        var descuento = service.CalcularDescuento(cliente, subtotal);
        var montoConDescuento = subtotal - descuento;
        var impuesto = service.CalcularImpuesto(montoConDescuento);
        var total = service.CalcularTotal(montoConDescuento, impuesto);
        // Assert
        Assert.Equal(10m, descuento);
        Assert.Equal(90m, montoConDescuento);
        Assert.Equal(16.20m, impuesto);
        Assert.Equal(106.20m, total);
    }

    [Fact]
    public void test_monto_descontado_cero_produce_impuesto_y_total_cero()
    {
        // Arrange
        var service = new PedidoService();
        var montoConDescuento = 0m;

        // Act
        var impuesto = service.CalcularImpuesto(montoConDescuento);
        var total = service.CalcularTotal(montoConDescuento, impuesto);

        // Assert
        Assert.Equal(0m, impuesto);
        Assert.Equal(0m, total);
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
        Assert.NotEqual(Guid.Empty, pedido.Cliente.Id);
        Assert.NotEqual(Guid.Empty, Assert.Single(pedido.Productos).Id);
        Assert.Equal(100m, pedido.Subtotal);
        repositorio.Verify(r => r.Guardar(It.Is<Pedido>(p => p.Id == pedido.Id)), Times.Once);
    }

    [Fact]
    public void test_recuperar_pedido_devuelve_resultado_del_repositorio_para_el_id()
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

