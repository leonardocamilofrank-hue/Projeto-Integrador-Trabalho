using MesaViva.Models;

namespace MesaViva.UI;

public static class MenuInstituicao
{
    public static void Exibir(Usuario instituicao)
    {
        bool continuar = true;

        while (continuar)
        {
            Console.Clear();
            Console.WriteLine("========== MENU DA INSTITUIÇÃO ==========");
            Console.WriteLine($"Instituição: {instituicao.Nome}");
            Console.WriteLine("1 - Consultar ofertas disponíveis");
            Console.WriteLine("2 - Reservar oferta");
            Console.WriteLine("3 - Listar minhas reservas");
            Console.WriteLine("4 - Cancelar reserva");
            Console.WriteLine("5 - Confirmar coleta");
            Console.WriteLine("6 - Voltar");
            Console.WriteLine("0 - Sair do menu");

            int opcao = Utils.LerInteiro("Escolha uma opção: ");

            switch (opcao)
            {
                case 1:
                    MostrarOperacaoPendente("Consulta de ofertas", "OfertaService");
                    break;
                case 2:
                    MostrarOperacaoPendente("Reserva de oferta", "ReservaService");
                    break;
                case 3:
                    MostrarOperacaoPendente("Listagem de reservas", "ReservaService");
                    break;
                case 4:
                    MostrarOperacaoPendente("Cancelamento de reserva", "ReservaService");
                    break;
                case 5:
                    MostrarOperacaoPendente("Confirmação de coleta", "ReservaService");
                    break;
                case 6:
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

    private static void MostrarOperacaoPendente(string operacao, string serviceNecessario)
    {
        Console.WriteLine();
        Console.WriteLine($"{operacao} aguardando implementação no {serviceNecessario}.");
        AguardarContinuacao();
    }

    private static void AguardarContinuacao()
    {
        Console.WriteLine();
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }
}
