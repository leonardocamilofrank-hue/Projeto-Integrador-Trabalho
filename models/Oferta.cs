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

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Alimento))
        {
            throw new ArgumentException("O nome do alimento é obrigatório.", nameof(Alimento));
        }

        if (QuantidadeTotal <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(QuantidadeTotal),
                "A quantidade total deve ser maior que zero.");
        }

        if (Validade == default)
        {
            throw new ArgumentException("Informe a data de validade.", nameof(Validade));
        }

        if (InicioRetirada == default || FimRetirada == default)
        {
            throw new ArgumentException("Informe o início e o fim da retirada.");
        }

        if (InicioRetirada >= FimRetirada)
        {
            throw new ArgumentException(
                "O início da retirada deve ser anterior ao fim.",
                nameof(InicioRetirada));
        }

        if (Validade < InicioRetirada)
        {
            throw new ArgumentException(
                "A validade do alimento não pode ser anterior ao início da retirada.",
                nameof(Validade));
        }
    }

    public decimal CalcularQuantidadeDisponivel(IEnumerable<Reserva> reservas)
    {
        if (reservas is null)
        {
            throw new ArgumentNullException(nameof(reservas));
        }

        decimal quantidadeReservada = reservas
            .Where(reserva => reserva.OfertaId == Id
                && (reserva.Status == "RESERVADA" || reserva.Status == "COLETADA"))
            .Sum(reserva => reserva.QuantidadeReservada);

        return Math.Max(0, QuantidadeTotal - quantidadeReservada);
    }

    public bool EstaUrgente(IEnumerable<Reserva> reservas, DateTime agora)
    {
        bool retiradaProxima = FimRetirada > agora
            && FimRetirada <= agora.AddHours(2);

        return retiradaProxima
            && Validade >= agora
            && CalcularQuantidadeDisponivel(reservas) > 0;
    }
}
