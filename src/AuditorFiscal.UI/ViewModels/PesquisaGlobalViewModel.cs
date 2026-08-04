using System.Collections.ObjectModel;
using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Application.OrdensServico.Busca;
using AuditorFiscal.Domain.Entities;
using AuditorFiscal.UI.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuditorFiscal.UI.ViewModels;

/// <summary>
/// Pesquisa Global (Ctrl+P): aceita a mesma query DSL do Banco de Dados
/// (<see cref="ConsultaOrdemServicoParser"/>), mas em uma janela leve que fica sempre a um
/// atalho de distância, sem precisar navegar até o Banco de Dados primeiro.
/// </summary>
public partial class PesquisaGlobalViewModel : ViewModelBase, IDisposable
{
    private readonly OrdemServicoService _ordemServicoService;
    private readonly INavigationService _navigation;
    private readonly DispatcherTimer _debounce;

    [ObservableProperty] private string? _consulta;
    [ObservableProperty] private OrdemServico? _selecionado;
    [ObservableProperty] private bool _semResultados;

    public ObservableCollection<OrdemServico> Resultados { get; } = [];

    /// <summary>A View escuta este evento para se fechar após abrir uma O.S. ou cancelar.</summary>
    public event Action? RequisitarFechamento;

    public PesquisaGlobalViewModel(OrdemServicoService ordemServicoService, INavigationService navigation)
    {
        _ordemServicoService = ordemServicoService;
        _navigation = navigation;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _debounce.Tick += async (_, _) =>
        {
            _debounce.Stop();
            await BuscarAsync();
        };
    }

    partial void OnConsultaChanged(string? value)
    {
        _debounce.Stop();
        _debounce.Start();
    }

    [RelayCommand]
    private async Task AbrirSelecionadoAsync()
    {
        if (Selecionado is null)
            return;

        var formulario = _navigation.Resolver<OrdemServicoFormViewModel>();
        await formulario.CarregarParaEdicaoAsync(Selecionado.Id);
        _navigation.NavegarPara(formulario);
        RequisitarFechamento?.Invoke();
    }

    [RelayCommand]
    private void Fechar() => RequisitarFechamento?.Invoke();

    [RelayCommand]
    private void MoverSelecao(string direcao)
    {
        if (Resultados.Count == 0)
            return;

        var indiceAtual = Selecionado is null ? -1 : Resultados.IndexOf(Selecionado);
        var novoIndice = direcao == "baixo"
            ? Math.Min(indiceAtual + 1, Resultados.Count - 1)
            : Math.Max(indiceAtual - 1, 0);

        Selecionado = Resultados[novoIndice];
    }

    private async Task BuscarAsync()
    {
        Resultados.Clear();

        if (string.IsNullOrWhiteSpace(Consulta))
        {
            SemResultados = false;
            return;
        }

        var filtro = ConsultaOrdemServicoParser.Interpretar(Consulta);
        var resultados = await _ordemServicoService.BuscarAsync(filtro);

        foreach (var ordem in resultados.Take(20))
            Resultados.Add(ordem);

        Selecionado = Resultados.FirstOrDefault();
        SemResultados = Resultados.Count == 0;
    }

    public void Dispose() => _debounce.Stop();
}
