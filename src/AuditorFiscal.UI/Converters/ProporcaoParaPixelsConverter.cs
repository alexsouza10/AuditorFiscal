using System.Globalization;
using Avalonia.Data.Converters;

namespace AuditorFiscal.UI.Converters;

/// <summary>
/// Converte uma proporção (0.0–1.0) em pixels, multiplicando pela largura real do
/// contêiner de referência. É o que permite ao Gantt ser responsivo: a mesma proporção
/// vira mais ou menos pixels conforme a janela é redimensionada.
/// </summary>
public class ProporcaoParaPixelsConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not double proporcao || values[1] is not double larguraReferencia)
            return 0.0;

        return Math.Max(0.0, proporcao * larguraReferencia);
    }
}
