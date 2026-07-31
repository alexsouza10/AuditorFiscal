using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AuditorFiscal.UI.Converters;

/// <summary>Mesma ideia de ProporcaoParaPixelsConverter, mas produz uma margem esquerda.</summary>
public class ProporcaoParaMargemConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not double proporcao || values[1] is not double larguraReferencia)
            return new Thickness(0);

        return new Thickness(Math.Max(0.0, proporcao * larguraReferencia), 0, 0, 0);
    }
}
