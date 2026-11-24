using System.Data;
using MesaViva.Data;
using MesaViva.Models;
using Microsoft.EntityFrameworkCore;

namespace MesaViva.Services;

public sealed record OfertaComDisponibilidade(Oferta Oferta, decimal QuantidadeDisponivel);

public class OfertaService
{
    public async Task<Oferta> CadastrarOfertaAsync(
        Usuario estabelecimento, string alimento, decimal quantidadeTotal,
        DateTime validade, DateTime inicioRetirada, DateTime fimRetirada)
    {
        estabelecimento.ExigirTipo(Usuario.TipoEstabelecimento);
        Oferta oferta = ValidarDados(alimento, quantidadeTotal, validade, inicioRetirada, fimRetirada);
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
        var ofertas = await CarregarOfertasAsync(context.Ofertas
            .Where(oferta => oferta.EstabelecimentoId == estabelecimento.Id));

        return ofertas.Select(ComDisponibilidade).ToList();
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
        Oferta novosDados = ValidarDados(alimento, quantidadeTotal, validade, inicioRetirada, fimRetirada);

        await using var context = new AppDbContext();
        await using var transacao = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        Oferta oferta = await BuscarOfertaPropriaAsync(context, estabelecimento.Id, ofertaId);
        await context.Entry(oferta).Collection(item => item.Reservas).LoadAsync();

        if (novosDados.QuantidadeTotal < oferta.CalcularQuantidadeComprometida(oferta.Reservas))
        {
            throw new InvalidOperationException("A quantidade total não pode ser menor que a quantidade já comprometida.");
        }

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
        var ofertas = await CarregarOfertasAsync(context.Ofertas
            .Where(oferta => oferta.Validade >= agora && oferta.FimRetirada > agora));

        return ofertas
            .Where(oferta => !apenasUrgentes || oferta.EstaUrgente(oferta.Reservas, agora))
            .Select(ComDisponibilidade)
            .Where(item => item.QuantidadeDisponivel > 0)
            .ToList();
    }

    private static Task<List<Oferta>> CarregarOfertasAsync(IQueryable<Oferta> consulta)
    {
        return consulta.AsNoTracking()
            .Include(oferta => oferta.Reservas)
            .OrderBy(oferta => oferta.FimRetirada)
            .ThenBy(oferta => oferta.Id)
            .ToListAsync();
    }

    private static OfertaComDisponibilidade ComDisponibilidade(Oferta oferta)
    {
        return new OfertaComDisponibilidade(oferta, oferta.CalcularQuantidadeDisponivel(oferta.Reservas));
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

    private static Oferta ValidarDados(
        string alimento, decimal quantidadeTotal, DateTime validade, DateTime inicioRetirada, DateTime fimRetirada)
    {
        var oferta = new Oferta
        {
            Alimento = alimento?.Trim() ?? string.Empty,
            QuantidadeTotal = quantidadeTotal,
            Validade = validade,
            InicioRetirada = inicioRetirada,
            FimRetirada = fimRetirada
        };
        oferta.Validar();
        return oferta;
    }
}
