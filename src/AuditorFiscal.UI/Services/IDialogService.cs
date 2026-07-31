namespace AuditorFiscal.UI.Services;

public interface IDialogService
{
    Task<bool> ConfirmarAsync(string titulo, string mensagem, string textoConfirmar = "Confirmar");
    Task InformarAsync(string titulo, string mensagem);
}
