using AuditorFiscal.Domain.Enums;

namespace AuditorFiscal.Application.OrdensServico.Dtos;

public sealed record OrdemServicoResumoDto(
    Guid Id,
    string Numero,
    string Empresa,
    DateOnly Data,
    TimeOnly Hora,
    SituacaoOS Situacao,
    bool Favorito);
