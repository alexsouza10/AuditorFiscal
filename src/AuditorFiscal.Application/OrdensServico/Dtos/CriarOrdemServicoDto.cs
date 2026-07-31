namespace AuditorFiscal.Application.OrdensServico.Dtos;

public sealed record CriarOrdemServicoDto(
    string Numero,
    string Empresa,
    string Cnpj,
    string Endereco,
    string Cidade,
    string Responsavel,
    DateOnly Data,
    TimeOnly Hora,
    Guid TipoAuditoriaId,
    string? Observacoes,
    double? Latitude,
    double? Longitude);
