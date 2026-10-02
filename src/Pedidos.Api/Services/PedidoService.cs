using Pedidos.Api.Domain;

namespace Pedidos.Api.Services;

public sealed class PedidoService
{
    private const decimal DescuentoVip = 0.10m;

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
            TipoCliente.Mayorista => subtotal > 500m ? subtotal * 0.20m : subtotal * 0.05m,
            _ => 0m
        };
    }

    private static void ValidarCantidades(IEnumerable<Producto> productos)
    {
        if (productos.Any(producto => producto.Cantidad < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(productos), "La cantidad no puede ser negativa.");
        }
    }
}
