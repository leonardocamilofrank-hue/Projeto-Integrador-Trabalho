namespace MesaViva.Models;

internal static class QuantidadeEmKg
{
    public static void Validar(decimal quantidade, string parametro, string descricao)
    {
        if (quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(parametro, $"{descricao} deve ser maior que zero.");
        }

        // As colunas de quantidade no MySQL são DECIMAL(10,2).
        if (quantidade > 99_999_999.99m || decimal.Round(quantidade, 2) != quantidade)
        {
            throw new ArgumentOutOfRangeException(
                parametro,
                $"{descricao} deve ser de até 99.999.999,99 kg e ter no máximo duas casas decimais.");
        }
    }
}
