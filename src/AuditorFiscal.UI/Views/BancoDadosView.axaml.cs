using Avalonia.Controls;

namespace AuditorFiscal.UI.Views;

public partial class BancoDadosView : UserControl
{
    public BancoDadosView()
    {
        InitializeComponent();
        CalendarWheelGuard.AplicarEm(this);
        DatePickerAutoFormat.AplicarEm(this);
    }
}
