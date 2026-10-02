using Pedidos.Api.Domain;
using Pedidos.Api.Repositories;

namespace Pedidos.Api.Services;

public sealed class PedidoService
{
    private const decimal TasaImpuesto = 0.18m;
    private const decimal DescuentoVip = 0.10m;
    private const decimal DescuentoMayoristaAlto = 0.20m;
    private const decimal DescuentoMayoristaBase = 0.05m;
    private const decimal UmbralMayorista = 500m;
    private readonly IPedidoRepository? _repositorio;

    public PedidoService() { }

    public PedidoService(IPedidoRepository repositorio) => _repositorio = repositorio;

    public decimal CalcularSubtotal(IEnumerable<Producto> productos)
    {
        ArgumentNullException.ThrowIfNull(productos);
        ValidarCantidades(productos);
        return productos.Sum(producto => producto.Precio * producto.Cantidad);
    }

    public decimal CalcularDescuento(Cliente cliente, decimal subtotal)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        return cliente.Tipo switch
        {
            TipoCliente.Vip => subtotal * DescuentoVip,
            TipoCliente.Mayorista => subtotal * (subtotal > UmbralMayorista ? DescuentoMayoristaAlto : DescuentoMayoristaBase),
            _ => 0m
        };
    }

    public decimal CalcularImpuesto(decimal montoConDescuento) => montoConDescuento * TasaImpuesto;

    public decimal CalcularTotal(decimal montoConDescuento, decimal impuesto) => montoConDescuento + impuesto;

    public Pedido CrearPedido(Cliente cliente, IEnumerable<Producto> productos)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        var listaProductos = productos.ToList();
        var subtotal = CalcularSubtotal(listaProductos);
        var descuento = CalcularDescuento(cliente, subtotal);
        var montoConDescuento = subtotal - descuento;
        var impuesto = CalcularImpuesto(montoConDescuento);
        var pedido = new Pedido
        {
            Id = Guid.NewGuid(),
            Cliente = cliente,
            Productos = listaProductos,
            Subtotal = subtotal,
            Descuento = descuento,
            MontoConDescuento = montoConDescuento,
            Impuesto = impuesto,
            Total = CalcularTotal(montoConDescuento, impuesto)
        };
        Repositorio.Guardar(pedido);
        return pedido;
    }

    public Pedido? ObtenerPedido(Guid id) => Repositorio.ObtenerPorId(id);

    private IPedidoRepository Repositorio => _repositorio ?? throw new InvalidOperationException("Se requiere un repositorio para persistir o consultar pedidos.");

    private static void ValidarCantidades(IEnumerable<Producto> productos)
    {
        if (productos.Any(producto => producto.Cantidad < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(productos), "La cantidad no puede ser negativa.");
        }
    }
}
