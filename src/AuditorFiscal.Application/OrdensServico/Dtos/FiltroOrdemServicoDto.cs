using AuditorFiscal.Domain.Enums;

namespace AuditorFiscal.Application.OrdensServico.Dtos;

public sealed record FiltroOrdemServicoDto
{
    /// <summary>Texto livre remanescente após a extração dos tokens estruturados da pesquisa.</summary>
    public string? Termo { get; init; }
    public SituacaoOS? Situacao { get; init; }

    /// <summary>Filtro multi-seleção de situação (ex.: tela de Banco de Dados); quando
    /// presente, é aplicado no lugar de <see cref="Situacao"/>.</summary>
    public IReadOnlyList<SituacaoOS>? Situacoes { get; init; }

    public TipoFiscalizacao? Fiscalizacao { get; init; }
    public Guid? TagId { get; init; }
    public bool SomenteFavoritos { get; init; }
    public DateOnly? DataInicio { get; init; }
    public DateOnly? DataFim { get; init; }

    // Filtros derivados de datas calculadas em tempo de leitura (Domain.OrdemServico), sem
    // tradução para SQL — aplicados em memória depois da consulta ao banco.
    public bool SomenteAtrasadas { get; init; }
    public bool SomenteSemMovimentacao { get; init; }
    public bool SomenteVencemHoje { get; init; }
    public int? PrazoMaximoDias { get; init; }
    public int? PrazoMinimoDias { get; init; }

    public string? EmpresaContem { get; init; }
    public string? CidadeContem { get; init; }
    public string? ResponsavelContem { get; init; }
    public string? TagNome { get; init; }
}
