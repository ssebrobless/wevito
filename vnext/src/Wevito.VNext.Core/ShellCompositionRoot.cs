using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Core;

public static class ShellCompositionRoot
{
    public static ConstitutionalDecisionService CreateConstitutionalDecisionService(KillSwitchService? killSwitchService = null)
    {
        return new ConstitutionalDecisionService(killSwitchService);
    }
}
