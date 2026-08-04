using AuditorFiscal.UI.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace AuditorFiscal.UI.Views;

public partial class PesquisaGlobalWindow : Window
{
    public PesquisaGlobalWindow()
    {
        InitializeComponent();
        Opened += (_, _) => CaixaConsulta.Focus();
    }

    private void ListBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is PesquisaGlobalViewModel viewModel && viewModel.AbrirSelecionadoCommand.CanExecute(null))
            viewModel.AbrirSelecionadoCommand.Execute(null);
    }
}
