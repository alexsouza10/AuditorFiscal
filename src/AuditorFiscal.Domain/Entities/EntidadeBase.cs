namespace AuditorFiscal.Domain.Entities;

public abstract class EntidadeBase
{
    public Guid Id { get; protected set; }
    public DateTimeOffset CriadoEm { get; protected set; }
    public DateTimeOffset AtualizadoEm { get; protected set; }

    protected EntidadeBase()
    {
        Id = Guid.NewGuid();
    }

    protected EntidadeBase(Guid id)
    {
        Id = id;
    }

    public void MarcarAtualizado(DateTimeOffset momento) => AtualizadoEm = momento;
}
