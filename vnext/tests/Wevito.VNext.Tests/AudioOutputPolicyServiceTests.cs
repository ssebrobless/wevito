using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AudioOutputPolicyServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

    [Fact]
    public void NoSoundByDefault()
    {
        var service = new AudioOutputPolicyService();

        var decision = service.Evaluate(new AudioOutputRequest("chirp", UserTriggered: true, IsTextToSpeech: false), new Dictionary<string, string>(), Now);

        Assert.False(decision.CanPlay);
        Assert.Equal("pet_sounds_disabled_by_default", decision.Reason);
    }

    [Fact]
    public void UserTriggeredSoundsPlayWhenEnabled()
    {
        var service = new AudioOutputPolicyService();
        var settings = new Dictionary<string, string>
        {
            [AudioOutputPolicyService.PetSoundEffectsEnabledSetting] = bool.TrueString
        };

        var decision = service.Evaluate(new AudioOutputRequest("chirp", UserTriggered: true, IsTextToSpeech: false), settings, Now);

        Assert.True(decision.CanPlay);
        Assert.False(decision.IsTtsBanned);
    }

    [Fact]
    public void TtsIsBanned()
    {
        var service = new AudioOutputPolicyService();
        var settings = new Dictionary<string, string>
        {
            [AudioOutputPolicyService.PetSoundEffectsEnabledSetting] = bool.TrueString
        };

        var decision = service.Evaluate(new AudioOutputRequest("read-aloud", UserTriggered: true, IsTextToSpeech: true), settings, Now);

        Assert.False(decision.CanPlay);
        Assert.True(decision.IsTtsBanned);
        Assert.False(AudioOutputPolicyService.CanUseTextToSpeech());
    }
}
