using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Application.Interfaces.Persistence;

public interface ITipoAuditoriaRepository : IRepository<TipoAuditoria>
{
    Task<IReadOnlyList<TipoAuditoria>> ListarAtivosAsync(CancellationToken ct = default);
}
