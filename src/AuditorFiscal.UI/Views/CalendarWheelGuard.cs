using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AuditorFiscal.UI.Views;

/// <summary>
/// O CalendarDatePicker do FluentTheme troca o dia/mês/ano sob o cursor ao rolar o mouse —
/// fácil de disparar sem querer. Bloqueia o wheel antes que chegue ao controle, deixando a
/// data ser alterada só digitando ou pelo ícone de calendário.
/// </summary>
internal static class CalendarWheelGuard
{
    public static void AplicarEm(InputElement raiz) =>
        raiz.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);

    private static void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.Source is Visual visual && visual.FindAncestorOfType<CalendarDatePicker>() is not null)
            e.Handled = true;
    }
}
