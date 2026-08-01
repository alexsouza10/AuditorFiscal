using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AuditorFiscal.UI.Views;

/// <summary>
/// Formata a digitação em CalendarDatePicker como dd/MM/aaaa automaticamente, inserindo as
/// barras conforme o auditor digita os números — sem isso, o campo mostra os dígitos crus
/// (ex.: "02022026") e não reconhece a data.
///
/// Intercepta o TextInput em modo Tunnel a partir da raiz da view: como o TabControl só
/// materializa o TextBox interno do CalendarDatePicker quando a aba é exibida, procurar os
/// controles uma única vez no construtor (via GetVisualDescendants) não os encontrava a
/// tempo. O tunneling resolve isso porque é resolvido em tempo real, na hora do evento.
/// </summary>
internal static class DatePickerAutoFormat
{
    public static void AplicarEm(InputElement raiz) =>
        raiz.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);

    private static void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text) || !e.Text.All(char.IsDigit))
            return;

        if (e.Source is not TextBox textBox || textBox.FindAncestorOfType<CalendarDatePicker>() is null)
            return;

        e.Handled = true;

        var textoAtual = textBox.Text ?? string.Empty;
        var inicioSelecao = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
        var fimSelecao = Math.Max(textBox.SelectionStart, textBox.SelectionEnd);
        var textoComEntrada = textoAtual.Remove(inicioSelecao, fimSelecao - inicioSelecao).Insert(inicioSelecao, e.Text);

        var digitos = new string(textoComEntrada.Where(char.IsDigit).ToArray());
        if (digitos.Length > 8)
            digitos = digitos[..8];

        var formatado = digitos.Length switch
        {
            <= 2 => digitos,
            <= 4 => $"{digitos[..2]}/{digitos[2..]}",
            _ => $"{digitos[..2]}/{digitos[2..4]}/{digitos[4..]}"
        };

        textBox.Text = formatado;
        textBox.CaretIndex = formatado.Length;
    }
}
