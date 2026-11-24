namespace MesaViva.Models;

public class Reserva
{
    public const string StatusReservada = "RESERVADA";
    public const string StatusCancelada = "CANCELADA";
    public const string StatusColetada = "COLETADA";

    public int Id { get; set; }
    public int OfertaId { get; set; }
    public int InstituicaoId { get; set; }
    public decimal QuantidadeReservada { get; set; }
    public DateTime DataReserva { get; set; }
    public DateTime? DataColeta { get; set; }
    public string Status { get; set; } = StatusReservada;

    public void Validar()
    {
        QuantidadeEmKg.Validar(QuantidadeReservada, nameof(QuantidadeReservada), "A quantidade reservada");
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
