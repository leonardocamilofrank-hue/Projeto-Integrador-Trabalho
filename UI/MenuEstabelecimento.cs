using MesaViva.Models;
using MesaViva.Services;

namespace MesaViva.UI;

public static class MenuEstabelecimento
{
    public static async Task ExibirAsync(
        Usuario estabelecimento, AuthService authService, OfertaService ofertaService, ReservaService reservaService)
    {
        while (true)
        {
            Utils.LimparTela();
            Console.WriteLine("========== MENU DO ESTABELECIMENTO ==========");
            Console.WriteLine($"Estabelecimento: {estabelecimento.Nome}");
            Console.WriteLine("1 - Cadastrar oferta");
            Console.WriteLine("2 - Listar minhas ofertas");
            Console.WriteLine("3 - Editar oferta");
            Console.WriteLine("4 - Excluir oferta");
            Console.WriteLine("5 - Consultar reservas das minhas ofertas");
            Console.WriteLine("6 - Editar meus dados");
            Console.WriteLine("0 - Voltar");

            int opcao = Utils.LerInteiro("Escolha uma opção: ");

            try
            {
                switch (opcao)
                {
                    case 1:
                        await CadastrarOfertaAsync(estabelecimento, ofertaService);
                        break;
                    case 2:
                        MenuComum.ExibirOfertas(await ofertaService.ListarMinhasOfertasAsync(estabelecimento));
                        break;
                    case 3:
                        await EditarOfertaAsync(estabelecimento, ofertaService);
                        break;
                    case 4:
                        int ofertaId = Utils.LerInteiro("ID da oferta: ");
                        await ofertaService.ExcluirOfertaAsync(estabelecimento, ofertaId);
                        Console.WriteLine("Oferta excluída com sucesso.");
                        break;
                    case 5:
                        MenuComum.ExibirReservas(await reservaService.ListarReservasRecebidasAsync(estabelecimento), recebidas: true);
                        break;
                    case 6:
                        estabelecimento = await MenuComum.EditarDadosAsync(authService, estabelecimento);
                        break;
                    case 0:
                        return;
                    default:
                        Console.WriteLine("Opção inválida.");
                        break;
                }
            }
            catch (Exception erro) when (MenuComum.EhErroOperacao(erro))
            {
                MenuComum.ExibirErroOperacao(erro);
            }

            Utils.AguardarContinuacao();
        }
    }

    private static async Task CadastrarOfertaAsync(Usuario estabelecimento, OfertaService ofertaService)
    {
        var (alimento, quantidade, validade, inicio, fim) = LerDadosOferta();
        Oferta oferta = await ofertaService.CadastrarOfertaAsync(estabelecimento, alimento, quantidade, validade, inicio, fim);
        Console.WriteLine($"Oferta #{oferta.Id} cadastrada com sucesso.");
    }

    private static async Task EditarOfertaAsync(Usuario estabelecimento, OfertaService ofertaService)
    {
        int ofertaId = Utils.LerInteiro("ID da oferta: ");
        var (alimento, quantidade, validade, inicio, fim) = LerDadosOferta();
        await ofertaService.EditarOfertaAsync(estabelecimento, ofertaId, alimento, quantidade, validade, inicio, fim);
        Console.WriteLine("Oferta atualizada com sucesso.");
    }

    private static (string, decimal, DateTime, DateTime, DateTime) LerDadosOferta()
    {
        string alimento = Utils.LerCampo("Alimento: ");
        decimal quantidade = Utils.LerDecimal("Quantidade total (kg): ");
        DateTime validade = Utils.LerDateTime("Validade (dd/MM/aaaa HH:mm): ");
        DateTime inicio = Utils.LerDateTime("Início da retirada (dd/MM/aaaa HH:mm): ");
        DateTime fim = Utils.LerDateTime("Fim da retirada (dd/MM/aaaa HH:mm): ");
        return (alimento, quantidade, validade, inicio, fim);
    }
}
