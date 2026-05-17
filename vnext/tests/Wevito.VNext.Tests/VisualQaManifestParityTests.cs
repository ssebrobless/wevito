using Wevito.VNext.Contracts;

namespace Wevito.VNext.Tests;

public sealed class VisualQaManifestParityTests
{
    [Fact]
    public void ManifestRowCountMatchesMatrix()
    {
        var species = new[] { "rat", "crow", "fox", "snake", "deer", "frog", "pigeon", "raccoon", "squirrel", "goose" };
        var ages = new[] { "baby", "teen", "adult" };
        var genders = new[] { "female", "male" };
        var colors = new[] { "red", "orange", "yellow", "blue", "indigo", "violet" };
        var rows = species
            .SelectMany(speciesId => ages.SelectMany(age => genders.SelectMany(gender => colors.Select(color =>
                new VisualQaMatrixCell(speciesId, age, gender, color, [], [], "No findings.")))))
            .ToList();

        var manifest = new VisualQaMatrixManifest(
            DateTimeOffset.Parse("2026-05-16T12:00:00Z"),
            "C:/repo",
            species.Length * ages.Length * genders.Length * colors.Length,
            species,
            ages,
            genders,
            colors,
            ["idle"],
            rows);

        Assert.Equal(360, manifest.ExpectedCellCount);
        Assert.Equal(manifest.ExpectedCellCount, manifest.Rows.Count);
    }
}
