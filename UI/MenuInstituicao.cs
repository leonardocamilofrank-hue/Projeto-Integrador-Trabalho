using MesaViva.Models;
using MesaViva.Services;

namespace MesaViva.UI;

public static class MenuInstituicao
{
    public static async Task ExibirAsync(
        Usuario instituicao, AuthService authService, OfertaService ofertaService, ReservaService reservaService)
    {
        while (true)
        {
            Utils.LimparTela();
            Console.WriteLine("========== MENU DA INSTITUIÇÃO ==========");
            Console.WriteLine($"Instituição: {instituicao.Nome}");
            Console.WriteLine("1 - Consultar ofertas disponíveis");
            Console.WriteLine("2 - Reservar oferta");
            Console.WriteLine("3 - Listar minhas reservas");
            Console.WriteLine("4 - Cancelar reserva");
            Console.WriteLine("5 - Confirmar coleta");
            Console.WriteLine("6 - Consultar ofertas urgentes");
            Console.WriteLine("7 - Editar meus dados");
            Console.WriteLine("0 - Sair do menu");

            int opcao = Utils.LerInteiro("Escolha uma opção: ");

            try
            {
                switch (opcao)
                {
                    case 1:
                        MenuComum.ExibirOfertas(await ofertaService.ListarOfertasDisponiveisAsync(instituicao));
                        break;
                    case 2:
                        int ofertaId = Utils.LerInteiro("ID da oferta: ");
                        decimal quantidade = Utils.LerDecimal("Quantidade a reservar (kg): ");
                        Reserva reserva = await reservaService.CriarReservaAsync(instituicao, ofertaId, quantidade);
                        Console.WriteLine($"Reserva #{reserva.Id} realizada com sucesso.");
                        break;
                    case 3:
                        MenuComum.ExibirReservas(await reservaService.ListarMinhasReservasAsync(instituicao), recebidas: false);
                        break;
                    case 4:
                        await reservaService.CancelarReservaAsync(instituicao, Utils.LerInteiro("ID da reserva: "));
                        Console.WriteLine("Reserva cancelada com sucesso.");
                        break;
                    case 5:
                        await reservaService.ConfirmarColetaAsync(instituicao, Utils.LerInteiro("ID da reserva: "));
                        Console.WriteLine("Coleta confirmada com sucesso.");
                        break;
                    case 6:
                        MenuComum.ExibirOfertas(await ofertaService.ListarOfertasUrgentesAsync(instituicao));
                        break;
                    case 7:
                        instituicao = await MenuComum.EditarDadosAsync(authService, instituicao);
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
}
