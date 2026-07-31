using AuditorFiscal.Domain.Enums;

namespace AuditorFiscal.Application.OrdensServico.Dtos;

public sealed record CriarOrdemServicoDto(
    string Numero,
    string Empresa,
    string Cnpj,
    string Endereco,
    string Cidade,
    string Responsavel,
    TipoFiscalizacao Fiscalizacao,
    DateOnly RecebimentoSfit,
    DateOnly AberturaSfit,
    DateOnly DataFiscalizacao,
    DateOnly PrazoNad,
    DateOnly PrazoNco,
    DateOnly ElaboracaoAutos,
    DateOnly DataFinal,
    string? Observacoes,
    double? Latitude,
    double? Longitude,
    bool TemNcre = false,
    DateOnly? PrazoNcre = null);
