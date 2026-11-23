namespace MesaViva.Models;

public class Reserva
{
    private const string StatusReservada = "RESERVADA";
    private const string StatusCancelada = "CANCELADA";
    private const string StatusColetada = "COLETADA";

    public int Id { get; set; }
    public int OfertaId { get; set; }
    public int InstituicaoId { get; set; }
    public decimal QuantidadeReservada { get; set; }
    public DateTime DataReserva { get; set; }
    public DateTime? DataColeta { get; set; }
    public string Status { get; set; } = StatusReservada;

    public void Validar()
    {
        if (QuantidadeReservada <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(QuantidadeReservada),
                "A quantidade reservada deve ser maior que zero.");
        }
    }

    public void Cancelar()
    {
        if (Status != StatusReservada)
        {
            throw new InvalidOperationException(
                "Somente reservas com status RESERVADA podem ser canceladas.");
        }

        Status = StatusCancelada;
    }

    public void ConfirmarColeta(DateTime dataColeta)
    {
        if (Status != StatusReservada)
        {
            throw new InvalidOperationException(
                "Somente reservas com status RESERVADA podem ter a coleta confirmada.");
        }

        Status = StatusColetada;
        DataColeta = dataColeta;
    }
}
