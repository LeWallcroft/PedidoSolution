using Pedidos.Api.Domain;

namespace Pedidos.Api.Services;

public sealed class PedidoService
{
    public decimal CalcularSubtotal(IEnumerable<Producto> productos)
    {
        ArgumentNullException.ThrowIfNull(productos);
        ValidarCantidades(productos);
        return productos.Sum(producto => producto.Precio * producto.Cantidad);
    }

    public decimal CalcularDescuento(Cliente cliente, decimal subtotal)
    {
        return cliente.Tipo == TipoCliente.Vip ? subtotal * 0.10m : 0m;
    }

    private static void ValidarCantidades(IEnumerable<Producto> productos)
    {
        if (productos.Any(producto => producto.Cantidad < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(productos), "La cantidad no puede ser negativa.");
        }
    }
}
