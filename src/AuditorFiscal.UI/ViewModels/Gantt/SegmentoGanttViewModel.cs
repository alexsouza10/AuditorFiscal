namespace AuditorFiscal.UI.ViewModels.Gantt;

/// <summary>Um trecho colorido da barra, delimitado por duas datas do fluxo SFIT.</summary>
public sealed class SegmentoGanttViewModel(string cor, string rotulo, double larguraProporcional)
{
    public string Cor { get; } = cor;
    public string Rotulo { get; } = rotulo;
    public double LarguraProporcional { get; } = larguraProporcional;
}
