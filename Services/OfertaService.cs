using System.Data;
using MesaViva.Data;
using MesaViva.Models;
using Microsoft.EntityFrameworkCore;

namespace MesaViva.Services;

public sealed record OfertaComDisponibilidade(
    Oferta Oferta, decimal SaldoRestante, SituacaoOferta Situacao, string? Estabelecimento = null)
{
    public decimal QuantidadeDisponivel =>
        Situacao is SituacaoOferta.Disponivel or SituacaoOferta.Urgente ? SaldoRestante : 0;
}

public class OfertaService
{
    public async Task<Oferta> CadastrarOfertaAsync(
        Usuario estabelecimento, string alimento, decimal quantidadeTotal,
        DateTime validade, DateTime inicioRetirada, DateTime fimRetirada)
    {
        estabelecimento.ExigirTipo(Usuario.TipoEstabelecimento);
        Oferta oferta = MontarOferta(alimento, quantidadeTotal, validade, inicioRetirada, fimRetirada);
        oferta.Validar();
        if (!oferta.PodeReservarEm(DateTime.Now))
        {
            throw new InvalidOperationException(
                "Não é possível cadastrar uma oferta de alimento vencido ou com o período de retirada encerrado.");
        }

        oferta.EstabelecimentoId = estabelecimento.Id;

        await using var context = new AppDbContext();
        context.Ofertas.Add(oferta);
        await context.SaveChangesAsync();
        return oferta;
    }

    public async Task<List<OfertaComDisponibilidade>> ListarMinhasOfertasAsync(Usuario estabelecimento)
    {
        estabelecimento.ExigirTipo(Usuario.TipoEstabelecimento);
        await using var context = new AppDbContext();
        var ofertas = await context.Ofertas.AsNoTracking()
            .Where(oferta => oferta.EstabelecimentoId == estabelecimento.Id)
            .Include(oferta => oferta.Reservas)
            .OrderBy(oferta => oferta.FimRetirada)
            .ThenBy(oferta => oferta.Id)
            .ToListAsync();

        DateTime agora = DateTime.Now;
        return ofertas.Select(oferta => ComDisponibilidade(oferta, oferta.Reservas, agora)).ToList();
    }

    public Task<List<OfertaComDisponibilidade>> ListarOfertasDisponiveisAsync(Usuario instituicao)
    {
        return ListarOfertasParaInstituicaoAsync(instituicao, apenasUrgentes: false);
    }

    public Task<List<OfertaComDisponibilidade>> ListarOfertasUrgentesAsync(Usuario instituicao)
    {
        return ListarOfertasParaInstituicaoAsync(instituicao, apenasUrgentes: true);
    }

    public async Task EditarOfertaAsync(
        Usuario estabelecimento, int ofertaId, string alimento, decimal quantidadeTotal,
        DateTime validade, DateTime inicioRetirada, DateTime fimRetirada)
    {
        estabelecimento.ExigirTipo(Usuario.TipoEstabelecimento);
        Oferta novosDados = MontarOferta(alimento, quantidadeTotal, validade, inicioRetirada, fimRetirada);
        novosDados.ValidarDados();

        await using var context = new AppDbContext();
        await using var transacao = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        Oferta oferta = await BuscarOfertaPropriaAsync(context, estabelecimento.Id, ofertaId);
        await context.Entry(oferta).Collection(item => item.Reservas).LoadAsync();

        bool possuiHistorico = oferta.Reservas.Count > 0;
        bool alterouAlimento = !string.Equals(oferta.Alimento.Trim(), novosDados.Alimento, StringComparison.Ordinal);
        if (possuiHistorico && alterouAlimento)
        {
            throw new InvalidOperationException("Não é possível alterar o alimento de uma oferta que já possui reservas registradas.");
        }
        if (alterouAlimento)
            novosDados.ValidarNomeSemNumeros();

        if (novosDados.QuantidadeTotal < oferta.CalcularQuantidadeComprometida(oferta.Reservas))
        {
            throw new InvalidOperationException("A quantidade total não pode ser menor que a quantidade já comprometida.");
        }

        if (!possuiHistorico)
            oferta.Alimento = novosDados.Alimento;
        oferta.QuantidadeTotal = novosDados.QuantidadeTotal;
        oferta.Validade = novosDados.Validade;
        oferta.InicioRetirada = novosDados.InicioRetirada;
        oferta.FimRetirada = novosDados.FimRetirada;
        await context.SaveChangesAsync();
        await transacao.CommitAsync();
    }

