using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AuditorFiscal.UI.ViewModels.Agenda;

/// <summary>Interseção de um dia com uma faixa de uma hora na grade da semana.</summary>
public partial class CelulaAgendaViewModel(DateOnly data, TimeOnly hora) : ObservableObject
{
    public DateOnly Data { get; } = data;
    public TimeOnly Hora { get; } = hora;

    public ObservableCollection<EventoAgendaViewModel> Eventos { get; } = [];

    public bool EhFimDeSemana => Data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public bool EhHoje => Data == DateOnly.FromDateTime(DateTime.Today);
}

public sealed class LinhaHorarioViewModel(TimeOnly hora, IReadOnlyList<CelulaAgendaViewModel> celulas)
{
    public TimeOnly Hora { get; } = hora;
    public string HoraTexto { get; } = hora.ToString("HH\\:mm");
    public IReadOnlyList<CelulaAgendaViewModel> Celulas { get; } = celulas;
}

public sealed class CabecalhoDiaViewModel(DateOnly data)
{
    public DateOnly Data { get; } = data;
    public string DiaSemana { get; } = data.ToDateTime(TimeOnly.MinValue)
        .ToString("ddd", new global::System.Globalization.CultureInfo("pt-BR"))
        .TrimEnd('.')
        .ToUpperInvariant();
    public string DiaNumero { get; } = data.Day.ToString("00");
    public bool EhHoje { get; } = data == DateOnly.FromDateTime(DateTime.Today);
    public bool EhFimDeSemana { get; } = data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
