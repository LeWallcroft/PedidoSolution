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

    private static void ValidarCantidades(IEnumerable<Producto> productos)
    {
        if (productos.Any(producto => producto.Cantidad < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(productos), "La cantidad no puede ser negativa.");
        }
    }
}
