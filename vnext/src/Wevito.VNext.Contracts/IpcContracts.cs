using System.Text.Json;

namespace Wevito.VNext.Contracts;

public static class ShellCommandTypes
{
    public const string SetPinned = "SetPinned";
    public const string OpenUrl = "OpenUrl";
    public const string CaptureClipboard = "CaptureClipboard";
    public const string RegisterDropTarget = "RegisterDropTarget";
    public const string SetOverlayRegions = "SetOverlayRegions";
    public const string RequestDesktopContext = "RequestDesktopContext";
    public const string Shutdown = "Shutdown";
}

public static class ShellEventTypes
{
    public const string DesktopContextChanged = "DesktopContextChanged";
    public const string ForegroundChanged = "ForegroundChanged";
    public const string ClipboardUrlAvailable = "ClipboardUrlAvailable";
    public const string DropReceived = "DropReceived";
    public const string HotkeyPressed = "HotkeyPressed";
    public const string OverlayClickReceived = "OverlayClickReceived";
    public const string WorkAreaChanged = "WorkAreaChanged";
    public const string PowerStateChanged = "PowerStateChanged";
    public const string ShellActionFailed = "ShellActionFailed";
}

public sealed record ShellCommandEnvelope(
    string CommandType,
    JsonElement Payload);

public sealed record ShellEventEnvelope(
    string EventType,
    JsonElement Payload);

public sealed record SetPinnedCommand(bool IsPinned);

public sealed record OpenUrlCommand(string Url);

public sealed record CaptureClipboardCommand();

public sealed record RegisterDropTargetCommand(WindowRole Role);

public sealed record SetOverlayRegionsCommand(IReadOnlyList<OverlayRegion> Regions);

public sealed record RequestDesktopContextCommand();

public sealed record ShutdownBrokerCommand();

public sealed record HotkeyPressedEvent(string ActionId);

public sealed record OverlayClickEvent(WindowRole Role, PointInt ScreenPosition, DateTimeOffset ClickedAtUtc);

public sealed record ShellActionFailedEvent(string ActionId, string Message);

public static class PipeMessage
{
    public static string SerializeCommand<TPayload>(string commandType, TPayload payload)
    {
        var envelope = new ShellCommandEnvelope(commandType, JsonSerializer.SerializeToElement(payload, JsonDefaults.Options));
        return JsonSerializer.Serialize(envelope, JsonDefaults.Options);
    }

    public static string SerializeEvent<TPayload>(string eventType, TPayload payload)
    {
        var envelope = new ShellEventEnvelope(eventType, JsonSerializer.SerializeToElement(payload, JsonDefaults.Options));
        return JsonSerializer.Serialize(envelope, JsonDefaults.Options);
    }

    public static ShellCommandEnvelope DeserializeCommand(string line)
    {
        return JsonSerializer.Deserialize<ShellCommandEnvelope>(line, JsonDefaults.Options)
            ?? throw new InvalidOperationException("Failed to deserialize command envelope.");
    }

    public static ShellEventEnvelope DeserializeEvent(string line)
    {
        return JsonSerializer.Deserialize<ShellEventEnvelope>(line, JsonDefaults.Options)
            ?? throw new InvalidOperationException("Failed to deserialize event envelope.");
    }

    public static TPayload DeserializePayload<TPayload>(JsonElement payload)
    {
        return payload.Deserialize<TPayload>(JsonDefaults.Options)
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(TPayload).Name} payload.");
    }
}
