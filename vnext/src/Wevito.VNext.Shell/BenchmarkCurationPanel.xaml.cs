using System.Windows;
using System.Windows.Controls;

namespace Wevito.VNext.Shell;

public partial class BenchmarkCurationPanel : UserControl
{
    public BenchmarkCurationPanel()
    {
        InitializeComponent();
    }

    public BenchmarkCurationViewModel? ViewModel { get; private set; }

    public void Configure(BenchmarkCurationViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        viewModel.LoadPendingCases();
    }

    public void Refresh()
    {
        ViewModel?.LoadPendingCases();
    }

    private void ReviseButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel?.ReviseSelected();
        }
        catch (InvalidOperationException ex)
        {
            if (ViewModel is not null)
            {
                ViewModel.StatusText = ex.Message;
            }
        }
    }

    private void ApproveButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel?.ApproveSelected();
        }
        catch (InvalidOperationException ex)
        {
            if (ViewModel is not null)
            {
                ViewModel.StatusText = ex.Message;
            }
        }
    }

    private void RejectButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel?.RejectSelected();
        }
        catch (InvalidOperationException ex)
        {
            if (ViewModel is not null)
            {
                ViewModel.StatusText = ex.Message;
            }
        }
    }

    private void AddAdversarialCaseButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.AddAdversarialCase();
    }
}
