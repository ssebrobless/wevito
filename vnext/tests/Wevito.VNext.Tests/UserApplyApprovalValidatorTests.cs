using System.Reflection;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class UserApplyApprovalValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ValidateUserApplyApproval_AcceptsFreshMatchingApproval()
    {
        var result = new UserApplyApprovalValidator().ValidateUserApplyApproval(
            ValidApproval(),
            "sprite-repair-batch-proposal",
            "apply-candidate-001",
            Now);

        Assert.IsType<ApprovalResult.Accepted>(result);
    }

    [Fact]
    public void ValidateUserApplyApproval_RefusesWhenNotConfirmedInThisMessage()
    {
        var result = Validate(ValidApproval() with { UserConfirmedInThisMessage = false });

        AssertRefused(result, "not_confirmed_in_this_message");
    }

    [Fact]
    public void ValidateUserApplyApproval_RefusesWhitespaceConfirmationText()
    {
        var result = Validate(ValidApproval() with { ConfirmationText = "  " });

        AssertRefused(result, "empty_confirmation_text");
    }

    [Fact]
    public void ValidateUserApplyApproval_RefusesStaleConfirmation()
    {
        var result = Validate(ValidApproval() with { ConfirmedAtUtc = Now.AddSeconds(-61) });

        AssertRefused(result, "stale_confirmation");
    }

    [Fact]
    public void ValidateUserApplyApproval_RefusesScopeMismatch()
    {
        var result = new UserApplyApprovalValidator().ValidateUserApplyApproval(
            ValidApproval(),
            "different-scope",
            "apply-candidate-001",
            Now);

        AssertRefused(result, "scope_id_mismatch");
    }

    [Fact]
    public void ValidateUserApplyApproval_RefusesOperationMismatch()
    {
        var result = new UserApplyApprovalValidator().ValidateUserApplyApproval(
            ValidApproval(),
            "sprite-repair-batch-proposal",
            "different-operation",
            Now);

        AssertRefused(result, "operation_id_mismatch");
    }

    [Fact]
    public void ValidateUserApplyApproval_RefusalPathDoesNotThrow()
    {
        var validator = new UserApplyApprovalValidator();

        var exception = Record.Exception(() => validator.ValidateUserApplyApproval(
            null,
            "sprite-repair-batch-proposal",
            "apply-candidate-001",
            Now));

        Assert.Null(exception);
        AssertRefused(
            validator.ValidateUserApplyApproval(null, "sprite-repair-batch-proposal", "apply-candidate-001", Now),
            "approval_missing");
    }

    [Fact]
    public void UserApplyApprovalValidator_ExposesNoBypassOverloads()
    {
        var methods = typeof(UserApplyApprovalValidator)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        var method = Assert.Single(methods);
        Assert.Equal(nameof(UserApplyApprovalValidator.ValidateUserApplyApproval), method.Name);
        Assert.Equal(typeof(ApprovalResult), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(
            [
                typeof(UserApplyApproval),
                typeof(string),
                typeof(string),
                typeof(DateTimeOffset)
            ],
            parameters.Select(parameter => Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType).ToArray());
    }

    [Fact]
    public void NonUserDrivenProductionCode_DoesNotConstructUserApplyApproval()
    {
        var productionRoot = Path.Combine(FindRepositoryRoot(), "vnext", "src");
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(productionRoot, "Wevito.VNext.Core", "SelfImprovement", "UserApplyApproval.cs"),
            Path.Combine(productionRoot, "Wevito.VNext.Core", "SelfImprovement", "UserApplyApprovalValidator.cs"),
            Path.Combine(productionRoot, "Wevito.VNext.Core", "SelfImprovement", "IRequiresUserApplyApproval.cs"),
        };

        var offenders = Directory
            .EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !allowedFiles.Contains(path))
            .Where(path => File.ReadAllText(path).Contains("new UserApplyApproval", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(productionRoot, path))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void NonUserDrivenNamespaces_DoNotExposeUserApplyApprovalInPublicSignatures()
    {
        var assembly = typeof(UserApplyApprovalValidator).Assembly;
        var offenders = assembly
            .GetTypes()
            .Where(IsNonUserDrivenType)
            .SelectMany(FindPublicApprovalSignatureReferences)
            .ToArray();

        Assert.Empty(offenders);
    }

    private static ApprovalResult Validate(UserApplyApproval approval)
    {
        return new UserApplyApprovalValidator().ValidateUserApplyApproval(
            approval,
            "sprite-repair-batch-proposal",
            "apply-candidate-001",
            Now);
    }

    private static UserApplyApproval ValidApproval()
    {
        return new UserApplyApproval(
            UserConfirmedInThisMessage: true,
            ConfirmationText: "I approve applying operation apply-candidate-001 for sprite-repair-batch-proposal.",
            ConfirmedAtUtc: Now,
            ApprovedScopeId: "sprite-repair-batch-proposal",
            ApprovedOperationId: "apply-candidate-001");
    }

    private static void AssertRefused(ApprovalResult result, string reason)
    {
        var refused = Assert.IsType<ApprovalResult.Refused>(result);
        Assert.Equal(reason, refused.Reason);
    }

    private static bool IsNonUserDrivenType(Type type)
    {
        var fullName = type.FullName ?? string.Empty;
        return fullName.Contains("Autonomous", StringComparison.Ordinal)
            || fullName.Contains("Scheduler", StringComparison.Ordinal)
            || fullName.Contains("Scope", StringComparison.Ordinal)
            || fullName.Contains("ModelAdapter", StringComparison.Ordinal)
            || fullName.Contains("LocalModel", StringComparison.Ordinal)
            || fullName.Contains("LocalReasoning", StringComparison.Ordinal)
            || fullName.Contains("Ollama", StringComparison.Ordinal)
            || fullName.Contains("Onnx", StringComparison.Ordinal);
    }

    private static IEnumerable<string> FindPublicApprovalSignatureReferences(Type type)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
        {
            if (constructor.GetParameters().Any(UsesUserApplyApproval))
            {
                yield return $"{type.FullName}::{constructor.Name}";
            }
        }

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            if (method.ReturnType == typeof(UserApplyApproval) || method.GetParameters().Any(UsesUserApplyApproval))
            {
                yield return $"{type.FullName}::{method.Name}";
            }
        }
    }

    private static bool UsesUserApplyApproval(ParameterInfo parameter)
    {
        return parameter.ParameterType == typeof(UserApplyApproval)
            || Nullable.GetUnderlyingType(parameter.ParameterType) == typeof(UserApplyApproval);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
