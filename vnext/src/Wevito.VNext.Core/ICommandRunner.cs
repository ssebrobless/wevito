using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public interface ICommandRunner
{
    Task<ProofExecutionResult> RunAsync(ProofExecutionRequest request, CancellationToken cancellationToken = default);
}
