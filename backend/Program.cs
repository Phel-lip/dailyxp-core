
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactLocal", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "https://phel-lip.github.io"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// Conexões configuradas fora do código
string? conexaoNeon = builder.Configuration.GetConnectionString("Neon");
string? senhaLocal = builder.Configuration["PostgresPassword"];

string conexaoString;

if (!string.IsNullOrWhiteSpace(conexaoNeon))
{
    conexaoString = conexaoNeon;
}
else if (!string.IsNullOrWhiteSpace(senhaLocal))
{
    conexaoString = new NpgsqlConnectionStringBuilder
    {
        Host = "localhost",
        Port = 5432,
        Database = "dailyxp_core",
        Username = "postgres",
        Password = senhaLocal
    }.ConnectionString;
}
else
{
    throw new Exception("Nenhuma conexão PostgreSQL configurada.");
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }
        )
    );
});

var app = builder.Build();

app.UseCors("ReactLocal");
app.UseRateLimiter();

// GET - Listar somente os hábitos do visitante
app.MapGet("/api/habitos", async (HttpRequest request) =>
{
    Guid? visitanteId = LerVisitante(request);

    if (visitanteId == null)
        return Results.BadRequest("Visitante inválido.");

    List<Habito> habitos = new List<Habito>();

    await using var conexao = new NpgsqlConnection(conexaoString);
    await conexao.OpenAsync();

    string sql = """
        SELECT id, nome, concluido
        FROM habitos
        WHERE visitante_id = $1
        ORDER BY id
        """;

    await using var comando = new NpgsqlCommand(sql, conexao);
    comando.Parameters.AddWithValue(visitanteId.Value);

    await using var leitor = await comando.ExecuteReaderAsync();

    while (await leitor.ReadAsync())
    {
        habitos.Add(LerHabito(leitor));
    }

    return Results.Ok(habitos);
});

// POST - Cadastrar hábito para o visitante
app.MapPost("/api/habitos", async (HttpRequest request, NovoHabito dados) =>
{
    Guid? visitanteId = LerVisitante(request);

    if (visitanteId == null)
        return Results.BadRequest("Visitante inválido.");

    if (string.IsNullOrWhiteSpace(dados.Nome) ||
        dados.Nome.Trim().Length > 100)
    {
        return Results.BadRequest("Nome inválido.");
    }

    await using var conexao = new NpgsqlConnection(conexaoString);
    await conexao.OpenAsync();

    string sql = """
        INSERT INTO habitos (nome, visitante_id)
        VALUES ($1, $2)
        RETURNING id, nome, concluido
        """;

    await using var comando = new NpgsqlCommand(sql, conexao);
    comando.Parameters.AddWithValue(dados.Nome.Trim());
    comando.Parameters.AddWithValue(visitanteId.Value);

    await using var leitor = await comando.ExecuteReaderAsync();
    await leitor.ReadAsync();

    return Results.Created(
        "/api/habitos",
        LerHabito(leitor)
    );
});

// PATCH - Concluir ou reabrir somente o próprio hábito
app.MapPatch("/api/habitos/{id}", async (
    int id,
    HttpRequest request,
    AtualizarHabito dados) =>
{
    Guid? visitanteId = LerVisitante(request);

    if (visitanteId == null)
        return Results.BadRequest("Visitante inválido.");

    await using var conexao = new NpgsqlConnection(conexaoString);
    await conexao.OpenAsync();

    string sql = """
        UPDATE habitos
        SET concluido = $1
        WHERE id = $2 AND visitante_id = $3
        RETURNING id, nome, concluido
        """;

    await using var comando = new NpgsqlCommand(sql, conexao);
    comando.Parameters.AddWithValue(dados.Concluido);
    comando.Parameters.AddWithValue(id);
    comando.Parameters.AddWithValue(visitanteId.Value);

    await using var leitor = await comando.ExecuteReaderAsync();

    if (await leitor.ReadAsync())
        return Results.Ok(LerHabito(leitor));

    return Results.NotFound();
});

// DELETE - Excluir somente o próprio hábito
app.MapDelete("/api/habitos/{id}", async (int id, HttpRequest request) =>
{
    Guid? visitanteId = LerVisitante(request);

    if (visitanteId == null)
        return Results.BadRequest("Visitante inválido.");

    await using var conexao = new NpgsqlConnection(conexaoString);
    await conexao.OpenAsync();

    string sql = """
        DELETE FROM habitos
        WHERE id = $1 AND visitante_id = $2
        """;

    await using var comando = new NpgsqlCommand(sql, conexao);
    comando.Parameters.AddWithValue(id);
    comando.Parameters.AddWithValue(visitanteId.Value);

    int linhasAfetadas = await comando.ExecuteNonQueryAsync();

    if (linhasAfetadas > 0)
        return Results.NoContent();

    return Results.NotFound();
});

app.Run();

// Lê o identificador enviado pelo React
static Guid? LerVisitante(HttpRequest request)
{
    string valor = request.Headers["X-Visitante-Id"].ToString();

    if (Guid.TryParse(valor, out Guid id) && id != Guid.Empty)
        return id;

    return null;
}

// Converte o resultado SQL em objeto C#
static Habito LerHabito(NpgsqlDataReader leitor)
{
    return new Habito
    {
        Id = leitor.GetInt32(0),
        Nome = leitor.GetString(1),
        Concluido = leitor.GetBoolean(2)
    };
}

public class Habito
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public bool Concluido { get; set; }
}

public class NovoHabito
{
    public string Nome { get; set; } = "";
}

public class AtualizarHabito
{
    public bool Concluido { get; set; }
}
