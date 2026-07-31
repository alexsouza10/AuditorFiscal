using AuditorFiscal.Shared;

namespace AuditorFiscal.Domain.Entities;

public class Tag : EntidadeBase
{
    public string Nome { get; private set; } = string.Empty;
    public string Cor { get; private set; } = "#607D8B";

    private readonly List<OrdemServico> _ordensServico = [];
    public IReadOnlyCollection<OrdemServico> OrdensServico => _ordensServico.AsReadOnly();

    private Tag()
    {
    }

    public Tag(string nome, string cor)
    {
        Nome = Guard.NotNullOrWhiteSpace(nome, nameof(nome));
        Cor = Guard.NotNullOrWhiteSpace(cor, nameof(cor));
    }
}
