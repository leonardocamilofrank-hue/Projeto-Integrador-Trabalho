using System.Data;
using MesaViva.Data;
using MesaViva.Models;
using Microsoft.EntityFrameworkCore;

namespace MesaViva.Services;

public sealed record ReservaDetalhada(Reserva Reserva, string Alimento, string Estabelecimento, string Instituicao);

public class ReservaService
{
    public async Task<Reserva> CriarReservaAsync(Usuario instituicao, int ofertaId, decimal quantidade)
    {
        instituicao.ExigirTipo(Usuario.TipoInstituicao);
        var reserva = new Reserva
        {
            OfertaId = ofertaId,
            InstituicaoId = instituicao.Id,
            QuantidadeReservada = quantidade
        };
        reserva.Validar();

        await using var context = new AppDbContext();
        await using var transacao = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        Oferta oferta = await context.BuscarOfertaParaAtualizarAsync(ofertaId)
            ?? throw new InvalidOperationException("Oferta não encontrada.");
        await context.Entry(oferta).Collection(item => item.Reservas).LoadAsync();

        DateTime agora = DateTime.Now;
        if (!oferta.PodeReservarEm(agora))
        {
            throw new InvalidOperationException("O alimento está vencido ou o período de retirada terminou.");
        }

        decimal quantidadeDisponivel = oferta.CalcularQuantidadeDisponivel(oferta.Reservas);
        if (quantidade > quantidadeDisponivel)
        {
            throw new InvalidOperationException("A quantidade solicitada supera a quantidade disponível.");
        }

        reserva.DataReserva = agora;
        context.Reservas.Add(reserva);
        await context.SaveChangesAsync();
        await transacao.CommitAsync();
        return reserva;
    }

    public async Task<List<ReservaDetalhada>> ListarMinhasReservasAsync(Usuario instituicao)
    {
        instituicao.ExigirTipo(Usuario.TipoInstituicao);
        await using var context = new AppDbContext();
        return await ConsultarReservas(context, instituicaoId: instituicao.Id).ToListAsync();
    }

    public async Task<List<ReservaDetalhada>> ListarReservasRecebidasAsync(Usuario estabelecimento)
    {
        estabelecimento.ExigirTipo(Usuario.TipoEstabelecimento);
        await using var context = new AppDbContext();
        return await ConsultarReservas(context, estabelecimentoId: estabelecimento.Id).ToListAsync();
    }

    public Task CancelarReservaAsync(Usuario instituicao, int reservaId)
    {
        return AlterarReservaAsync(instituicao, reservaId, reserva => reserva.Cancelar());
    }

    public Task ConfirmarColetaAsync(Usuario instituicao, int reservaId)
    {
        return AlterarReservaAsync(instituicao, reservaId, reserva => reserva.ConfirmarColeta(DateTime.Now));
    }

    private static IQueryable<ReservaDetalhada> ConsultarReservas(
        AppDbContext context, int? estabelecimentoId = null, int? instituicaoId = null)
    {
        return from reserva in context.Reservas.AsNoTracking()
               join oferta in context.Ofertas on reserva.OfertaId equals oferta.Id
               join estabelecimento in context.Usuarios on oferta.EstabelecimentoId equals estabelecimento.Id
               join instituicao in context.Usuarios on reserva.InstituicaoId equals instituicao.Id
               where estabelecimentoId == null || oferta.EstabelecimentoId == estabelecimentoId
               where instituicaoId == null || reserva.InstituicaoId == instituicaoId
               orderby reserva.DataReserva descending, reserva.Id descending
               select new ReservaDetalhada(reserva, oferta.Alimento, estabelecimento.Nome, instituicao.Nome);
    }

    private static async Task AlterarReservaAsync(Usuario instituicao, int reservaId, Action<Reserva> alterar)
    {
        instituicao.ExigirTipo(Usuario.TipoInstituicao);
        await using var context = new AppDbContext();
        await using var transacao = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var consulta = context.Reservas
            .Where(reserva => reserva.Id == reservaId && reserva.InstituicaoId == instituicao.Id);
        int? ofertaId = await consulta.Select(reserva => (int?)reserva.OfertaId).SingleOrDefaultAsync();
        if (ofertaId is null)
        {
            throw new InvalidOperationException("Reserva não encontrada entre as suas reservas.");
        }

        // Releia a reserva após obter o bloqueio para usar o status mais recente.
        await context.BuscarOfertaParaAtualizarAsync(ofertaId.Value);
        Reserva reserva = await consulta.SingleAsync();
        alterar(reserva);
        await context.SaveChangesAsync();
        await transacao.CommitAsync();
    }
}
