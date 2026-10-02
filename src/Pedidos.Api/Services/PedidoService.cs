using Pedidos.Api.Domain;

namespace Pedidos.Api.Services;

public sealed class PedidoService
{
    public decimal CalcularSubtotal(IEnumerable<Producto> productos)
    {
        return productos.Sum(producto => producto.Precio * producto.Cantidad);
    }
}
