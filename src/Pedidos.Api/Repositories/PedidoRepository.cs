using System.Collections.Concurrent;
using Pedidos.Api.Domain;

namespace Pedidos.Api.Repositories;

public sealed class PedidoRepository : IPedidoRepository
{
    private readonly ConcurrentDictionary<Guid, Pedido> _pedidos = new();

    public void Guardar(Pedido pedido) => _pedidos[pedido.Id] = pedido;

    public Pedido? ObtenerPorId(Guid id) => _pedidos.GetValueOrDefault(id);
}
