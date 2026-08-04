using System.Collections.ObjectModel;
using System.ComponentModel;
using AuditorFiscal.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AuditorFiscal.UI.ViewModels;

/// <summary>
/// Estado de um filtro de situação com múltipla seleção (checkbox), reaproveitado pelo Banco
/// de Dados e pelo Cronograma GANTT — mantém sozinho o invariante entre a opção "Todas" e os
/// itens marcados individualmente, para as duas telas não duplicarem essa lógica.
/// </summary>
public sealed partial class SituacaoMultiSelectViewModel : ObservableObject
{
    [ObservableProperty] private bool _todasSituacoes = true;

    public ObservableCollection<SituacaoFiltroItem> Itens { get; } = new(
        Enum.GetValues<SituacaoOS>().Select(s => new SituacaoFiltroItem(s)));

    /// <summary>Disparado sempre que a seleção muda, para a tela reagir (nova busca).</summary>
    public event EventHandler? SelecaoAlterada;

    public string Resumo
    {
        get
        {
            var selecionadas = Selecionadas;
            if (TodasSituacoes || selecionadas.Count == 0)
                return "Todas";

            return selecionadas.Count == 1
                ? selecionadas[0].Descricao()
                : $"{selecionadas.Count} selecionadas";
        }
    }

    public List<SituacaoOS> Selecionadas =>
        Itens.Where(i => i.Selecionada).Select(i => i.Situacao).ToList();

    public SituacaoMultiSelectViewModel()
    {
        foreach (var item in Itens)
            item.PropertyChanged += OnItemChanged;
    }

    /// <summary>Marcar "Todas" desmarca as situações específicas; desmarcar "Todas" sem
    /// nenhuma situação específica marcada não faria sentido (ficaria sem nenhuma opção
    /// visivelmente ativa), então revertemos para o estado consistente.</summary>
    partial void OnTodasSituacoesChanged(bool value)
    {
        if (!value && Itens.All(i => !i.Selecionada))
        {
            TodasSituacoes = true;
            return;
        }

        if (value)
            foreach (var item in Itens)
                item.Selecionada = false;

        Notificar();
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SituacaoFiltroItem.Selecionada) || sender is not SituacaoFiltroItem item)
            return;

        if (item.Selecionada)
            TodasSituacoes = false;
        else if (Itens.All(i => !i.Selecionada))
            TodasSituacoes = true;

        Notificar();
    }

    private void Notificar()
    {
        OnPropertyChanged(nameof(Resumo));
        SelecaoAlterada?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Uma opção marcável do filtro de situação, permitindo combinar mais de uma situação
/// na mesma busca — ex.: ver "Agendada" e "Em andamento" ao mesmo tempo.</summary>
public sealed partial class SituacaoFiltroItem(SituacaoOS situacao) : ObservableObject
{
    public SituacaoOS Situacao { get; } = situacao;
    public string Descricao => Situacao.Descricao();

    [ObservableProperty] private bool _selecionada;
}
