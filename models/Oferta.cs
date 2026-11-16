namespace MesaViva.Models;

public class Oferta
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    public string Alimento { get; set; } = string.Empty;
    public decimal QuantidadeTotal { get; set; }
    public DateTime Validade { get; set; }
    public DateTime InicioRetirada { get; set; }
    public DateTime FimRetirada { get; set; }
}
