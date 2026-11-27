using MesaViva.Models;
using MesaViva.Services;
using Microsoft.EntityFrameworkCore;

namespace MesaViva.Data;

internal static class DadosDemonstracao
{
    private const string Senha = "Demo123!";

    public static async Task PrepararAsync()
    {
        await using var contexto = new AppDbContext();
        await contexto.Database.OpenConnectionAsync();

        // Serializa preparações simultâneas, inclusive os cadastros feitos por outro contexto.
        int? bloqueio = await contexto.Database.SqlQuery<int?>(
            $"SELECT GET_LOCK(SHA2(CONCAT(DATABASE(), ':mesaviva:demonstracao'), 256), 10) AS Value")
            .SingleAsync();
        if (bloqueio != 1)
            throw new InvalidOperationException("Não foi possível obter o bloqueio para preparar os dados de demonstração.");

        try
        {
            var auth = new AuthService(contexto);
            Usuario padaria = await ObterOuCadastrarUsuarioAsync(contexto, auth, "Padaria MesaViva",
                "padaria@mesaviva.test", "(47) 99000-1001", "Rua de Demonstração, 100", Usuario.TipoEstabelecimento);
            await ObterOuCadastrarUsuarioAsync(contexto, auth, "Instituto Esperança",
                "instituto@mesaviva.test", "(47) 99000-1002", "Rua de Demonstração, 200", Usuario.TipoInstituicao);

            var ofertas = new OfertaService();
            DateTime agora = DateTime.Now;
            await CadastrarOfertaSeAusenteAsync(contexto, ofertas, padaria, "Pães", 15m, agora, agora.AddHours(1));
            await CadastrarOfertaSeAusenteAsync(contexto, ofertas, padaria, "Maçãs", 20m, agora, agora.AddHours(24));
        }
        finally
        {
            await contexto.Database.SqlQuery<int?>(
                $"SELECT RELEASE_LOCK(SHA2(CONCAT(DATABASE(), ':mesaviva:demonstracao'), 256)) AS Value")
                .SingleAsync();
        }
    }

    private static async Task<Usuario> ObterOuCadastrarUsuarioAsync(
        AppDbContext contexto, AuthService auth, string nome, string email, string telefone, string endereco, string tipo)
    {
        if (!await contexto.Usuarios.AnyAsync(usuario => usuario.Email == email))
            return await auth.CadastrarUsuarioAsync(nome, email, Senha, telefone, endereco, tipo);

        Usuario? existente = await auth.FazerLoginAsync(email, Senha);
        if (existente is null || existente.Tipo != tipo)
            throw new InvalidOperationException($"O e-mail {email} está em uso por uma conta incompatível. Use um banco de demonstração separado.");

        return existente;
    }

    private static async Task CadastrarOfertaSeAusenteAsync(
        AppDbContext contexto, OfertaService ofertas, Usuario estabelecimento, string alimento, decimal quantidade,
        DateTime inicioRetirada, DateTime fimRetirada)
    {
        if (await contexto.Ofertas.AnyAsync(oferta =>
            oferta.EstabelecimentoId == estabelecimento.Id && oferta.Alimento == alimento))
        {
            return;
        }

        DateTime validade = fimRetirada.AddHours(1);
        await ofertas.CadastrarOfertaAsync(estabelecimento, alimento, quantidade, validade, inicioRetirada, fimRetirada);
    }
}
