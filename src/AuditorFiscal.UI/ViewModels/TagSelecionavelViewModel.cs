using CommunityToolkit.Mvvm.ComponentModel;

namespace AuditorFiscal.UI.ViewModels;

public partial class TagSelecionavelViewModel(Guid id, string nome, string cor, bool selecionada) : ObservableObject
{
    public Guid Id { get; } = id;
    public string Nome { get; } = nome;
    public string Cor { get; } = cor;

    [ObservableProperty]
    private bool _selecionada = selecionada;
}
