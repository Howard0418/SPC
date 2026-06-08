using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: SqlRunner <sql-file>");
    Environment.Exit(2);
}

var sqlFile = args[0];
var sql = await File.ReadAllTextAsync(sqlFile);
var connectionString = "Server=172.16.110.16;Database=PMR_SPC_2026;User Id=sa;Password=a@t123;Encrypt=True;TrustServerCertificate=True;";

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

await using var command = connection.CreateCommand();
command.CommandText = sql;
command.CommandTimeout = 300;

await using var reader = await command.ExecuteReaderAsync();
var resultSets = new List<List<Dictionary<string, object?>>>();

do
{
    var rows = new List<Dictionary<string, object?>>();
    while (await reader.ReadAsync())
    {
        var row = new Dictionary<string, object?>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            row[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
        }
        rows.Add(row);
    }

    if (rows.Count > 0)
    {
        resultSets.Add(rows);
    }
} while (await reader.NextResultAsync());

var output = resultSets.Count == 1 ? (object)resultSets[0] : resultSets;
Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
