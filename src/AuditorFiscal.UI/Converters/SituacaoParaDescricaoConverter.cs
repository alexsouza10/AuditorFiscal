using System.Globalization;
using AuditorFiscal.Domain.Enums;
using Avalonia.Data.Converters;

namespace AuditorFiscal.UI.Converters;

/// <summary>Mostra "Em andamento" em vez do nome cru do enum ("EmAndamento") na tela.</summary>
public class SituacaoParaDescricaoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is SituacaoOS situacao ? situacao.Descricao() : "Todas";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
