using System.Globalization;

namespace AuditorFiscal.UI.ViewModels.Gantt;

public sealed class MesGanttViewModel(DateOnly inicioMes, DateOnly fimMes, DateOnly inicioJanela, DateOnly fimJanela)
{
    private static readonly CultureInfo CulturaPtBr = new("pt-BR");

    public string Rotulo { get; } = inicioMes.ToDateTime(TimeOnly.MinValue)
        .ToString("MMM", CulturaPtBr).TrimEnd('.').ToUpperInvariant();

    public string Ano { get; } = inicioMes.Year.ToString(CulturaPtBr);

    public double LarguraProporcional { get; } =
        (fimMes.DayNumber - inicioMes.DayNumber + 1) / (double)Math.Max(1, fimJanela.DayNumber - inicioJanela.DayNumber);
}
