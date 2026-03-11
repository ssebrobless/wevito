namespace Wevito.VNext.Contracts;

public enum CompanionMode
{
    Focused,
    Passive,
    Pinned
}

public enum WindowRole
{
    HomePanel,
    RoamBand,
    ToolPopup
}

public enum PetBehaviorState
{
    Home,
    Roaming,
    Recalling
}

public sealed record ForegroundWindowInfo(
    int ProcessId,
    long Hwnd,
    string ProcessName,
    string Title,
    string ClassName,
    bool IsShellSurface,
    bool IsFullscreenApp);

public sealed record DesktopContext(
    ForegroundWindowInfo ForegroundWindow,
    RectInt WorkArea,
    RectInt PrimaryMonitorBounds,
    PointInt CursorPosition,
    DateTimeOffset TimestampUtc);

public sealed record OverlayRegion(
    WindowRole Role,
    RectInt Bounds,
    bool Interactive);

public sealed record ClipboardPayload(
    string Url,
    string Source,
    DateTimeOffset CapturedAtUtc);

public sealed record DropPayload(
    IReadOnlyList<string> Urls,
    IReadOnlyList<string> FilePaths,
    WindowRole TargetRole,
    DateTimeOffset CapturedAtUtc);

public sealed record BasketItem(
    Guid Id,
    string Url,
    string Label,
    string Source,
    DateTimeOffset CapturedAtUtc);

public sealed record PetActor(
    Guid Id,
    string Name,
    string SpeciesId,
    string AccentColor,
    double HomeX,
    double HomeY,
    double CurrentX,
    double CurrentY,
    double TargetX,
    double TargetY,
    double Speed,
    PetBehaviorState BehaviorState,
    DateTimeOffset NextDecisionAtUtc);

public sealed record ToolSession(
    string ToolId,
    bool IsOpen);

public sealed record CompanionState(
    CompanionMode Mode,
    bool IsPinned,
    string ActiveEnvironmentId,
    ToolSession ActiveTool,
    IReadOnlyList<PetActor> ActivePets,
    IReadOnlyList<BasketItem> BasketItems,
    IReadOnlyDictionary<string, string> SettingsSnapshot);
