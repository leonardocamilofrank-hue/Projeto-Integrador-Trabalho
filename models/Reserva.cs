namespace MesaViva.Models;

public class Reserva
{
    public int Id { get; set; }
    public int OfertaId { get; set; }
    public int InstituicaoId { get; set; }
    public decimal QuantidadeReservada { get; set; }
    public DateTime DataReserva { get; set; }
    public DateTime? DataColeta { get; set; }
    public string Status { get; set; } = "RESERVADA";
}
