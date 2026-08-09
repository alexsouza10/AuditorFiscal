using System.Globalization;
using AuditorFiscal.Domain.Enums;
using Avalonia.Data.Converters;

namespace AuditorFiscal.UI.Converters;

/// <summary>Mostra a descrição do papel do auditor, ou "Todos" para a opção nula do filtro.</summary>
public class PapelAuditorParaDescricaoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is PapelAuditor papel ? papel.Descricao() : "Todos";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
