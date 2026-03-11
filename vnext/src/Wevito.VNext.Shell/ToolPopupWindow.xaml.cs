using System.Windows;
using System.Windows.Interop;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

public partial class ToolPopupWindow : Window
{
    private bool _closingSilently;

    public ToolPopupWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => OverlayWindowStyler.Apply(this, clickThrough: false, noActivate: false);
        Visibility = Visibility.Hidden;
    }

    public event Func<Task>? CloseRequested;

    public event Action<Guid>? CopyRequested;

    public event Func<Guid, Task>? OpenRequested;

    public event Func<Guid, Task>? DeleteRequested;

    public event Func<IReadOnlyList<string>, Task>? LinksDropped;

    public long WindowHandle => new WindowInteropHelper(this).Handle.ToInt64();

    public void Render(CompanionState state)
    {
        BasketList.ItemsSource = state.BasketItems;
    }

    public void CloseSilently()
    {
        _closingSilently = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_closingSilently)
        {
            Hide();
        }

        base.OnClosed(e);
    }

    private async void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CloseRequested is not null)
        {
            await CloseRequested.Invoke();
        }
    }

    private void CopyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (BasketList.SelectedItem is BasketItem item)
        {
            CopyRequested?.Invoke(item.Id);
        }
    }

    private async void OpenButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (BasketList.SelectedItem is BasketItem item && OpenRequested is not null)
        {
            await OpenRequested.Invoke(item.Id);
        }
    }

    private async void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (BasketList.SelectedItem is BasketItem item && DeleteRequested is not null)
        {
            await DeleteRequested.Invoke(item.Id);
        }
    }

    private async void ToolPopupWindow_OnDrop(object sender, DragEventArgs e)
    {
        if (LinksDropped is null)
        {
            return;
        }

        var urls = DropPayloadReader.ExtractUrls(e.Data);
        if (urls.Count > 0)
        {
            await LinksDropped.Invoke(urls);
        }
    }
}
