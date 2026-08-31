using Avalonia.Controls;

namespace AuditorFiscal.UI.Views;

public partial class OrdemServicoFormView : UserControl
{
    public OrdemServicoFormView()
    {
        InitializeComponent();
        CalendarWheelGuard.AplicarEm(this);
        DatePickerAutoFormat.AplicarEm(this);
        CalendarDateValidationGuard.AplicarEm(this);
    }
}
