using Avalonia.Controls;
using AuditorFiscal.UI.ViewModels;

namespace AuditorFiscal.UI.Views;

public partial class GanttView : UserControl
{
    public GanttView()
    {
        InitializeComponent();
        CalendarWheelGuard.AplicarEm(this);
        DatePickerAutoFormat.AplicarEm(this);

        // Informa o ViewModel da altura real do viewport de linhas a cada redimensionamento —
        // é o único jeito confiável de saber quanto espaço existe para decidir o quanto as
        // linhas precisam encolher no modo maximizado (ver GanttViewModel.RecalcularAlturasLinha).
        var scrollLinhas = this.FindControl<ScrollViewer>("ScrollLinhas");
        if (scrollLinhas is not null)
        {
            scrollLinhas.SizeChanged += (_, e) =>
            {
                if (DataContext is GanttViewModel viewModel)
                    viewModel.AlturaDisponivel = e.NewSize.Height;
            };
        }
    }
}
