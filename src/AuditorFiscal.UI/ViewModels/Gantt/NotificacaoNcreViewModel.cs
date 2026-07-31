namespace AuditorFiscal.UI.ViewModels.Gantt;

/// <summary>
/// Alerta exibido abaixo do Gantt para uma O.S. sem NCRE cadastrado, ou cujo prazo de
/// NCRE já cadastrado está próximo/vencido.
/// </summary>
public sealed class NotificacaoNcreViewModel(Guid ordemServicoId, string texto)
{
    public Guid OrdemServicoId { get; } = ordemServicoId;
    public string Texto { get; } = texto;
}
