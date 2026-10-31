using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AlphaQuant.Desktop.ViewModels;

namespace AlphaQuant.Desktop.Views;

public partial class ComparisonView : UserControl
{
    private ComparisonViewModel? _viewModel;
    private Button? _lastCell;
    public ComparisonView()
    {
        InitializeComponent();
        DataContextChanged += ContextChanged;
        SizeChanged += (_, _) => UpdateEvidenceLayout();
        AttachedToVisualTree += (_, _) => ObserveContext();
        DetachedFromVisualTree += (_, _) => { if (_viewModel is not null) _viewModel.PropertyChanged -= ModelChanged; _viewModel = null; };
    }
    private void ContextChanged(object? sender, EventArgs e) => ObserveContext();
    private void ObserveContext()
    {
        if (_viewModel is not null) _viewModel.PropertyChanged -= ModelChanged;
        _viewModel = DataContext as ComparisonViewModel;
        if (_viewModel is not null) _viewModel.PropertyChanged += ModelChanged;
        UpdateEvidenceLayout();
    }
    private void ModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ComparisonViewModel.HasEvidence)) return;
        UpdateEvidenceLayout();
        if (_viewModel?.HasEvidence == true) CloseEvidenceButton.Focus();
        else if (_lastCell?.IsAttachedToVisualTree() == true) _lastCell.Focus();
        else EditCompaniesButton.Focus();
    }
    private void UpdateEvidenceLayout()
    {
        var wide = Bounds.Width >= 1100 && _viewModel?.HasEvidence == true;
        WorkspaceGrid.ColumnDefinitions[1].Width = new GridLength(wide ? 340 : 0);
        Grid.SetColumn(EvidencePanel, wide ? 1 : 0);
        Grid.SetColumnSpan(EvidencePanel, wide ? 1 : 2);
    }
    private void InspectClicked(object? sender, RoutedEventArgs e) { _lastCell = sender as Button; CloseEvidenceButton.Focus(); }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _viewModel?.HasEvidence == true) { _viewModel.CloseEvidenceCommand.Execute(null); e.Handled = true; }
    }
}
