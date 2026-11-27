using System.Globalization;

namespace MesaViva;

public static class Utils
{
    public const string FormatoData = "dd/MM/yyyy HH:mm";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public static string FormatarQuantidade(decimal quantidade)
    {
        return quantidade.ToString("0.##", Cultura) + " kg";
    }

    public static string FormatarData(DateTime data)
    {
        return data.ToString(FormatoData, Cultura);
    }
}
