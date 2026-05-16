using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record NotificationRequest(
    string Id,
    string Kind,
    bool IsEmergency,
    string Summary);

public sealed record NotificationContext(
    bool UserTypingRecently,
    bool ForegroundFullscreen,
    bool CoexistenceActive,
    bool DoNotDisturbActive,
    DateTimeOffset NowUtc);

public sealed record NotificationPolicyDecision(
    bool DeliverNow,
    bool Deferred,
    bool StealsFocus,
    string Reason);

public sealed class NotificationPolicyService
{
    public const string DeferDuringActivitySetting = "notification_defer_during_user_activity";
    public const string NotificationDeferredPacketKind = "notification_deferred";
    public const string NotificationDeliveredPacketKind = "notification_delivered";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly List<QueuedNotification> _queue = [];

    public NotificationPolicyService(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public IReadOnlyList<NotificationRequest> QueuedNotifications => _queue.Select(item => item.Request).ToList();

    public NotificationPolicyDecision Submit(
        NotificationRequest request,
        NotificationContext context,
        IReadOnlyDictionary<string, string>? settings = null)
    {
        if (request.IsEmergency)
        {
            Record(NotificationDeliveredPacketKind, context.NowUtc, $"Delivered emergency notification {request.Id} without stealing focus.", "Delivered");
            return new NotificationPolicyDecision(true, false, false, "emergency_bypasses_queue_without_focus_steal");
        }

        if (ShouldDefer(context, settings))
        {
            if (_killSwitchService?.IsActive() != true)
            {
                _queue.RemoveAll(item => string.Equals(item.Request.Id, request.Id, StringComparison.OrdinalIgnoreCase));
                _queue.Add(new QueuedNotification(request, context.NowUtc));
                Record(NotificationDeferredPacketKind, context.NowUtc, $"Deferred notification {request.Id} while user activity or focus protection was active.", "Deferred");
            }

            return new NotificationPolicyDecision(false, true, false, "deferred_until_idle");
        }

        Record(NotificationDeliveredPacketKind, context.NowUtc, $"Delivered notification {request.Id} at idle moment.", "Delivered");
        return new NotificationPolicyDecision(true, false, false, "idle_delivery");
    }

    public IReadOnlyList<NotificationPolicyDecision> DeliverReady(
        NotificationContext context,
        IReadOnlyDictionary<string, string>? settings = null)
    {
        if (_killSwitchService?.IsActive() == true || ShouldDefer(context, settings))
        {
            return [];
        }

        var decisions = new List<NotificationPolicyDecision>();
        foreach (var item in _queue.ToList())
        {
            _queue.Remove(item);
            decisions.Add(new NotificationPolicyDecision(true, false, false, "delivered_on_next_idle_moment"));
            Record(NotificationDeliveredPacketKind, context.NowUtc, $"Delivered deferred notification {item.Request.Id} after the next idle moment.", "Delivered");
        }

        return decisions;
    }

    public static bool ShouldDefer(NotificationContext context, IReadOnlyDictionary<string, string>? settings = null)
    {
        var deferDuringActivity = GetBool(settings, DeferDuringActivitySetting, true);
        return context.DoNotDisturbActive ||
               context.ForegroundFullscreen ||
               context.CoexistenceActive ||
               (deferDuringActivity && context.UserTypingRecently);
    }

    private void Record(string packetKind, DateTimeOffset timestamp, string summary, string status)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: status,
            Error: ""));
    }

    private static bool GetBool(IReadOnlyDictionary<string, string>? settings, string key, bool defaultValue)
    {
        return settings is not null &&
               settings.TryGetValue(key, out var raw) &&
               bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private sealed record QueuedNotification(NotificationRequest Request, DateTimeOffset QueuedAtUtc);
}
