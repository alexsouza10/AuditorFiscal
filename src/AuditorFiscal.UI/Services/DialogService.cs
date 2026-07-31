using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AuditorFiscal.UI.Services;

/// <summary>
/// Diálogos construídos em código para evitar mais uma dependência externa em um app
/// que precisa ser leve — o Fluent do Avalonia já fornece a aparência.
/// </summary>
public class DialogService : IDialogService
{
    public Task<bool> ConfirmarAsync(string titulo, string mensagem, string textoConfirmar = "Confirmar") =>
        MostrarAsync(titulo, mensagem, textoConfirmar, "Cancelar");

    public async Task InformarAsync(string titulo, string mensagem) =>
        await MostrarAsync(titulo, mensagem, "OK", null);

    private static async Task<bool> MostrarAsync(string titulo, string mensagem, string textoPrimario, string? textoSecundario)
    {
        // "resultado" só é lido depois que ShowDialog retorna, ou seja, depois que a janela
        // fecha — por isso cada botão precisa fechar a janela, não só anotar a escolha.
        var resultado = false;

        var botaoPrimario = new Button
        {
            Content = textoPrimario,
            MinWidth = 100,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        botaoPrimario.Classes.Add("accent");

        var botoes = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        Window janela = null!;

        if (textoSecundario is not null)
        {
            var botaoSecundario = new Button
            {
                Content = textoSecundario,
                MinWidth = 100,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            botoes.Children.Add(botaoSecundario);
            botaoSecundario.Click += (_, _) =>
            {
                resultado = false;
                janela.Close();
            };
        }

        botoes.Children.Add(botaoPrimario);

        janela = new Window
        {
            Title = titulo,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 16,
                MaxWidth = 460,
                Children =
                {
                    new TextBlock { Text = titulo, FontSize = 16, FontWeight = FontWeight.SemiBold },
                    new TextBlock { Text = mensagem, TextWrapping = TextWrapping.Wrap },
                    botoes
                }
            }
        };

        botaoPrimario.Click += (_, _) =>
        {
            resultado = true;
            janela.Close();
        };

        await janela.ShowDialog(JanelaAtual.Obter());
        return resultado;
    }
}
