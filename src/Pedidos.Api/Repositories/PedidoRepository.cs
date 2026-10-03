using Microsoft.Data.SqlClient;
using Pedidos.Api.Domain;

namespace Pedidos.Api.Repositories;

public sealed class PedidoRepository : IPedidoRepository
{
    private readonly string _connectionString;

    public PedidoRepository(string connectionString) => _connectionString = connectionString;

    public void Guardar(Pedido pedido)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        Execute(connection, transaction,
            "INSERT INTO dbo.Clientes (Id, Tipo) VALUES (@id, @tipo)",
            ("@id", pedido.Cliente.Id), ("@tipo", (byte)pedido.Cliente.Tipo));

        Execute(connection, transaction,
            """
            INSERT INTO dbo.Pedidos
                (Id, ClienteId, Subtotal, Descuento, MontoConDescuento, Impuesto, Total)
            VALUES
                (@id, @clienteId, @subtotal, @descuento, @monto, @impuesto, @total)
            """,
            ("@id", pedido.Id), ("@clienteId", pedido.Cliente.Id),
            ("@subtotal", pedido.Subtotal), ("@descuento", pedido.Descuento),
            ("@monto", pedido.MontoConDescuento), ("@impuesto", pedido.Impuesto),
            ("@total", pedido.Total));

        foreach (var producto in pedido.Productos)
        {
            Execute(connection, transaction,
                "INSERT INTO dbo.Productos (Id, Nombre) VALUES (@id, @nombre)",
                ("@id", producto.Id), ("@nombre", producto.Nombre));
            Execute(connection, transaction,
                "INSERT INTO dbo.Precios (ProductoId, Valor) VALUES (@id, @precio)",
                ("@id", producto.Id), ("@precio", producto.Precio));
            Execute(connection, transaction,
                "INSERT INTO dbo.DetallesPedido (PedidoId, ProductoId, Cantidad) VALUES (@pedidoId, @productoId, @cantidad)",
                ("@pedidoId", pedido.Id), ("@productoId", producto.Id), ("@cantidad", producto.Cantidad));
        }

        transaction.Commit();
    }

    public Pedido? ObtenerPorId(Guid id)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        Pedido? pedido;
        using (var command = new SqlCommand(
            """
            SELECT p.Id, c.Id, c.Tipo, p.Subtotal, p.Descuento,
                   p.MontoConDescuento, p.Impuesto, p.Total
            FROM dbo.Pedidos AS p
            INNER JOIN dbo.Clientes AS c ON c.Id = p.ClienteId
            WHERE p.Id = @id
            """, connection))
        {
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            pedido = new Pedido
            {
                Id = reader.GetGuid(0),
                Cliente = new Cliente { Id = reader.GetGuid(1), Tipo = (TipoCliente)reader.GetByte(2) },
                Subtotal = reader.GetDecimal(3),
                Descuento = reader.GetDecimal(4),
                MontoConDescuento = reader.GetDecimal(5),
                Impuesto = reader.GetDecimal(6),
                Total = reader.GetDecimal(7)
            };
        }

        using (var command = new SqlCommand(
            """
            SELECT pr.Id, pr.Nombre, pe.Valor, d.Cantidad
            FROM dbo.DetallesPedido AS d
            INNER JOIN dbo.Productos AS pr ON pr.Id = d.ProductoId
            INNER JOIN dbo.Precios AS pe ON pe.ProductoId = pr.Id
            WHERE d.PedidoId = @id
            ORDER BY pr.Id
            """, connection))
        {
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                pedido.Productos.Add(new Producto
                {
                    Id = reader.GetGuid(0),
                    Nombre = reader.GetString(1),
                    Precio = reader.GetDecimal(2),
                    Cantidad = reader.GetInt32(3)
                });
            }
        }

        return pedido;
    }

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql,
        params (string Name, object Value)[] values)
    {
        using var command = new SqlCommand(sql, connection, transaction);
        foreach (var (name, value) in values)
        {
            command.Parameters.AddWithValue(name, value);
        }
        command.ExecuteNonQuery();
    }
}
