using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text.Json;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.Tools.InDistributionSeed;

public static partial class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return Run(args, Console.Out, Console.Error);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || !args[0].Equals("add", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Usage: add --domain <X> --prompt-file <P> --expected-file <E> --root <R> [--id <ID>] [--notes <N>]");
            return 1;
        }

        var options = Parse(args.Skip(1).ToArray());
        if (!options.TryGetValue("domain", out var domain) ||
            !options.TryGetValue("prompt-file", out var promptFile) ||
            !options.TryGetValue("expected-file", out var expectedFile) ||
            !options.TryGetValue("root", out var root))
        {
            error.WriteLine("Missing required argument.");
            return 1;
        }

        if (!File.Exists(promptFile) || !File.Exists(expectedFile))
        {
            error.WriteLine("Prompt and expected files must exist.");
            return 1;
        }

        var promptBytes = File.ReadAllBytes(promptFile);
        var expectedBytes = File.ReadAllBytes(expectedFile);
        var promptSha = Sha256(promptBytes);
        var expectedSha = Sha256(expectedBytes);
        var id = options.TryGetValue("id", out var requestedId) && !string.IsNullOrWhiteSpace(requestedId)
            ? requestedId
            : Sha256(promptBytes.Concat(expectedBytes).ToArray())[..20];

        var candidate = new InDistributionEvalCase(
            id,
            domain,
            promptSha,
            expectedSha,
            DateTimeOffset.UtcNow,
            options.TryGetValue("notes", out var notes) ? notes : "");

        var validationError = Validate(candidate);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            error.WriteLine(validationError);
            return 1;
        }

        var canonicalRoot = EnsureTrailingSeparator(Path.GetFullPath(root));
        var outputPath = Path.GetFullPath(Path.Combine(canonicalRoot, candidate.Id + ".json"));
        if (!outputPath.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Output path escapes root.");
            return 1;
        }

        if (File.Exists(outputPath))
        {
            error.WriteLine("Case already exists.");
            return 1;
        }

        Directory.CreateDirectory(canonicalRoot);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(candidate, new JsonSerializerOptions { WriteIndented = true }));
        output.WriteLine(Path.GetRelativePath(canonicalRoot, outputPath));
        return 0;
    }

    private static string? Validate(InDistributionEvalCase candidate)
    {
        if (!IdRegex().IsMatch(candidate.Id ?? ""))
        {
            return "id_invalid";
        }

        if (!DomainRegex().IsMatch(candidate.Domain ?? ""))
        {
            return "domain_invalid";
        }

        if (!Sha256Regex().IsMatch(candidate.PromptSha256 ?? ""))
        {
            return "prompt_sha256_invalid";
        }

        if (!Sha256Regex().IsMatch(candidate.ExpectedKindSha256 ?? ""))
        {
            return "expected_kind_sha256_invalid";
        }

        if (string.Equals(candidate.PromptSha256, candidate.ExpectedKindSha256, StringComparison.Ordinal))
        {
            return "hashes_must_differ";
        }

        if (candidate.AuthoredAtUtc <= DateTimeOffset.MinValue)
        {
            return "authored_at_utc_invalid";
        }

        return (candidate.Notes ?? "").Length > 1024 ? "notes_too_long" : null;
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            var key = args[index];
            if (!key.StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
            {
                throw new ArgumentException($"Invalid argument: {key}");
            }

            options[key[2..]] = args[++index];
        }

        return options;
    }

    private static string Sha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdRegex();

    [GeneratedRegex("^[a-z][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex DomainRegex();

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();
}
