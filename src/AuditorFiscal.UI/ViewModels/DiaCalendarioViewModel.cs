using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AuditorFiscal.UI.ViewModels;

public partial class DiaCalendarioViewModel(DateOnly data, bool estaNoMesAtual, IReadOnlyList<OrdemServico> ordensServico)
    : ObservableObject
{
    private const int MaximoBarrasVisiveis = 4;

    public DateOnly Data { get; } = data;
    public bool EstaNoMesAtual { get; } = estaNoMesAtual;
    public IReadOnlyList<OrdemServico> OrdensServico { get; } = ordensServico;

    [ObservableProperty]
    private bool _selecionado;

    public int Dia => Data.Day;
    public bool EhHoje => Data == DateOnly.FromDateTime(DateTime.Today);
    public bool TemMais => OrdensServico.Count > MaximoBarrasVisiveis;
    public int QuantidadeExtra => Math.Max(0, OrdensServico.Count - MaximoBarrasVisiveis);

    public IReadOnlyList<string> CoresBarras => OrdensServico
        .Take(MaximoBarrasVisiveis)
        .Select(os => CorPorSituacao(os.Situacao))
        .ToList();

    private static string CorPorSituacao(SituacaoOS situacao) => situacao switch
    {
        SituacaoOS.Agendada => "#2196F3",
        SituacaoOS.EmAndamento => "#FF9800",
        SituacaoOS.Concluida => "#4CAF50",
        SituacaoOS.Adiada => "#9E9E9E",
        SituacaoOS.Cancelada => "#F44336",
        _ => "#9E9E9E"
    };
}
