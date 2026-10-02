using Pedidos.Api.Domain;

namespace Pedidos.Api.Repositories;

public interface IPedidoRepository
{
    void Guardar(Pedido pedido);
    Pedido? ObtenerPorId(Guid id);
}
