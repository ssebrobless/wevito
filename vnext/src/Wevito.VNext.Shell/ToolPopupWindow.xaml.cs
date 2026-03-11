using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

public partial class ToolPopupWindow : Window
{
    private bool _closingSilently;
    private bool _suppressSettingEvents;

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

    public event Action<string, bool>? SettingChanged;

    public long WindowHandle => new WindowInteropHelper(this).Handle.ToInt64();

    public void Render(CompanionState state)
    {
        var toolId = string.IsNullOrWhiteSpace(state.ActiveTool.ToolId) ? "basket" : state.ActiveTool.ToolId;
        var showingBasket = string.Equals(toolId, "basket", StringComparison.OrdinalIgnoreCase);

        Title = showingBasket ? "Wevito Basket" : "Wevito Settings";
        PopupTitle.Text = showingBasket ? "Basket" : "Settings";
        BasketPanel.Visibility = showingBasket ? Visibility.Visible : Visibility.Collapsed;
        BasketButtons.Visibility = showingBasket ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = showingBasket ? Visibility.Collapsed : Visibility.Visible;

        BasketList.ItemsSource = state.BasketItems;

        _suppressSettingEvents = true;
        CompactHudCheckBox.IsChecked = GetSettingBool(state, "compact_hud");
        ShowPetNamesCheckBox.IsChecked = GetSettingBool(state, "show_pet_names");
        ShowStatusSummaryCheckBox.IsChecked = GetSettingBool(state, "show_status_summary", true);
        _suppressSettingEvents = false;
    }

    public void CloseSilently()
    {
        _closingSilently = true;
        Close();
    }

    public async Task<bool> TryInvokeOverlayClickAsync(PointInt screenPosition)
    {
        if (Visibility != Visibility.Visible)
        {
            return false;
        }

        UpdateLayout();
        var localPoint = PointFromScreen(new Point(screenPosition.X, screenPosition.Y));

        if (await TryInvokeButtonAsync(CloseButton, localPoint, CloseRequested))
        {
            return true;
        }

        if (BasketPanel.Visibility == Visibility.Visible)
        {
            if (TrySelectBasketItem(localPoint))
            {
                return true;
            }

            if (TryInvokeBasketButton(CopyButton, localPoint, item => CopyRequested?.Invoke(item.Id)))
            {
                return true;
            }

            if (TryInvokeBasketButton(OpenButton, localPoint, async item =>
                {
                    if (OpenRequested is not null)
                    {
                        await OpenRequested.Invoke(item.Id);
                    }
                }))
            {
                return true;
            }

            if (TryInvokeBasketButton(DeleteButton, localPoint, async item =>
                {
                    if (DeleteRequested is not null)
                    {
                        await DeleteRequested.Invoke(item.Id);
                    }
                }))
            {
                return true;
            }
        }
        else
        {
            if (TryToggleCheckBox(CompactHudCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(ShowPetNamesCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(ShowStatusSummaryCheckBox, localPoint)) { return true; }
        }

        return false;
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

    private void CompactHudCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting("compact_hud", CompactHudCheckBox.IsChecked == true);
    }

    private void ShowPetNamesCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting("show_pet_names", ShowPetNamesCheckBox.IsChecked == true);
    }

    private void ShowStatusSummaryCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting("show_status_summary", ShowStatusSummaryCheckBox.IsChecked != false);
    }

    private void PublishSetting(string key, bool value)
    {
        if (_suppressSettingEvents)
        {
            return;
        }

        SettingChanged?.Invoke(key, value);
    }

    private static bool GetSettingBool(CompanionState state, string key, bool defaultValue = false)
    {
        if (state.SettingsSnapshot.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private async Task<bool> TryInvokeButtonAsync(Button button, Point localPoint, Func<Task>? action)
    {
        if (!IsPointInside(button, localPoint) || action is null)
        {
            return false;
        }

        await action.Invoke();
        return true;
    }

    private bool TrySelectBasketItem(Point localPoint)
    {
        foreach (var item in BasketList.Items)
        {
            if (BasketList.ItemContainerGenerator.ContainerFromItem(item) is not ListBoxItem container)
            {
                continue;
            }

            if (!IsPointInside(container, localPoint))
            {
                continue;
            }

            BasketList.SelectedItem = item;
            return true;
        }

        return false;
    }

    private bool TryInvokeBasketButton(Button button, Point localPoint, Action<BasketItem> action)
    {
        if (!IsPointInside(button, localPoint) || BasketList.SelectedItem is not BasketItem item)
        {
            return false;
        }

        action(item);
        return true;
    }

    private bool TryInvokeBasketButton(Button button, Point localPoint, Func<BasketItem, Task> action)
    {
        if (!IsPointInside(button, localPoint) || BasketList.SelectedItem is not BasketItem item)
        {
            return false;
        }

        _ = action(item);
        return true;
    }

    private bool TryToggleCheckBox(CheckBox checkBox, Point localPoint)
    {
        if (!IsPointInside(checkBox, localPoint))
        {
            return false;
        }

        checkBox.IsChecked = !(checkBox.IsChecked ?? false);
        return true;
    }

    private bool IsPointInside(FrameworkElement element, Point localPoint)
    {
        if (element.Visibility != Visibility.Visible || !element.IsEnabled || element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }

        var origin = element.TransformToAncestor(this).Transform(new Point(0, 0));
        var bounds = new Rect(origin.X, origin.Y, element.ActualWidth, element.ActualHeight);
        return bounds.Contains(localPoint);
    }
}
