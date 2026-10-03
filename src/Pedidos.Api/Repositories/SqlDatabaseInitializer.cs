using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Pedidos.Api.Repositories;

public static class SqlDatabaseInitializer
{
    public static void Initialize(string connectionString)
    {
        var settings = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(settings.InitialCatalog))
        {
            throw new InvalidOperationException("La cadena ConnectionStrings:Pedidos debe incluir Database.");
        }

        var databaseName = settings.InitialCatalog;
        var masterSettings = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        using (var master = new SqlConnection(masterSettings.ConnectionString))
        {
            master.Open();
            using var exists = new SqlCommand("SELECT DB_ID(@name)", master);
            exists.Parameters.AddWithValue("@name", databaseName);
            if (exists.ExecuteScalar() is DBNull)
            {
                var safeName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
                using var create = new SqlCommand($"CREATE DATABASE [{safeName}]", master)
                {
                    CommandTimeout = 60
                };
                create.ExecuteNonQuery();
            }
        }

        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Pedidos.Api.Database.Schema.sql")
            ?? throw new InvalidOperationException("No se encontró el script SQL integrado.");
        using var reader = new StreamReader(stream);
        using var schema = new SqlCommand(reader.ReadToEnd(), connection) { CommandTimeout = 60 };
        schema.ExecuteNonQuery();
    }
}
