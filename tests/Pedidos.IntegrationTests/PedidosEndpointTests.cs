using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pedidos.Api.Domain;

namespace Pedidos.IntegrationTests;

public class PedidosEndpointTests
{
    [Fact]
    public async Task Post_pedidos_devuelve_calculos_y_persiste_en_sql()
    {
        // Arrange: dos productos suman 100 × 1 + 50 × 2 = 200; VIP descuenta 10 %.
        using var database = new TemporarySqlDatabase();
        using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var request = new Pedido
        {
            Cliente = new Cliente { Tipo = TipoCliente.Vip },
            Productos =
            [
                new Producto { Nombre = "Cuaderno", Precio = 100m, Cantidad = 1 },
                new Producto { Nombre = "Lápiz", Precio = 50m, Cantidad = 2 }
            ]
        };

        // Act
        var response = await client.PostAsJsonAsync("/pedidos", request);
        var pedido = await response.Content.ReadFromJsonAsync<Pedido>();

        // Assert: 200 - 20 = 180; impuesto 32,40; total 212,40.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(pedido);
        Assert.NotEqual(Guid.Empty, pedido.Id);
        Assert.Equal(200m, pedido.Subtotal);
        Assert.Equal(20m, pedido.Descuento);
        Assert.Equal(180m, pedido.MontoConDescuento);
        Assert.Equal(32.40m, pedido.Impuesto);
        Assert.Equal(212.40m, pedido.Total);
        Assert.Equal(new[] { 1, 1, 2, 2, 2 }, CountDatabaseRows(database.ConnectionString));
    }

    [Fact]
    public async Task Get_pedidos_devuelve_pedido_previamente_creado()
    {
        // Arrange: un pedido Regular de 25 × 4 vale 100 sin descuento.
        using var database = new TemporarySqlDatabase();
        using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var request = new Pedido
        {
            Cliente = new Cliente { Tipo = TipoCliente.Regular },
            Productos = [new Producto { Nombre = "Bolígrafo", Precio = 25m, Cantidad = 4 }]
        };
        var creado = await client.PostAsJsonAsync("/pedidos", request);
        var guardado = await creado.Content.ReadFromJsonAsync<Pedido>();
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        Assert.NotNull(guardado);

        // Act
        var response = await client.GetAsync($"/pedidos/{guardado.Id}");
        var recuperado = await response.Content.ReadFromJsonAsync<Pedido>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(recuperado);
        Assert.Equal(guardado.Id, recuperado.Id);
        Assert.Equal(TipoCliente.Regular, recuperado.Cliente.Tipo);
        Assert.Equal("Bolígrafo", Assert.Single(recuperado.Productos).Nombre);
        Assert.Equal(25m, recuperado.Productos[0].Precio);
        Assert.Equal(4, recuperado.Productos[0].Cantidad);
        Assert.Equal(100m, recuperado.Subtotal);
        Assert.Equal(118m, recuperado.Total);
    }

    [Fact]
    public async Task Get_pedidos_inexistente_devuelve_404()
    {
        // Arrange: la base temporal está vacía.
        using var database = new TemporarySqlDatabase();
        using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/pedidos/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_pedidos_con_cantidad_negativa_devuelve_400_sin_guardar()
    {
        // Arrange: una cantidad -1 se rechaza antes de persistir.
        using var database = new TemporarySqlDatabase();
        using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var request = new Pedido
        {
            Cliente = new Cliente { Tipo = TipoCliente.Regular },
            Productos = [new Producto { Precio = 10m, Cantidad = -1 }]
        };

        // Act
        var response = await client.PostAsJsonAsync("/pedidos", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, CountDatabaseRows(database.ConnectionString)[1]);
    }

    [Fact]
    public async Task Pedido_permanece_en_sql_entre_instancias_de_la_api()
    {
        // Arrange: ambas aplicaciones usan la misma base temporal.
        using var database = new TemporarySqlDatabase();
        Guid id;
        using (var firstFactory = CreateFactory(database.ConnectionString))
        using (var firstClient = firstFactory.CreateClient())
        {
            var response = await firstClient.PostAsJsonAsync("/pedidos", new Pedido
            {
                Cliente = new Cliente { Tipo = TipoCliente.Mayorista },
                Productos = [new Producto { Precio = 500m, Cantidad = 1 }]
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            id = (await response.Content.ReadFromJsonAsync<Pedido>())!.Id;
        }

        // Act: se inicia una segunda aplicación después de cerrar la primera.
        using var secondFactory = CreateFactory(database.ConnectionString);
        using var secondClient = secondFactory.CreateClient();
        var recovered = await secondClient.GetAsync($"/pedidos/{id}");
        var pedido = await recovered.Content.ReadFromJsonAsync<Pedido>();

        // Assert: 500 exactos reciben 5 %, total (500 - 25) × 1,18 = 560,50.
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        Assert.NotNull(pedido);
        Assert.Equal(id, pedido.Id);
        Assert.Equal(25m, pedido.Descuento);
        Assert.Equal(560.50m, pedido.Total);
    }

    private static WebApplicationFactory<Program> CreateFactory(string connectionString) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Pedidos"] = connectionString
                })));

    private static int[] CountDatabaseRows(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = new SqlCommand(
            """
            SELECT (SELECT COUNT(*) FROM dbo.Clientes),
                   (SELECT COUNT(*) FROM dbo.Pedidos),
                   (SELECT COUNT(*) FROM dbo.Productos),
                   (SELECT COUNT(*) FROM dbo.Precios),
                   (SELECT COUNT(*) FROM dbo.DetallesPedido)
            """, connection);
        using var reader = command.ExecuteReader();
        reader.Read();
        return Enumerable.Range(0, 5).Select(reader.GetInt32).ToArray();
    }

    private sealed class TemporarySqlDatabase : IDisposable
    {
        private readonly string _databaseName = $"PedidosTest_{Guid.NewGuid():N}";
        private readonly string _masterConnectionString;
        public string ConnectionString { get; }

        public TemporarySqlDatabase()
        {
            var configured = Environment.GetEnvironmentVariable("ConnectionStrings__Pedidos")
                ?? throw new InvalidOperationException(
                    "Configure ConnectionStrings__Pedidos con el servidor SQL antes de ejecutar integración.");
            var settings = new SqlConnectionStringBuilder(configured);
            settings.InitialCatalog = _databaseName;
            ConnectionString = settings.ConnectionString;
            settings.InitialCatalog = "master";
            _masterConnectionString = settings.ConnectionString;
        }

        public void Dispose()
        {
            using var connection = new SqlConnection(_masterConnectionString);
            connection.Open();
            using var exists = new SqlCommand("SELECT DB_ID(@name)", connection);
            exists.Parameters.AddWithValue("@name", _databaseName);
            if (exists.ExecuteScalar() is DBNull) return;

            // El nombre siempre se genera internamente con el prefijo PedidosTest_.
            using var drop = new SqlCommand(
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]",
                connection)
            {
                CommandTimeout = 60
            };
            drop.ExecuteNonQuery();
        }
    }
}
