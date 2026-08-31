using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using AuditorFiscal.UI.ViewModels;

namespace AuditorFiscal.UI.Views;

/// <summary>
/// Avisa o formulário quando o auditor digita uma data que não existe no calendário (ex.:
/// 31/02) ou que o CalendarDatePicker não consegue interpretar — por padrão o controle apenas
/// ignora a digitação e volta ao valor anterior, sem nenhum aviso visível.
///
/// Usa a árvore lógica, não a visual: os CalendarDatePickers de uma aba não selecionada só
/// entram na árvore visual quando a aba é exibida (ver DatePickerAutoFormat), mas já existem
/// como objetos na árvore lógica assim que InitializeComponent roda.
/// </summary>
internal static class CalendarDateValidationGuard
{
    public static void AplicarEm(Control raiz)
    {
        foreach (var picker in raiz.GetLogicalDescendants().OfType<CalendarDatePicker>())
        {
            if (picker.Tag is not string rotulo)
                continue;

            picker.DateValidationError += (_, _) =>
            {
                if (picker.DataContext is OrdemServicoFormViewModel formulario)
                    formulario.InformarDataInvalida(rotulo);
            };

            // Volta a data digitada ao "válida" para o Salvar não continuar priorizando um
            // aviso desatualizado depois que o auditor corrige o campo.
            picker.SelectedDateChanged += (_, _) =>
            {
                if (picker.DataContext is OrdemServicoFormViewModel formulario)
                    formulario.LimparDataInvalida(rotulo);
            };
        }
    }
}
