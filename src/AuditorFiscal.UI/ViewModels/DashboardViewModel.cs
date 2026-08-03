using System.Collections.ObjectModel;
using System.Globalization;
using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.UI.Messaging;
using AuditorFiscal.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AuditorFiscal.UI.ViewModels;

/// <summary>
/// Painel de eficiência: mostra o panorama geral das O.S. (total, por situação, favoritas)
/// e quantas foram concluídas em cada janela de tempo recente, para o auditor acompanhar
/// o ritmo de conclusão sem precisar aplicar filtros manualmente no Banco de Dados.
/// </summary>
public partial class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly OrdemServicoService _ordemServicoService;
    private readonly INavigationService _navigation;

    [ObservableProperty] private int _total;
    [ObservableProperty] private int _totalAgendadas;
    [ObservableProperty] private int _totalEmAndamento;
    [ObservableProperty] private int _totalConcluidas;
    [ObservableProperty] private int _totalFavoritas;
    [ObservableProperty] private int _concluidasSemana;
    [ObservableProperty] private int _concluidasMes;
    [ObservableProperty] private int _concluidasAno;
    [ObservableProperty] private double _taxaConclusao;

    public ObservableCollection<BarraDashboardViewModel> DistribuicaoSituacao { get; } = [];
    public ObservableCollection<BarraDashboardViewModel> DistribuicaoTipo { get; } = [];
    public ObservableCollection<FatiaPizzaViewModel> PizzaSituacao { get; } = [];
    public ObservableCollection<BarraProducaoMensalViewModel> ProducaoMensal { get; } = [];

    public bool SemDados => Total == 0;

    partial void OnTotalChanged(int value) => OnPropertyChanged(nameof(SemDados));

    public DashboardViewModel(OrdemServicoService ordemServicoService, INavigationService navigation)
    {
        _ordemServicoService = ordemServicoService;
        _navigation = navigation;

        WeakReferenceMessenger.Default.Register<OrdemServicoAlteradaMessage>(this, async (_, _) => await CarregarAsync());

        _ = CarregarAsync();
    }

    [RelayCommand]
    private void Voltar() => _navigation.IrParaInicio();

    [RelayCommand]
    private void NovaOrdemServico() => _navigation.NavegarPara<OrdemServicoFormViewModel>();

    private async Task CarregarAsync()
    {
        var todas = await _ordemServicoService.BuscarAsync(new FiltroOrdemServicoDto());

        Total = todas.Count;
        TotalAgendadas = todas.Count(o => o.Situacao == SituacaoOS.Agendada);
        TotalEmAndamento = todas.Count(o => o.Situacao == SituacaoOS.EmAndamento);
        TotalConcluidas = todas.Count(o => o.Situacao == SituacaoOS.Concluida);
        TotalFavoritas = todas.Count(o => o.Favorito);
        TaxaConclusao = Total == 0 ? 0 : Math.Round(TotalConcluidas * 100.0 / Total, 1);

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var inicioSemana = hoje.AddDays(-(int)hoje.DayOfWeek);
        var inicioMes = new DateOnly(hoje.Year, hoje.Month, 1);
        var inicioAno = new DateOnly(hoje.Year, 1, 1);

        var concluidas = todas.Where(o => o.Situacao == SituacaoOS.Concluida).ToList();
        ConcluidasSemana = concluidas.Count(o => o.DataFinal >= inicioSemana);
        ConcluidasMes = concluidas.Count(o => o.DataFinal >= inicioMes);
        ConcluidasAno = concluidas.Count(o => o.DataFinal >= inicioAno);

        AtualizarDistribuicoes(todas);
        AtualizarPizzaSituacao(todas);
        AtualizarProducaoMensal(todas);
    }

    private void AtualizarDistribuicoes(IReadOnlyList<OrdemServico> todas)
    {
        var maximoSituacao = Math.Max(1, todas
            .GroupBy(o => o.Situacao)
            .Select(g => g.Count())
            .DefaultIfEmpty(0)
            .Max());

        DistribuicaoSituacao.Clear();
        foreach (var situacao in Enum.GetValues<SituacaoOS>())
        {
            var quantidade = todas.Count(o => o.Situacao == situacao);
            if (quantidade == 0)
                continue;

            DistribuicaoSituacao.Add(new BarraDashboardViewModel(
                situacao.Descricao(), quantidade, quantidade / (double)maximoSituacao, situacao.Cor()));
        }

        var porTipo = todas
            .GroupBy(o => o.Fiscalizacao.Descricao())
            .Select(g => (Nome: g.Key, Quantidade: g.Count()))
            .OrderByDescending(x => x.Quantidade)
            .ToList();

        var maximoTipo = Math.Max(1, porTipo.Select(x => x.Quantidade).DefaultIfEmpty(0).Max());

        DistribuicaoTipo.Clear();
        foreach (var (nome, quantidade) in porTipo)
            DistribuicaoTipo.Add(new BarraDashboardViewModel(
                nome, quantidade, quantidade / (double)maximoTipo, "#6366F1"));
    }

    /// <summary>
    /// Gráfico de rosca (fatias como geometria SVG) para a distribuição por situação. O
    /// miolo vazado sobra livre na view para mostrar o total — assim o número mais
    /// importante do gráfico fica dentro dele, em vez de disputar espaço com a legenda.
    /// </summary>
    private void AtualizarPizzaSituacao(IReadOnlyList<OrdemServico> todas)
    {
        PizzaSituacao.Clear();

        if (todas.Count == 0)
            return;

        const double cx = 90, cy = 90, raioExterno = 88, raioInterno = 52;
        var anguloAcumulado = 0.0;

        foreach (var situacao in Enum.GetValues<SituacaoOS>())
        {
            var quantidade = todas.Count(o => o.Situacao == situacao);
            if (quantidade == 0)
                continue;

            var fracao = quantidade / (double)todas.Count;
            var anguloInicial = anguloAcumulado;
            var anguloFinal = Math.Min(anguloAcumulado + fracao * 360, anguloAcumulado + 359.999);
            anguloAcumulado += fracao * 360;

            var geometria = MontarFatiaDonut(cx, cy, raioExterno, raioInterno, anguloInicial, anguloFinal);
            var percentual = Math.Round(fracao * 100, 1);

            PizzaSituacao.Add(new FatiaPizzaViewModel(
                situacao.Descricao(), quantidade, percentual, situacao.Cor(), geometria));
        }
    }

    private static string MontarFatiaDonut(
        double cx, double cy, double raioExterno, double raioInterno, double anguloInicialGraus, double anguloFinalGraus)
    {
        var (xExt0, yExt0) = PontoNoCirculo(cx, cy, raioExterno, anguloInicialGraus);
        var (xExt1, yExt1) = PontoNoCirculo(cx, cy, raioExterno, anguloFinalGraus);
        var (xInt1, yInt1) = PontoNoCirculo(cx, cy, raioInterno, anguloFinalGraus);
        var (xInt0, yInt0) = PontoNoCirculo(cx, cy, raioInterno, anguloInicialGraus);
        var arcoGrande = anguloFinalGraus - anguloInicialGraus > 180 ? 1 : 0;

        return string.Create(CultureInfo.InvariantCulture,
            $"M {xExt0:F2},{yExt0:F2} A {raioExterno},{raioExterno} 0 {arcoGrande} 1 {xExt1:F2},{yExt1:F2} " +
            $"L {xInt1:F2},{yInt1:F2} A {raioInterno},{raioInterno} 0 {arcoGrande} 0 {xInt0:F2},{yInt0:F2} Z");
    }

    private static (double X, double Y) PontoNoCirculo(double cx, double cy, double raio, double anguloGraus)
    {
        var anguloRad = Math.PI * anguloGraus / 180.0;
        return (cx + raio * Math.Sin(anguloRad), cy - raio * Math.Cos(anguloRad));
    }

    /// <summary>
    /// Gráfico de produção mensal: para cada um dos últimos 6 meses, compara quantas O.S.
    /// entraram (recebidas no SFIT) com quantas saíram (concluídas) — o indicador mais direto
    /// de ritmo de trabalho para um sistema de fiscalização, já que mostra se o auditor está
    /// dando vazão à demanda que chega ou se o volume em aberto está crescendo.
    /// </summary>
    private void AtualizarProducaoMensal(IReadOnlyList<OrdemServico> todas)
    {
        const int meses = 6;
        const double alturaMaximaPixels = 120;
        var culturaPtBr = new CultureInfo("pt-BR");
        var hoje = DateTime.Today;

        var contagens = new List<(string Rotulo, int Abertas, int Concluidas)>();
        for (var i = meses - 1; i >= 0; i--)
        {
            var referencia = hoje.AddMonths(-i);
            var inicioMes = new DateOnly(referencia.Year, referencia.Month, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);

            var abertas = todas.Count(o => o.RecebimentoSfit >= inicioMes && o.RecebimentoSfit <= fimMes);
            var concluidas = todas.Count(o =>
                o.Situacao == SituacaoOS.Concluida && o.DataFinal >= inicioMes && o.DataFinal <= fimMes);

            var rotulo = referencia.ToString("MMM", culturaPtBr).TrimEnd('.').ToUpperInvariant();
            contagens.Add((rotulo, abertas, concluidas));
        }

        var maximo = Math.Max(1, contagens
            .SelectMany(c => new[] { c.Abertas, c.Concluidas })
            .DefaultIfEmpty(0)
            .Max());

        ProducaoMensal.Clear();
        foreach (var (rotulo, abertas, concluidas) in contagens)
            ProducaoMensal.Add(new BarraProducaoMensalViewModel(
                rotulo, abertas, concluidas,
                abertas == 0 ? 0 : Math.Max(4, abertas / (double)maximo * alturaMaximaPixels),
                concluidas == 0 ? 0 : Math.Max(4, concluidas / (double)maximo * alturaMaximaPixels)));
    }

    public void Dispose() => WeakReferenceMessenger.Default.UnregisterAll(this);
}

public sealed class FatiaPizzaViewModel(string rotulo, int quantidade, double percentual, string cor, string geometria)
{
    public string Rotulo { get; } = rotulo;
    public int Quantidade { get; } = quantidade;
    public double Percentual { get; } = percentual;
    public string Cor { get; } = cor;
    public string Geometria { get; } = geometria;
}

public sealed class BarraProducaoMensalViewModel(
    string rotulo, int abertas, int concluidas, double alturaAbertasPixels, double alturaConcluidasPixels)
{
    public string Rotulo { get; } = rotulo;
    public int Abertas { get; } = abertas;
    public int Concluidas { get; } = concluidas;
    public double AlturaAbertasPixels { get; } = alturaAbertasPixels;
    public double AlturaConcluidasPixels { get; } = alturaConcluidasPixels;
}
