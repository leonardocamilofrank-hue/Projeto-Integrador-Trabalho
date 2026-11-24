using System.Globalization;

namespace MesaViva;

public static class Utils
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public static void LimparTela()
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
    }

    public static string LerCampo(string mensagem)
    {
        Console.Write(mensagem);
        return Console.ReadLine() ?? throw new EndOfStreamException();
    }

    public static void AguardarContinuacao()
    {
        Console.WriteLine();
        LerCampo("Pressione Enter para continuar.");
    }

    public static string LerTexto(string mensagem)
    {
        while (true)
        {
            string entrada = LerCampo(mensagem);
            if (!string.IsNullOrWhiteSpace(entrada))
            {
                return entrada;
            }

            Console.WriteLine("Erro: Este campo não pode ficar vazio!");
        }
    }

    public static int LerInteiro(string mensagem)
    {
        int numero;
        while (!int.TryParse(LerCampo(mensagem), out numero) || numero < 0)
        {
            Console.WriteLine(numero < 0
                ? "O número não pode ser negativo."
                : "Erro: Digite um número inteiro válido!");
        }

        return numero;
    }

    public static decimal LerDecimal(string mensagem)
    {
        const NumberStyles formato = NumberStyles.Number & ~NumberStyles.AllowThousands;
        decimal numero;
        while (!decimal.TryParse(LerCampo(mensagem), formato, Cultura, out numero) || numero <= 0)
        {
            Console.WriteLine("Digite uma quantidade em kg maior que zero (ex.: 2,50).");
        }

        return numero;
    }

    public static DateTime LerDateTime(string mensagem)
    {
        DateTime data;
        while (!DateTime.TryParseExact(LerCampo(mensagem), "dd/MM/yyyy HH:mm", Cultura, DateTimeStyles.AllowWhiteSpaces, out data))
        {
            Console.WriteLine("Digite a data e o horário no formato dd/MM/aaaa HH:mm.");
        }

        return data;
    }

    public static string FormatarQuantidade(decimal quantidade)
    {
        return quantidade.ToString("0.##", Cultura) + " kg";
    }
}
