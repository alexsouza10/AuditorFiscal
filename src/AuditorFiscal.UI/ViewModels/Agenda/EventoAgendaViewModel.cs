using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AuditorFiscal.UI.ViewModels.Agenda;

public partial class EventoAgendaViewModel(OrdemServico ordemServico) : ObservableObject
{
    public OrdemServico OrdemServico { get; } = ordemServico;

    public Guid Id => OrdemServico.Id;
    public string Numero => OrdemServico.Numero;
    public string Empresa => OrdemServico.Empresa;
    public string Cidade => OrdemServico.Cidade;
    public string HoraTexto => OrdemServico.Hora.ToString("HH\\:mm");
    public string SituacaoTexto => OrdemServico.Situacao.Descricao();
    public string Cor => OrdemServico.Situacao.Cor();
    public bool Favorito => OrdemServico.Favorito;

    [ObservableProperty]
    private bool _selecionado;
}
