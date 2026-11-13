using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AlphaQuant.Desktop.ViewModels;
namespace AlphaQuant.Desktop.Views;
public partial class FinancialAnalysisView : UserControl
{
    private Button? _lastCell;
    public FinancialAnalysisView() => InitializeComponent();
    private void InspectClicked(object? sender, RoutedEventArgs e)
    {
        _lastCell = sender as Button;
        Dispatcher.UIThread.Post(() => CloseEvidenceButton.Focus());
    }
    private void CloseClicked(object? sender, RoutedEventArgs e) => RestoreFocus();
    private void RestoreFocus()
    {
        if (_lastCell?.IsAttachedToVisualTree() == true) _lastCell.Focus();
        else EditCompaniesButton.Focus();
    }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is FinancialAnalysisViewModel { HasEvidence: true } vm)
        { vm.CloseEvidenceCommand.Execute(null); RestoreFocus(); e.Handled = true; }
    }
}
