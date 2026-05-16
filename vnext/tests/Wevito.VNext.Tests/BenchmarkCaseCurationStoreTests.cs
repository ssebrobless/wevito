using Microsoft.Data.Sqlite;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class BenchmarkCaseCurationStoreTests
{
    [Fact]
    public void AppendIsAppendOnly()
    {
        using var temp = BenchmarkCurationTempWorkspace.Create();
        var draftPath = temp.WriteDraft(new BenchmarkCase("chat-1", "chat", "hello", "hello"));
        var store = new BenchmarkCaseCurationStore(temp.DatabasePath);

        store.AppendPending(draftPath);
        store.AssertAppendOnlyGuards();

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = temp.DatabasePath,
            Mode = SqliteOpenMode.ReadWrite
        }.ToString());
        connection.Open();
        using var update = connection.CreateCommand();
        update.CommandText = "UPDATE benchmark_case_reviews SET notes = 'tamper' WHERE id = 1;";
        Assert.Throws<SqliteException>(() => update.ExecuteNonQuery());
        using var delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM benchmark_case_reviews WHERE id = 1;";
        Assert.Throws<SqliteException>(() => delete.ExecuteNonQuery());
    }

    [Fact]
    public void ApproveMovesCaseFromDraftToApproved()
    {
        using var temp = BenchmarkCurationTempWorkspace.Create();
        var draftPath = temp.WriteDraft(new BenchmarkCase("tool-1", "tool-use", "audit", ExpectedToolFamily: "spriteAudit"));
        var store = new BenchmarkCaseCurationStore(temp.DatabasePath);

        var record = store.Approve(draftPath, temp.ApprovedRoot, "user");

        Assert.Equal(BenchmarkCaseReviewState.Approved, record.State);
        Assert.False(File.Exists(draftPath));
        Assert.True(File.Exists(record.ApprovedPath));
        Assert.True((File.GetAttributes(record.ApprovedPath) & FileAttributes.ReadOnly) != 0);
    }

    [Fact]
    public void RejectDeletesDraftWithoutRow()
    {
        using var temp = BenchmarkCurationTempWorkspace.Create();
        var draftPath = temp.WriteDraft(new BenchmarkCase("chat-1", "chat", "hello", "hello"));
        var store = new BenchmarkCaseCurationStore(temp.DatabasePath);

        var record = store.Reject(draftPath, "user");

        Assert.Equal(BenchmarkCaseReviewState.Rejected, record.State);
        Assert.False(File.Exists(draftPath));
        Assert.Equal("", record.ApprovedPath);
        Assert.Empty(Directory.EnumerateFiles(temp.ApprovedRoot, "*.json", SearchOption.AllDirectories));
    }
}
