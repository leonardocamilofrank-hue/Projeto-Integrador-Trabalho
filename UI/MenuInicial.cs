using MesaViva.Models;
using MesaViva.Services;

namespace MesaViva.UI;

public static class MenuInicial
{
    public static async Task ExibirAsync(AuthService authService, OfertaService ofertaService, ReservaService reservaService)
    {
        while (true)
        {
            Utils.LimparTela();
            Console.WriteLine("========== MESAVIVA ==========");
            Console.WriteLine("1 - Fazer login");
            Console.WriteLine("2 - Cadastrar conta");
            Console.WriteLine("0 - Sair");

            int opcao = Utils.LerInteiro("Escolha uma opção: ");

            try
            {
                switch (opcao)
                {
                    case 1:
                        await FazerLoginAsync(authService, ofertaService, reservaService);
                        break;
                    case 2:
                        await CadastrarContaAsync(authService);
                        break;
                    case 0:
                        return;
                    default:
                        Console.WriteLine("Opção inválida.");
                        Utils.AguardarContinuacao();
                        break;
                }
            }
            catch (Exception erro) when (MenuComum.EhErroOperacao(erro))
            {
                MenuComum.ExibirErroOperacao(erro);
                Utils.AguardarContinuacao();
            }
        }
    }

    private static async Task FazerLoginAsync(AuthService authService, OfertaService ofertaService, ReservaService reservaService)
    {
        Utils.LimparTela();
        Console.WriteLine("========== LOGIN ==========");

        string email = Utils.LerCampo("E-mail: ");
        string senha = Utils.LerCampo("Senha: ");

        Usuario? usuario = await authService.FazerLoginAsync(email, senha);

        if (usuario is null)
        {
            Console.WriteLine("E-mail ou senha inválidos.");
            Utils.AguardarContinuacao();
            return;
        }

        if (usuario.Tipo == Usuario.TipoEstabelecimento)
        {
            await MenuEstabelecimento.ExibirAsync(usuario, authService, ofertaService, reservaService);
            return;
        }

        if (usuario.Tipo == Usuario.TipoInstituicao)
        {
            await MenuInstituicao.ExibirAsync(usuario, authService, ofertaService, reservaService);
            return;
        }

        Console.WriteLine("Tipo de conta não reconhecido.");
        Utils.AguardarContinuacao();
    }

    private static async Task CadastrarContaAsync(AuthService authService)
    {
        Utils.LimparTela();
        Console.WriteLine("========== CADASTRO ==========");

        string nome = Utils.LerCampo("Nome: ");
        string email = Utils.LerCampo("E-mail: ");
        string senha = Utils.LerCampo("Senha: ");
        string telefone = Utils.LerCampo("Telefone: ");
        string endereco = Utils.LerCampo("Endereço: ");
        string tipo = EscolherTipoConta();

        await authService.CadastrarUsuarioAsync(nome, email, senha, telefone, endereco, tipo);
        Console.WriteLine("Conta cadastrada com sucesso.");
        Utils.AguardarContinuacao();
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
                    return Usuario.TipoEstabelecimento;
                case 2:
                    return Usuario.TipoInstituicao;
                default:
                    Console.WriteLine("Opção inválida.");
                    break;
            }
        }
    }
}
