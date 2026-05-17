using System.Text.Json;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class SpriteRepairQueueReaderTests
{
    [Fact]
    public void LoadsRepairQueueJson()
    {
        var root = CreateRepoRoot();
        var queuePath = WriteQueue(root, [
            Row("goose|baby|female", "goose", "baby", "female", "P2", ["tools/repair_runtime_sprite_canvas_padding.py"])
        ]);

        var queue = new SpriteRepairQueueReader().Load(queuePath, root);

        Assert.Equal("1.0", queue.SchemaVersion);
        Assert.Single(queue.Rows);
        Assert.Equal("goose|baby|female", queue.Rows[0].RowId);
        Assert.Equal(["blue"], queue.Rows[0].ColorsAffected);
    }

    [Fact]
    public void OrdersRowsByPriorityP0FirstP3Last()
    {
        var root = CreateRepoRoot();
        var queuePath = WriteQueue(root, [
            Row("rat|adult|male", "rat", "adult", "male", "P3", ["tools/repair_runtime_sprite_canvas_padding.py"]),
            Row("frog|adult|female", "frog", "adult", "female", "P1", ["tools/generate_runtime_pose_sprites.py"]),
            Row("crow|teen|male", "crow", "teen", "male", "P0", ["tools/repair_runtime_sprite_canvas_padding.py"])
        ]);

        var queue = new SpriteRepairQueueReader().Load(queuePath, root);

        Assert.Equal(["P0", "P1", "P3"], queue.Rows.Select(row => row.Priority).ToArray());
        Assert.Equal("crow|teen|male", queue.Rows[0].RowId);
        Assert.Equal("rat|adult|male", queue.Rows[^1].RowId);
    }

    [Fact]
    public void GroupsRowsBySpeciesAgeGender()
    {
        var root = CreateRepoRoot();
        var queuePath = WriteQueue(root, [
            Row("snake|adult|female", "snake", "adult", "female", "P2", ["tools/repair_runtime_sprite_canvas_padding.py"]),
            Row("snake|baby|female", "snake", "baby", "female", "P2", ["tools/repair_runtime_sprite_canvas_padding.py"]),
            Row("snake|teen|male", "snake", "teen", "male", "P2", ["tools/repair_runtime_sprite_canvas_padding.py"])
        ]);

        var queue = new SpriteRepairQueueReader().Load(queuePath, root);
        var groups = queue.Rows.GroupBy(row => (row.SpeciesId, row.LifeStage, row.Gender)).ToArray();

        Assert.Equal(3, groups.Length);
        Assert.All(groups, group => Assert.Single(group));
        Assert.Equal(["baby", "teen", "adult"], queue.Rows.Select(row => row.LifeStage).ToArray());
    }

    [Fact]
    public void DoesNotEmitSeniorAgeStageRows()
    {
        var root = CreateRepoRoot();
        var queuePath = WriteQueue(root, [
            Row("goose|senior|female", "goose", "senior", "female", "P2", ["tools/repair_runtime_sprite_canvas_padding.py"])
        ]);

        var error = Assert.Throws<InvalidOperationException>(() => new SpriteRepairQueueReader().Load(queuePath, root));

        Assert.Contains("Senior", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RespectsKillSwitch()
    {
        var root = CreateRepoRoot(createTools: false);
        var queuePath = WriteQueue(root, [
            Row("goose|baby|female", "goose", "baby", "female", "P0", ["tools/missing.py"])
        ]);
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };

        var queue = new SpriteRepairQueueReader(new KillSwitchService(() => settings)).Load(queuePath, root);

        Assert.Empty(queue.Rows);
        Assert.Equal(0, queue.RowCount);
    }

    [Fact]
    public void RefusesQueueReferencingMissingRepairTool()
    {
        var root = CreateRepoRoot();
        var queuePath = WriteQueue(root, [
            Row("goose|baby|female", "goose", "baby", "female", "P2", ["tools/missing.py"])
        ]);

        var error = Assert.Throws<FileNotFoundException>(() => new SpriteRepairQueueReader().Load(queuePath, root));

        Assert.Contains("missing repair tool", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlainLanguageExplainerCoversSpriteRepairQueuePacket()
    {
        var text = new PlainLanguageExplainer().ExplainPacketKind("sprite_repair_queue_built");

        Assert.Contains("sprite repair queue", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unknown", text, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateRepoRoot(bool createTools = true)
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-sprite-repair-queue-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "tools"));
        File.WriteAllText(Path.Combine(root, "wevito.godot"), "");
        if (createTools)
        {
            File.WriteAllText(Path.Combine(root, "tools", "repair_runtime_sprite_canvas_padding.py"), "# test tool");
            File.WriteAllText(Path.Combine(root, "tools", "generate_runtime_pose_sprites.py"), "# test tool");
        }

        return root;
    }

    private static string WriteQueue(string root, IReadOnlyList<SpriteRepairQueueRow> rows)
    {
        var path = Path.Combine(root, "repair_queue.json");
        var manifest = new SpriteRepairQueueManifest(
            "1.0",
            DateTimeOffset.Parse("2026-05-16T12:00:00Z"),
            "visual_qa_manifest.json",
            "2026-05-16T11:00:00Z",
            rows.Count,
            rows.Count,
            rows.GroupBy(row => row.Priority).ToDictionary(group => group.Key, group => group.Count()),
            new Dictionary<string, int> { ["crop_detected"] = 1 },
            rows);
        File.WriteAllText(path, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private static SpriteRepairQueueRow Row(
        string rowId,
        string speciesId,
        string lifeStage,
        string gender,
        string priority,
        IReadOnlyList<string> tools)
    {
        return new SpriteRepairQueueRow(
            rowId,
            speciesId,
            lifeStage,
            gender,
            priority,
            "queued",
            1,
            ["blue"],
            ["walk"],
            tools,
            [
                new SpriteRepairQueueIssue(
                    "blue",
                    "walk",
                    priority,
                    ["crop_detected"],
                    ["test warning"],
                    tools[0],
                    "test reason",
                    "sprites_runtime/test.png",
                    null)
            ]);
    }
}
