namespace Pedidos.Api.Domain;

public enum TipoCliente
{
    Regular,
    Vip,
    Mayorista
}

public sealed class Cliente
{
    public TipoCliente Tipo { get; init; }
}
