using CommunityToolkit.Mvvm.ComponentModel;

namespace AuditorFiscal.UI.ViewModels.Gantt;

/// <summary>Um trecho colorido da barra, delimitado por duas datas do fluxo SFIT.</summary>
public sealed partial class SegmentoGanttViewModel(string cor, string rotulo, double larguraProporcional) : ObservableObject
{
    public string Cor { get; } = cor;
    public string Rotulo { get; } = rotulo;
    public double LarguraProporcional { get; } = larguraProporcional;

    /// <summary>Altura da barra em pixels — recalculada pela linha-mãe quando o cronograma
    /// maximizado precisa comprimir as linhas para caber todas as O.S. sem scroll.</summary>
    [ObservableProperty]
    private double _alturaBarra = 40.0;
}
