using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record GameModeDetectionResult(
    bool IsGameModeActive,
    string Source,
    string Reason);

public sealed class GameModeDetectorService
{
    public const string EnabledSetting = "coexistence_game_mode_enabled";
    public const string GameModeDetectedPacketKind = "game_mode_detected";
    public const string GameModeClearedPacketKind = "game_mode_cleared";
    private const string RegistryKeyPath = @"Software\Microsoft\GameBar";
    private const string AllowAutoGameModeValueName = "AllowAutoGameMode";

    private readonly Func<int?> _registryReader;
    private readonly Func<DateTimeOffset> _clock;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly string _jsonlPath;
    private bool _wasActive;

    public GameModeDetectorService(
        Func<int?>? registryReader = null,
        Func<DateTimeOffset>? clock = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        string? jsonlPath = null)
    {
        _registryReader = registryReader ?? ReadAutoGameModeRegistryValue;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _jsonlPath = string.IsNullOrWhiteSpace(jsonlPath) ? ResolveDefaultJsonlPath() : jsonlPath;
    }

    public GameModeDetectionResult Evaluate(IReadOnlyDictionary<string, string>? settings = null)
    {
        if (!ReadBool(settings, EnabledSetting, true))
        {
            var disabled = new GameModeDetectionResult(false, "settings", "Game Mode honor is disabled.");
            RecordTransition(disabled);
            return disabled;
        }

        var raw = _registryReader();
        var active = raw == 1;
        var result = new GameModeDetectionResult(
            active,
            "registry",
            active ? "Windows Game Mode is enabled in the Game Bar registry." : "Windows Game Mode is not currently enabled by registry signal.");
        RecordTransition(result);
        return result;
    }

    private void RecordTransition(GameModeDetectionResult result)
    {
        if (_killSwitchService?.IsActive() == true || result.IsGameModeActive == _wasActive)
        {
            return;
        }

        _wasActive = result.IsGameModeActive;
        var packetKind = result.IsGameModeActive ? GameModeDetectedPacketKind : GameModeClearedPacketKind;
        var now = _clock();
        Directory.CreateDirectory(Path.GetDirectoryName(_jsonlPath) ?? ".");
        File.AppendAllText(_jsonlPath, JsonSerializer.Serialize(new
        {
            packet_kind = packetKind,
            created_at_utc = now,
            did_use_network = false,
            did_use_hosted_ai = false,
            did_use_local_model = false,
            did_mutate = false,
            summary = result.Reason
        }) + Environment.NewLine);

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            null,
            now,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: _jsonlPath,
            Summary: result.Reason,
            Status: "Completed"));
    }

    private static int? ReadAutoGameModeRegistryValue()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            return key?.GetValue(AllowAutoGameModeValueName) switch
            {
                int value => value,
                string text when int.TryParse(text, out var parsed) => parsed,
                _ => null
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return null;
        }
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string>? settings, string key, bool defaultValue)
    {
        return settings is not null &&
            settings.TryGetValue(key, out var raw) &&
            bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static string ResolveDefaultJsonlPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "audit",
            "game-mode-events.jsonl");
    }
}
