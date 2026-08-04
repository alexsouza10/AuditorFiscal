using System.Globalization;
using AuditorFiscal.Domain.Enums;
using Avalonia.Data.Converters;

namespace AuditorFiscal.UI.Converters;

/// <summary>Mostra a descrição do tipo de fiscalização, ou "Todas" para a opção nula do filtro.</summary>
public class FiscalizacaoParaDescricaoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TipoFiscalizacao fiscalizacao ? fiscalizacao.Descricao() : "Todas";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
