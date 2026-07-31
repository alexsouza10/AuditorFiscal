using AuditorFiscal.Shared;

namespace AuditorFiscal.Domain.Entities;

public class TipoAuditoria : EntidadeBase
{
    public string Nome { get; private set; } = string.Empty;
    public bool Ativo { get; private set; } = true;

    private TipoAuditoria()
    {
    }

    public TipoAuditoria(Guid id, string nome) : base(id)
    {
        Nome = Guard.NotNullOrWhiteSpace(nome, nameof(nome));
    }

    public void Renomear(string nome) => Nome = Guard.NotNullOrWhiteSpace(nome, nameof(nome));

    public void Desativar() => Ativo = false;

    public void Ativar() => Ativo = true;
}
