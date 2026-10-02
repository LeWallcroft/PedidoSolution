using Pedidos.Api.Domain;

namespace Pedidos.Api.Services;

public sealed class PedidoService
{
    public decimal CalcularSubtotal(IEnumerable<Producto> productos)
    {
        ArgumentNullException.ThrowIfNull(productos);
        foreach (var producto in productos)
        {
            if (producto.Cantidad < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(productos), "La cantidad no puede ser negativa.");
            }
        }

        return productos.Sum(producto => producto.Precio * producto.Cantidad);
    }
}
