namespace Pedidos.Api.Domain;

public sealed class Pedido
{
    public Guid Id { get; set; }
    public Cliente Cliente { get; init; } = new();
    public List<Producto> Productos { get; init; } = [];
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal MontoConDescuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
}
