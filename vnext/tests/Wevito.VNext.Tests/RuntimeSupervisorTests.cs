using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class RuntimeSupervisorTests
{
    [Fact]
    public void Evaluate_DefaultsToActiveButDoesNotStartBackgroundWork()
    {
        var service = new RuntimeSupervisorService();

        var status = service.Evaluate(new Dictionary<string, string>());
        var userStarted = service.CanStartUserInitiatedWork(status, out var userReason);

        Assert.Equal(RuntimeSupervisorMode.Active, status.Mode);
        Assert.False(status.BackgroundWorkAllowed);
        Assert.True(status.ToolWindowAllowed);
        Assert.True(userStarted, userReason);
        Assert.Contains("background helper work is off", status.UserStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_QuietModeBlocksBackgroundWork()
    {
        var service = new RuntimeSupervisorService();
        var settings = new Dictionary<string, string>
        {
            [RuntimeSupervisorService.QuietModeSetting] = bool.TrueString,
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString
        };

        var status = service.Evaluate(settings);
        var allowed = service.CanStartBackgroundWork(status, out var reason);
        var userStarted = service.CanStartUserInitiatedWork(status, out var userReason);

        Assert.Equal(RuntimeSupervisorMode.Quiet, status.Mode);
        Assert.False(status.BackgroundWorkAllowed);
        Assert.False(allowed);
        Assert.False(userStarted);
        Assert.Contains("Quiet mode", reason);
        Assert.Contains("Quiet mode", userReason);
    }

    [Fact]
    public void Evaluate_PetOnlyWinsOverQuietAndBlocksToolWindow()
    {
        var service = new RuntimeSupervisorService();
        var settings = new Dictionary<string, string>
        {
            [RuntimeSupervisorService.QuietModeSetting] = bool.TrueString,
            [RuntimeSupervisorService.PetOnlyModeSetting] = bool.TrueString,
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString
        };

        var status = service.Evaluate(settings);

        Assert.Equal(RuntimeSupervisorMode.PetOnly, status.Mode);
        Assert.False(status.BackgroundWorkAllowed);
        Assert.False(status.ToolWindowAllowed);
        Assert.Contains("Pet-only", status.UserStatus);
    }

    [Fact]
    public void Evaluate_FullscreenAutoQuietBlocksBackgroundWorkAndFocus()
    {
        var service = new RuntimeSupervisorService();
        var settings = new Dictionary<string, string>
        {
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString,
            [RuntimeSupervisorService.AutoQuietFullscreenSetting] = bool.TrueString,
            [RuntimeSupervisorService.NoFocusStealSetting] = bool.TrueString
        };

        var status = service.Evaluate(settings, BuildDesktopContext(isFullscreen: true));

        Assert.Equal(RuntimeSupervisorMode.Quiet, status.Mode);
        Assert.True(status.IsQuietedForFullscreen);
        Assert.False(status.BackgroundWorkAllowed);
        Assert.False(status.ToolWindowAllowed);
        Assert.Contains("fullscreen", status.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_UserInitiatedToolCanStayVisibleDuringFullscreen()
    {
        var service = new RuntimeSupervisorService();
        var settings = new Dictionary<string, string>
        {
            [RuntimeSupervisorService.AutoQuietFullscreenSetting] = bool.TrueString,
            [RuntimeSupervisorService.NoFocusStealSetting] = bool.TrueString
        };

        var status = service.Evaluate(settings, BuildDesktopContext(isFullscreen: true), isUserInitiatedToolOpen: true);

        Assert.True(status.ToolWindowAllowed);
        Assert.False(status.BackgroundWorkAllowed);
    }

    [Fact]
    public void Evaluate_ActiveWithBackgroundAllowedAllowsBackgroundWorkWithinBudget()
    {
        var service = new RuntimeSupervisorService();
        var settings = new Dictionary<string, string>
        {
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString,
            [RuntimeSupervisorService.MaxBackgroundTasksPerHourSetting] = "3",
            [RuntimeSupervisorService.CpuBudgetPercentSetting] = "18",
            [RuntimeSupervisorService.MemoryBudgetMbSetting] = "384"
        };

        var status = service.Evaluate(settings);
        var allowed = service.CanStartBackgroundWork(status, out var reason);

        Assert.True(status.BackgroundWorkAllowed);
        Assert.True(allowed, reason);
        Assert.Contains("3/hr", status.UserStatus);
        Assert.Contains("CPU 18%", status.UserStatus);
        Assert.Contains("384 MB", status.UserStatus);
    }

    [Fact]
    public void ApplyUserInteractingWithPet_BlocksBackgroundForFiveSeconds()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var service = new RuntimeSupervisorService();
        var interaction = new UserInteractingWithPetState();
        interaction.EnterFromGodotPetInput(now, "pointer_down");
        var active = service.Evaluate(new Dictionary<string, string>
        {
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString
        });

        var blocked = service.ApplyUserInteractingWithPet(active, interaction, now.AddSeconds(2));

        Assert.False(blocked.BackgroundWorkAllowed);
        Assert.Equal(RuntimeSupervisorService.UserInteractingWithPetBlockReason, blocked.BlockReason);
    }

    private static DesktopContext BuildDesktopContext(bool isFullscreen)
    {
        return new DesktopContext(
            new ForegroundWindowInfo(
                ProcessId: 123,
                Hwnd: 456,
                ProcessName: "game.exe",
                Title: "Fullscreen Game",
                ClassName: "GameWindow",
                IsShellSurface: false,
                IsFullscreenApp: isFullscreen),
            new RectInt(0, 0, 1920, 1040),
            new RectInt(0, 0, 1920, 1080),
            new PointInt(100, 100),
            DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
    }
}
