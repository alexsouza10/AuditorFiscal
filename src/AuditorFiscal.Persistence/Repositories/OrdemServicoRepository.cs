using AuditorFiscal.Application.Interfaces.Persistence;
using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AuditorFiscal.Persistence.Repositories;

public class OrdemServicoRepository(AuditorFiscalDbContext contexto)
    : Repository<OrdemServico>(contexto), IOrdemServicoRepository
{
    private IQueryable<OrdemServico> ComDetalhes() =>
        DbSet
            .Include(x => x.Fotos)
            .Include(x => x.Anexos)
            .Include(x => x.Timeline)
            .Include(x => x.Tags);

    public override async Task<OrdemServico?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        await ComDetalhes().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<OrdemServico?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default) =>
        ObterPorIdAsync(id, ct);

    public async Task<IReadOnlyList<OrdemServico>> BuscarAsync(FiltroOrdemServicoDto filtro, CancellationToken ct = default)
    {
        var consulta = DbSet.Include(x => x.Tags).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Termo))
        {
            var termo = $"%{filtro.Termo.Trim()}%";
            consulta = consulta.Where(x =>
                EF.Functions.Like(x.Numero, termo) ||
                EF.Functions.Like(x.Empresa, termo) ||
                EF.Functions.Like(x.Responsavel, termo) ||
                EF.Functions.Like(x.Cidade, termo) ||
                EF.Functions.Like(x.Endereco, termo));
        }

        if (filtro.Situacao.HasValue)
            consulta = consulta.Where(x => x.Situacao == filtro.Situacao.Value);

        if (filtro.TagId.HasValue)
            consulta = consulta.Where(x => x.Tags.Any(t => t.Id == filtro.TagId.Value));

        if (filtro.SomenteFavoritos)
            consulta = consulta.Where(x => x.Favorito);

        if (filtro.DataInicio.HasValue)
            consulta = consulta.Where(x => x.DataFinal >= filtro.DataInicio.Value);

        if (filtro.DataFim.HasValue)
            consulta = consulta.Where(x => x.RecebimentoSfit <= filtro.DataFim.Value);

        return await consulta
            .OrderByDescending(x => x.RecebimentoSfit)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Usado pelo Cronograma GANTT: qualquer O.S. cujo intervalo [RecebimentoSfit, DataFinal]
    /// cruze com o período visível na tela deve aparecer, mesmo que tenha começado antes.
    /// </summary>
    public async Task<IReadOnlyList<OrdemServico>> ObterPorPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken ct = default) =>
        await DbSet
            .Where(x => x.RecebimentoSfit <= fim && x.DataFinal >= inicio)
            .OrderBy(x => x.RecebimentoSfit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<OrdemServico>> ObterPorEmpresaAsync(string empresa, CancellationToken ct = default) =>
        await DbSet
            .Where(x => x.Empresa == empresa)
            .OrderByDescending(x => x.RecebimentoSfit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> ListarEmpresasAsync(CancellationToken ct = default) =>
        await DbSet.Select(x => x.Empresa).Distinct().OrderBy(x => x).ToListAsync(ct);

    public async Task<bool> NumeroJaExisteAsync(string numero, Guid? ignorarId = null, CancellationToken ct = default) =>
        await DbSet.AnyAsync(x => x.Numero == numero && (ignorarId == null || x.Id != ignorarId), ct);

    /// <summary>
    /// Sugere o próximo número no padrão 00000000-9 (8 dígitos sequenciais + 1 dígito
    /// verificador), continuando a partir do maior sequencial já usado para o auditor
    /// não precisar controlar isso manualmente.
    /// </summary>
    public async Task<string> SugerirProximoNumeroAsync(CancellationToken ct = default)
    {
        var numeros = await DbSet.Select(x => x.Numero).ToListAsync(ct);

        var maiorSequencial = numeros
            .Select(NumeroOsFormatter.ExtrairSequencial)
            .DefaultIfEmpty(0)
            .Max();

        return NumeroOsFormatter.Formatar(maiorSequencial + 1);
    }
}
