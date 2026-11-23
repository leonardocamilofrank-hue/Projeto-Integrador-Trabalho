using MesaViva.Models;
using MesaViva.Services;

namespace MesaViva.UI;

public static class MenuInicial
{
    // Valores normalizados e gravados pelo AuthService.
    private const string TipoEstabelecimento = "Estabelecimento";
    private const string TipoInstituicao = "Instituição";

    public static async Task ExibirAsync(AuthService authService)
    {
        bool continuar = true;

        while (continuar)
        {
            Console.Clear();
            Console.WriteLine("========== MESAVIVA ==========");
            Console.WriteLine("1 - Fazer login");
            Console.WriteLine("2 - Cadastrar conta");
            Console.WriteLine("0 - Sair");

            int opcao = Utils.LerInteiro("Escolha uma opção: ");

            switch (opcao)
            {
                case 1:
                    await FazerLoginAsync(authService);
                    break;
                case 2:
                    await CadastrarContaAsync(authService);
                    break;
                case 0:
                    continuar = false;
                    break;
                default:
                    Console.WriteLine("Opção inválida.");
                    AguardarContinuacao();
                    break;
            }
        }
    }

    private static async Task FazerLoginAsync(AuthService authService)
    {
        Console.Clear();
        Console.WriteLine("========== LOGIN ==========");

        string email = LerCampo("E-mail: ");
        string senha = LerCampo("Senha: ");

        Usuario? usuario = await authService.FazerLoginAsync(email, senha);

        if (usuario is null)
        {
            Console.WriteLine("E-mail ou senha inválidos.");
            AguardarContinuacao();
            return;
        }

        if (string.Equals(usuario.Tipo, TipoEstabelecimento, StringComparison.OrdinalIgnoreCase))
        {
            MenuEstabelecimento.Exibir(usuario);
            return;
        }

        if (string.Equals(usuario.Tipo, TipoInstituicao, StringComparison.OrdinalIgnoreCase))
        {
            MenuInstituicao.Exibir(usuario);
            return;
        }

        Console.WriteLine("Tipo de conta não reconhecido.");
        AguardarContinuacao();
    }

    private static async Task CadastrarContaAsync(AuthService authService)
    {
        Console.Clear();
        Console.WriteLine("========== CADASTRO ==========");

        string nome = LerCampo("Nome: ");
        string email = LerCampo("E-mail: ");
        string senha = LerCampo("Senha: ");
        string telefone = LerCampo("Telefone: ");
        string endereco = LerCampo("Endereço: ");
        string tipo = EscolherTipoConta();

        try
        {
            await authService.CadastrarUsuarioAsync(
                nome,
                email,
                senha,
                telefone,
                endereco,
                tipo);

            Console.WriteLine("Conta cadastrada com sucesso.");
        }
        catch (ArgumentException exception)
        {
            Console.WriteLine(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            Console.WriteLine(exception.Message);
        }

        AguardarContinuacao();
    }

    private static string EscolherTipoConta()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Tipo de conta:");
            Console.WriteLine("1 - ESTABELECIMENTO");
            Console.WriteLine("2 - INSTITUICAO");

            int opcao = Utils.LerInteiro("Escolha uma opção: ");

            switch (opcao)
            {
                case 1:
                    return TipoEstabelecimento;
                case 2:
                    return TipoInstituicao;
                default:
                    Console.WriteLine("Opção inválida.");
                    break;
            }
        }
    }

    private static string LerCampo(string mensagem)
    {
        Console.Write(mensagem);
        return Console.ReadLine() ?? string.Empty;
    }

    private static void AguardarContinuacao()
    {
        Console.WriteLine();
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }
}
