using Npgsql;

string connStr = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=loja";

using var connection = new NpgsqlConnection(connStr);
connection.Open();

using var cmd = new NpgsqlCommand(
    "INSERT INTO cliente (nome, email) VALUES (@nome, @email) RETURNING id", connection);

// No livro o e-mail é fixo. Aqui ele é único a cada execução, para você poder rodar quantas vezes quiser.
cmd.Parameters.AddWithValue("nome", "Maria");
cmd.Parameters.AddWithValue("email", $"maria-{Guid.NewGuid():N}@email.com");

long id = Convert.ToInt64(cmd.ExecuteScalar());
Console.WriteLine($"Cliente criado com id {id}");

// Um SELECT na mão
using var cmd2 = new NpgsqlCommand("SELECT id, nome, email FROM cliente", connection);
using var reader = cmd2.ExecuteReader();

while (reader.Read())
{
    Console.WriteLine($"{reader["id"]} - {reader["nome"]} - {reader["email"]}");
}
