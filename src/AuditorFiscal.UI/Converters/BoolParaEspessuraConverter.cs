using System.Globalization;
using Avalonia.Data.Converters;

namespace AuditorFiscal.UI.Converters;

/// <summary>Realce de seleção: true vira uma borda de 2px, false vira nenhuma borda.</summary>
public class BoolParaEspessuraConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? new Avalonia.Thickness(2) : new Avalonia.Thickness(0);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
