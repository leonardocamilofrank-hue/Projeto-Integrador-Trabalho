using MesaViva.Models;

namespace MesaViva.UI;

public static class MenuEstabelecimento
{
    public static void Exibir(Usuario estabelecimento)
    {
        bool continuar = true;

        while (continuar)
        {
            Console.Clear();
            Console.WriteLine("========== MENU DO ESTABELECIMENTO ==========");
            Console.WriteLine($"Estabelecimento: {estabelecimento.Nome}");
            Console.WriteLine("1 - Cadastrar oferta");
            Console.WriteLine("2 - Listar minhas ofertas");
            Console.WriteLine("3 - Editar oferta");
            Console.WriteLine("4 - Excluir oferta");
            Console.WriteLine("5 - Consultar reservas das minhas ofertas");
            Console.WriteLine("0 - Voltar");

            int opcao = Utils.LerInteiro("Escolha uma opção: ");

            switch (opcao)
            {
                case 1:
                    MostrarOperacaoPendente("Cadastro de oferta", "OfertaService");
                    break;
                case 2:
                    MostrarOperacaoPendente("Listagem de ofertas", "OfertaService");
                    break;
                case 3:
                    MostrarOperacaoPendente("Edição de oferta", "OfertaService");
                    break;
                case 4:
                    MostrarOperacaoPendente("Exclusão de oferta", "OfertaService");
                    break;
                case 5:
                    MostrarOperacaoPendente("Consulta de reservas", "ReservaService");
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
