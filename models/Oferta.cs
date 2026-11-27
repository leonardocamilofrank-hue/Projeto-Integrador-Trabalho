using System.Text;

namespace MesaViva.Models;

public enum SituacaoOferta
{
    Vencida,
    Encerrada,
    Esgotada,
    Urgente,
    Disponivel
}

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
        ValidarDados();
        ValidarNomeSemNumeros();
    }

    internal void ValidarNomeSemNumeros()
    {
        if (Alimento.EnumerateRunes().Any(Rune.IsNumber))
            throw new ArgumentException("O nome do alimento não pode conter números.", nameof(Alimento));
    }

    internal void ValidarDados()
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

        if (Validade.Year < 1000 || InicioRetirada.Year < 1000 || FimRetirada.Year < 1000)
        {
            throw new ArgumentException("Informe datas de validade e retirada com ano a partir de 1000.");
        }

        if (InicioRetirada > Validade || FimRetirada > Validade)
        {
            throw new ArgumentException("O período de retirada não pode ultrapassar a validade do alimento.");
        }

        if (InicioRetirada >= FimRetirada)
        {
            throw new ArgumentException(
                "O início da retirada deve ser anterior ao fim.",
                nameof(InicioRetirada));
        }
    }

    public decimal CalcularQuantidadeDisponivel(IEnumerable<Reserva> reservas)
    {
        // O saldo aritmético pode ser positivo mesmo após o encerramento da retirada.
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
        return ObterSituacao(CalcularQuantidadeDisponivel(reservas), agora) == SituacaoOferta.Urgente;
    }

    public SituacaoOferta ObterSituacao(decimal saldoRestante, DateTime agora)
    {
        if (Validade < agora)
            return SituacaoOferta.Vencida;
        if (!PodeReservarEm(agora))
            return SituacaoOferta.Encerrada;
        if (saldoRestante <= 0)
            return SituacaoOferta.Esgotada;

        return FimRetirada - agora <= TimeSpan.FromHours(2)
            ? SituacaoOferta.Urgente
            : SituacaoOferta.Disponivel;
    }
}
