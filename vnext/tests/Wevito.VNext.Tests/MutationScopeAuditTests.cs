using System.Reflection;
using System.Reflection.Emit;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.Sandbox;
using Wevito.VNext.Core.SelfImprovement.Apply;
using Wevito.VNext.Tests.Support;

namespace Wevito.VNext.Tests;

public sealed class MutationScopeAuditTests
{
    private static readonly Dictionary<short, OpCode> OpCodesByValue =
        typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.GetValue(null) is OpCode)
            .Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(code => code.Value);

    [Fact]
    public void MutationAllowList_ContainsExactlyTheCPhase186Types()
    {
        Assert.Equal(
            [
                "ArtifactRenameApplyRunner",
                "ArtifactRenameRollbackRunner",
                "AuditLedgerService",
                "HeldOutEvalSeedTool",
                "InDistributionEvalSeedTool",
                "ReplayResultStore",
                "SnapshotExportService",
                "SnapshotVerifyTool"
            ],
            MutationAllowList.TypeNames.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void MutationScopeGuard_SourceDoesNotSwallowScopeViolations()
    {
        var source = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Sandbox", "MutationScopeGuard.cs"));

        Assert.DoesNotContain("catch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("try", source, StringComparison.Ordinal);
    }

    [Fact]
    public void KnownSafetyCriticalMutators_AreDiscoveredFromIlAndAllowListed()
    {
        var discovered = DiscoverMutatingCallers([
            typeof(ArtifactRenameApplyRunner),
            typeof(ArtifactRenameRollbackRunner),
            typeof(AuditLedgerService)
        ]);

        Assert.Contains(nameof(ArtifactRenameApplyRunner), discovered);
        Assert.Contains(nameof(ArtifactRenameRollbackRunner), discovered);
        Assert.Contains(nameof(AuditLedgerService), discovered);
        Assert.All(discovered, typeName => Assert.Contains(typeName, MutationAllowList.TypeNames));
    }

    [Fact]
    public void ApplyAndRollbackRunners_CallMutationScopeGuard()
    {
        var applySource = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ArtifactRenameApplyRunner.cs"));
        var rollbackSource = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ArtifactRenameRollbackRunner.cs"));

        Assert.Contains("MutationScopeGuard.ThrowIfOutsideScope", applySource, StringComparison.Ordinal);
        Assert.Contains("MutationScopeGuard.ThrowIfOutsideScope", rollbackSource, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (InvalidOperationException", applySource, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (InvalidOperationException", rollbackSource, StringComparison.Ordinal);
    }

    [Fact]
    public void MutationScopeAuditPacket_IsKnownButNotEmittedByGuard()
    {
        var guardSource = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Sandbox", "MutationScopeGuard.cs"));

        Assert.Equal("self_improvement_mutation_scope_audit_event", SelfImprovementPacketKinds.MutationScopeAuditEvent);
        Assert.Contains(SelfImprovementPacketKinds.MutationScopeAuditEvent, PlainLanguageExplainer.KnownPacketKinds);
        Assert.DoesNotContain("EvidencePacket", guardSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Record(", guardSource, StringComparison.Ordinal);
    }

    [Fact]
    public void MutationScopeAuditFlag_DefaultsFalse()
    {
        var entry = CapabilityFlagInventory.Entries.Single(entry => entry.Name == "mutation_scope_audit_emit_enabled");

        Assert.Equal(bool.FalseString, entry.DefaultValue);
    }

    private static HashSet<string> DiscoverMutatingCallers(IEnumerable<Type> types)
    {
        var callers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in types)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (MethodCallsMutatingApi(method))
                {
                    callers.Add(type.Name);
                }
            }
        }

        return callers;
    }

    private static bool MethodCallsMutatingApi(MethodInfo method)
    {
        var body = method.GetMethodBody();
        var il = body?.GetILAsByteArray();
        if (il is null)
        {
            return false;
        }

        var module = method.Module;
        for (var index = 0; index < il.Length;)
        {
            var offset = index;
            var codeValue = (int)il[index++];
            if (codeValue == 0xFE)
            {
                codeValue = unchecked((short)(0xFE00 | il[index++]));
            }

            if (!OpCodesByValue.TryGetValue((short)codeValue, out var opCode))
            {
                continue;
            }

            if ((opCode == OpCodes.Call || opCode == OpCodes.Callvirt || opCode == OpCodes.Newobj) && index + 4 <= il.Length)
            {
                var token = BitConverter.ToInt32(il, index);
                if (IsMutatingMember(module, token))
                {
                    return true;
                }
            }

            index = offset + 1 + OperandSize(opCode, il, offset + 1);
        }

        return false;
    }

    private static bool IsMutatingMember(Module module, int token)
    {
        MemberInfo member;
        try
        {
            member = module.ResolveMember(token) ?? throw new ArgumentException("Token did not resolve to a member.", nameof(token));
        }
        catch (ArgumentException)
        {
            return false;
        }

        return member switch
        {
            MethodBase { DeclaringType: { FullName: "System.IO.File" }, Name: "Move" or "Copy" or "Delete" or "WriteAllText" or "WriteAllBytes" or "OpenWrite" } => true,
            MethodBase { DeclaringType: { FullName: "System.IO.Directory" }, Name: "CreateDirectory" or "Delete" or "Move" } => true,
            ConstructorInfo { DeclaringType: { FullName: "System.IO.FileStream" } } => true,
            _ => false
        };
    }

    private static int OperandSize(OpCode opCode, byte[] il, int operandStart)
    {
        return opCode.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI or OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType or OperandType.ShortInlineR => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => 4 + (BitConverter.ToInt32(il, operandStart) * 4),
            _ => 0
        };
    }

    private static string SourcePath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate {string.Join('/', parts)} from test output directory.");
    }
}
