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
    public ICollection<Reserva> Reservas { get; } = new List<Reserva>();

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Alimento))
        {
            throw new ArgumentException("O nome do alimento é obrigatório.", nameof(Alimento));
        }

        QuantidadeEmKg.Validar(QuantidadeTotal, nameof(QuantidadeTotal), "A quantidade total");

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
        return QuantidadeTotal - CalcularQuantidadeComprometida(reservas);
    }

    internal decimal CalcularQuantidadeComprometida(IEnumerable<Reserva> reservas)
    {
        ArgumentNullException.ThrowIfNull(reservas);

        return reservas
            .Where(reserva => reserva.OfertaId == Id
                && reserva.Status is Reserva.StatusReservada or Reserva.StatusColetada)
            .Sum(reserva => reserva.QuantidadeReservada);
    }

    internal bool PodeReservarEm(DateTime agora)
    {
        return Validade >= agora && FimRetirada > agora;
    }

    public bool EstaUrgente(IEnumerable<Reserva> reservas, DateTime agora)
    {
        return PodeReservarEm(agora)
            && FimRetirada <= agora.AddHours(2)
            && CalcularQuantidadeDisponivel(reservas) > 0;
    }
}
