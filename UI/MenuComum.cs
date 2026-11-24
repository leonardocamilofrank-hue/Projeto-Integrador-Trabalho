using System.Data.Common;
using System.Security.Authentication;
using MesaViva.Models;
using MesaViva.Services;
using Microsoft.EntityFrameworkCore;

namespace MesaViva.UI;

internal static class MenuComum
{
    public static async Task<Usuario> EditarDadosAsync(AuthService authService, Usuario usuario)
    {
        Console.WriteLine("Pressione Enter para manter o valor atual de um campo.");
        string nome = LerDado("Nome", usuario.Nome);
        string email = LerDado("E-mail", usuario.Email);
        string telefone = LerDado("Telefone", usuario.Telefone);
        string endereco = LerDado("Endereço", usuario.Endereco);

        Usuario cadastro = await authService.AtualizarDadosAsync(usuario, nome, email, telefone, endereco);
        Console.WriteLine("Dados atualizados com sucesso.");
        return cadastro;
    }

    public static void ExibirOfertas(List<OfertaComDisponibilidade> ofertas)
    {
        if (ofertas.Count == 0)
        {
            Console.WriteLine("Nenhuma oferta encontrada.");
        }

        foreach (var item in ofertas)
        {
            Oferta oferta = item.Oferta;
            Console.WriteLine($"#{oferta.Id} | {oferta.Alimento} | Disponível: {Utils.FormatarQuantidade(item.QuantidadeDisponivel)} de {Utils.FormatarQuantidade(oferta.QuantidadeTotal)}");
            Console.WriteLine($"  Validade: {oferta.Validade:dd/MM/yyyy HH:mm} | Retirada: {oferta.InicioRetirada:dd/MM/yyyy HH:mm} até {oferta.FimRetirada:dd/MM/yyyy HH:mm}");
        }
    }

    public static void ExibirReservas(List<ReservaDetalhada> reservas, bool recebidas)
    {
        if (reservas.Count == 0)
        {
            Console.WriteLine("Nenhuma reserva encontrada.");
        }

        foreach (var item in reservas)
        {
            Reserva reserva = item.Reserva;
            string parte = recebidas ? $"Instituição: {item.Instituicao}" : $"Estabelecimento: {item.Estabelecimento}";
            Console.WriteLine($"#{reserva.Id} | Oferta #{reserva.OfertaId}: {item.Alimento} | {Utils.FormatarQuantidade(reserva.QuantidadeReservada)} | {reserva.Status}");
            Console.WriteLine($"  {parte} | Reserva em {reserva.DataReserva:dd/MM/yyyy HH:mm}");
            if (reserva.DataColeta is DateTime dataColeta)
            {
                Console.WriteLine($"  Coleta em {dataColeta:dd/MM/yyyy HH:mm}");
            }
        }
    }

    public static bool EhErroOperacao(Exception erro)
    {
        return erro is ArgumentException or InvalidOperationException or DbException or DbUpdateException or AuthenticationException;
    }

    public static void ExibirErroOperacao(Exception erro)
    {
        Console.WriteLine(erro is DbException or DbUpdateException or AuthenticationException
            ? "Não foi possível concluir a operação no banco de dados. Tente novamente."
            : erro.Message);
    }

    private static string LerDado(string campo, string atual)
    {
        string valor = Utils.LerCampo($"{campo} [{atual}]: ");
        return string.IsNullOrWhiteSpace(valor) ? atual : valor;
    }
}
