using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace MesaViva.Data;

internal static class ConexaoMySql
{
    public const string Variavel = "MESAVIVA_DB";
    public const string ServidorPadrao = "localhost";
    public const uint PortaPadrao = 3306;
    public const string BancoPadrao = "mesaviva";
    public const string UsuarioPadrao = "root";

    private const string PrepararBanco = "Para preparar o banco, defina MESAVIVA_DB no PowerShell com os dados do MySQL e execute "
        + "dotnet ef database update na pasta do projeto com uma conta autorizada. Depois, tente conectar novamente.";
    private const string EsquemaIncompleto = "As tabelas do MesaViva estão ausentes ou desatualizadas. " + PrepararBanco;
    private const string ServidorIndisponivel = "Não foi possível acessar o MySQL. Verifique se o serviço está em execução e confira o servidor e a porta.";

    public static string? LerConfiguracao()
    {
        string? conexao = Environment.GetEnvironmentVariable(Variavel);
        return string.IsNullOrWhiteSpace(conexao) ? null : conexao;
    }

    public static string Montar(string servidor, uint porta, string banco, string usuario, string senha)
    {
        return new MySqlConnectionStringBuilder
        {
            Server = servidor,
            Port = porta,
            Database = banco,
            UserID = usuario,
            Password = senha,
            ConnectionTimeout = 8
        }.ConnectionString;
    }

    public static async Task<string?> VerificarAsync(string conexao, CancellationToken cancelamento)
    {
        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(cancelamento);
        prazo.CancelAfter(TimeSpan.FromSeconds(15));
        CancellationToken token = prazo.Token;

        try
        {
            var configuracao = new MySqlConnectionStringBuilder(conexao)
            {
                Pooling = false,
                DefaultCommandTimeout = 5
            };
            if (string.IsNullOrWhiteSpace(configuracao.Database))
                return "Informe o nome do banco de dados do MesaViva.";

            var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(configuracao.ConnectionString).Options;
            await using var contexto = new AppDbContext(opcoes);
            await contexto.Database.OpenConnectionAsync(token);
            if ((await contexto.Database.GetPendingMigrationsAsync(token)).Any())
                return EsquemaIncompleto;

            // As leituras conferem as tabelas e colunas esperadas sem alterar dados.
            await contexto.Usuarios.AsNoTracking().Take(1).ToListAsync(token);
            await contexto.Ofertas.AsNoTracking().Take(1).ToListAsync(token);
            await contexto.Reservas.AsNoTracking().Take(1).ToListAsync(token);
            token.ThrowIfCancellationRequested();
            return null;
        }
        catch (Exception) when (cancelamento.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancelamento);
        }
        catch (Exception) when (prazo.IsCancellationRequested)
        {
            return ServidorIndisponivel;
        }
        catch (ArgumentException)
        {
            return "A configuração do MySQL é inválida. Confira os campos ou a variável MESAVIVA_DB.";
        }
        catch (Exception erro)
        {
            return MensagemDeErro(erro);
        }
    }

    private static string MensagemDeErro(Exception erro)
    {
        for (Exception? causa = erro; causa is not null; causa = causa.InnerException)
        {
            if (causa is MySqlException mysql)
            {
                return mysql.Number switch
                {
                    1045 => "O MySQL recusou a autenticação. Confira o usuário e a senha.",
                    1044 or 1130 or 1142 or 1143 or 1227 => "O usuário do MySQL não tem as permissões necessárias para acessar o banco do MesaViva.",
                    1049 => "O banco de dados informado não existe. " + PrepararBanco,
                    1054 or 1146 => EsquemaIncompleto,
                    _ => ServidorIndisponivel
                };
            }
        }

        return ServidorIndisponivel;
    }
}
