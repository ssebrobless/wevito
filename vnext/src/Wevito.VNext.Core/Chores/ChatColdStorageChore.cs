namespace Wevito.VNext.Core.Chores;

public sealed class ChatColdStorageChore
{
    private readonly ChatColdStorageService _service;

    public ChatColdStorageChore(ChatColdStorageService service)
    {
        _service = service;
    }

    public ChatColdStorageResult Run(DateTimeOffset nowUtc)
    {
        return _service.ArchiveInactiveSessions(nowUtc);
    }
}
