namespace Pedidos.Api.Domain;

public sealed class Producto
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = "Producto";
    public decimal Precio { get; init; }
    public int Cantidad { get; init; }
}
