using System.Globalization;
using Avalonia.Data.Converters;

namespace AuditorFiscal.UI.Converters;

/// <summary>
/// Compara a largura real de um contêiner com um limite e devolve um bool, usado para
/// alternar a classe de estilo "compacta" e assim empilhar colunas quando a janela é
/// estreitada — a base da responsividade por breakpoint nas telas de formulário.
/// </summary>
public class LarguraAbaixoDoLimiteConverter : IValueConverter
{
    public double Limite { get; set; } = 980;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double largura && largura > 0 && largura < Limite;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
