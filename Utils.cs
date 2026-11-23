namespace MesaViva;
public static class Utils
{
    public static string LerTexto(string mensagem)
{
    string? entrada;
    do
    {
        Console.Write(mensagem);
        entrada = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(entrada))
        {
            Console.WriteLine("Erro: Este campo não pode ficar vazio!");
        }
        else if (!entrada.All(c => char.IsLetter(c) || c == ' '))
        {
            Console.WriteLine("Utilize apenas letras ou espaçamento quando necessário.");

            entrada = null;
        }

    } while (string.IsNullOrWhiteSpace(entrada));

    return entrada;
}

public static int LerInteiro(string mensagem)
{
    int numero;
    Console.Write(mensagem);

    while (!int.TryParse(Console.ReadLine(), out numero) || numero < 0)
    {
        if (numero < 0)
            {
                Console.WriteLine("O número não pode ser negativo. ");
            }
            else
            {
                Console.WriteLine("Erro: Digite um número inteiro válido!");  
            }
        Console.Write(mensagem);
    }

    return numero;
}

public static decimal LerDecimal(string mensagem)
{
    decimal numero;
    Console.Write(mensagem);

    while (!decimal.TryParse(Console.ReadLine(), out numero) || numero <= 0)
    {
        Console.WriteLine("Erro: Digite um preço válido e maior que zero!");
        Console.Write(mensagem);
    }

    return numero;
}
public static DateTime LerDateTime(string mensagem)
{
    DateTime data;
    Console.Write(mensagem);

    // Garante que o texto digitado seja convertido corretamente para DateTime
    while (!DateTime.TryParse(Console.ReadLine(), out data))
    {
        Console.WriteLine("Erro: Digite uma data válida (ex: 25/12/2026)!");
        Console.Write(mensagem);
    }

    return data;
}

}