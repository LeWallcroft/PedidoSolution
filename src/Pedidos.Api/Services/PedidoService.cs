using Pedidos.Api.Domain;

namespace Pedidos.Api.Services;

public sealed class PedidoService
{
    private const decimal DescuentoVip = 0.10m;
    private const decimal DescuentoMayoristaAlto = 0.20m;
    private const decimal DescuentoMayoristaBase = 0.05m;
    private const decimal UmbralMayorista = 500m;

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

    public decimal CalcularImpuesto(decimal montoConDescuento) => montoConDescuento * 0.18m;

    public decimal CalcularTotal(decimal montoConDescuento, decimal impuesto) => montoConDescuento + impuesto;

    private static void ValidarCantidades(IEnumerable<Producto> productos)
    {
        if (productos.Any(producto => producto.Cantidad < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(productos), "La cantidad no puede ser negativa.");
        }
    }
}