    public async Task ExcluirOfertaAsync(Usuario estabelecimento, int ofertaId)
    {
        estabelecimento.ExigirTipo(Usuario.TipoEstabelecimento);
        await using var context = new AppDbContext();
        await using var transacao = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        Oferta oferta = await BuscarOfertaPropriaAsync(context, estabelecimento.Id, ofertaId);

        if (await context.Reservas.AnyAsync(reserva => reserva.OfertaId == oferta.Id))
        {
            throw new InvalidOperationException("Ofertas com reservas registradas não podem ser excluídas.");
        }

        context.Ofertas.Remove(oferta);
        await context.SaveChangesAsync();
        await transacao.CommitAsync();
    }

    private static async Task<List<OfertaComDisponibilidade>> ListarOfertasParaInstituicaoAsync(
        Usuario instituicao, bool apenasUrgentes)
    {
        instituicao.ExigirTipo(Usuario.TipoInstituicao);
        await using var context = new AppDbContext();
        DateTime agora = DateTime.Now;
        var consulta = from oferta in context.Ofertas.AsNoTracking()
                       join estabelecimento in context.Usuarios on oferta.EstabelecimentoId equals estabelecimento.Id
                       where oferta.Validade >= agora && oferta.FimRetirada > agora
                       orderby oferta.FimRetirada, oferta.Id
                       select new
                       {
                           Oferta = oferta,
                           Estabelecimento = estabelecimento.Nome,
                           Reservas = oferta.Reservas.Select(reserva => new Reserva
                           {
                               OfertaId = reserva.OfertaId,
                               Status = reserva.Status,
                               QuantidadeReservada = reserva.QuantidadeReservada
                           }).ToList()
                       };
        var ofertas = await consulta.ToListAsync();

        return ofertas
            .Select(item => ComDisponibilidade(item.Oferta, item.Reservas, agora, item.Estabelecimento))
            .Where(item => item.QuantidadeDisponivel > 0 && (!apenasUrgentes || item.Situacao == SituacaoOferta.Urgente))
            .ToList();
    }

    private static OfertaComDisponibilidade ComDisponibilidade(
        Oferta oferta, IEnumerable<Reserva> reservas, DateTime agora, string? estabelecimento = null)
    {
        decimal saldo = oferta.CalcularQuantidadeDisponivel(reservas);
        return new OfertaComDisponibilidade(oferta, saldo, oferta.ObterSituacao(saldo, agora), estabelecimento);
    }

    private static async Task<Oferta> BuscarOfertaPropriaAsync(AppDbContext context, int estabelecimentoId, int ofertaId)
    {
        Oferta? oferta = await context.BuscarOfertaParaAtualizarAsync(ofertaId);
        if (oferta is null || oferta.EstabelecimentoId != estabelecimentoId)
        {
            throw new InvalidOperationException("Oferta não encontrada entre as suas ofertas.");
        }

        return oferta;
    }

    private static Oferta MontarOferta(
        string alimento, decimal quantidadeTotal, DateTime validade, DateTime inicioRetirada, DateTime fimRetirada) =>
        new()
        {
            Alimento = alimento?.Trim() ?? string.Empty,
            QuantidadeTotal = quantidadeTotal,
            Validade = validade,
            InicioRetirada = inicioRetirada,
            FimRetirada = fimRetirada
        };
}
