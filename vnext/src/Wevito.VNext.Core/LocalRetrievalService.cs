namespace Wevito.VNext.Core;

public sealed class LocalRetrievalService
{
    public const int DefaultTopK = 20;
    public const int DefaultRerankTopN = 8;
    private readonly PetMemoryStore _memoryStore;
    private readonly RerankHead _rerankHead;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public LocalRetrievalService(
        PetMemoryStore? memoryStore = null,
        RerankHead? rerankHead = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _memoryStore = memoryStore ?? new PetMemoryStore();
        _rerankHead = rerankHead ?? RerankHead.CreateDefault("local-retrieval", DateTimeOffset.UtcNow);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public RetrievalResult Retrieve(
        Guid petId,
        string query,
        string requestedToolFamily = "localResearch",
        int denseTopK = DefaultTopK,
        int keywordTopK = DefaultTopK,
        int rerankTopN = DefaultRerankTopN,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (_killSwitchService?.IsActive() == true)
        {
            Record("local_retrieval", "Blocked local retrieval because kill_switch=true.", "Blocked", timestamp);
            return new RetrievalResult([], new Dictionary<string, RetrievalScore>(StringComparer.OrdinalIgnoreCase), ["kill_switch=true"], timestamp);
        }

        var dense = _memoryStore.SearchDocumentChunksDense(petId, query, denseTopK);
        var keyword = _memoryStore.SearchDocumentChunksKeyword(petId, query, keywordTopK);
        var fused = ReciprocalRankFusion.Fuse(
            dense.Select(result => result.Chunk.ChunkId).ToList(),
            keyword.Select(result => result.Chunk.ChunkId).ToList());
        var byId = dense.Concat(keyword)
            .GroupBy(result => result.Chunk.ChunkId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Chunk, StringComparer.OrdinalIgnoreCase);
        var candidates = fused.Keys
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToList();
        var reranked = _rerankHead.Apply(candidates, query)
            .Take(Math.Max(1, rerankTopN))
            .ToList();
        var scores = new Dictionary<string, RetrievalScore>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in reranked)
        {
            var denseScore = dense.FirstOrDefault(result => result.Chunk.ChunkId.Equals(chunk.ChunkId, StringComparison.OrdinalIgnoreCase))?.Score ?? 0;
            var keywordScore = keyword.FirstOrDefault(result => result.Chunk.ChunkId.Equals(chunk.ChunkId, StringComparison.OrdinalIgnoreCase))?.Score ?? 0;
            var fusionScore = fused.GetValueOrDefault(chunk.ChunkId);
            var rerankScore = _rerankHead.Score(chunk, query);
            scores[chunk.ChunkId] = new RetrievalScore(chunk.ChunkId, denseScore, keywordScore, fusionScore, rerankScore);
        }

        var trace = new[]
        {
            $"dense={dense.Count}/{denseTopK}:{string.Join(",", dense.Select(result => result.Method).Distinct(StringComparer.OrdinalIgnoreCase))}",
            $"keyword={keyword.Count}/{keywordTopK}:{string.Join(",", keyword.Select(result => result.Method).Distinct(StringComparer.OrdinalIgnoreCase))}",
            $"rrf_k={ReciprocalRankFusion.DefaultK}",
            $"rerank={_rerankHead.HeadId}:{requestedToolFamily}:top{rerankTopN}"
        };
        Record("local_retrieval", $"Retrieved {reranked.Count} chunk(s) for {requestedToolFamily}.", "Completed", timestamp);
        return new RetrievalResult(reranked, scores, trace, timestamp);
    }

    private void Record(string packetKind, string summary, string status, DateTimeOffset timestamp)
    {
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
            summary,
            status));
    }
}
