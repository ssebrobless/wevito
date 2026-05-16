using System.Windows;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public partial class FirstLaunchWizardWindow : Window
{
    private readonly FirstLaunchWizardStateService _wizardStateService;
    private IReadOnlyDictionary<string, string> _settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private int _step = 1;

    public FirstLaunchWizardWindow(FirstLaunchWizardStateService? wizardStateService = null)
    {
        _wizardStateService = wizardStateService ?? new FirstLaunchWizardStateService();
        InitializeComponent();
        Loaded += (_, _) => WindowPlacementHelper.FitInsideWorkArea(this);
    }

    public IReadOnlyDictionary<string, string> ResultSettings => _settings;

    public void LoadSettings(IReadOnlyDictionary<string, string> settings, IReadOnlyList<PetActor> pets)
    {
        _settings = new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase);
        AiNameTextBox.Text = new AiIdentityService().GetAiName(_settings);
        var names = pets.Take(3).Select(pet => pet.Name).ToList();
        AgentOneTextBox.Text = names.ElementAtOrDefault(0) ?? "Agent 1";
        AgentTwoTextBox.Text = names.ElementAtOrDefault(1) ?? "Agent 2";
        AgentThreeTextBox.Text = names.ElementAtOrDefault(2) ?? "Agent 3";
        RenderStep();
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        _step = Math.Max(1, _step - 1);
        RenderStep();
    }

    private void NextButton_OnClick(object sender, RoutedEventArgs e)
    {
        var now = DateTimeOffset.UtcNow;
        _settings = _step switch
        {
            1 => _wizardStateService.CompleteIdentityStep(_settings, AiNameTextBox.Text, now),
            2 => _wizardStateService.CompleteAgentNamesStep(_settings, [AgentOneTextBox.Text, AgentTwoTextBox.Text, AgentThreeTextBox.Text], now),
            3 => _wizardStateService.CompleteBackgroundChoiceStep(_settings, ResolveBackgroundChoice(), now),
            _ => _wizardStateService.CompleteFirstChatStep(_settings, now)
        };

        if (_step >= 4)
        {
            DialogResult = true;
            Close();
            return;
        }

        _step++;
        RenderStep();
    }

    private FirstLaunchBackgroundChoice ResolveBackgroundChoice()
    {
        if (SpriteCleanupRadioButton.IsChecked == true)
        {
            return FirstLaunchBackgroundChoice.HelpWithSpriteCleanup;
        }

        return AddLaterRadioButton.IsChecked == true
            ? FirstLaunchBackgroundChoice.AddLater
            : FirstLaunchBackgroundChoice.JustChat;
    }

    private void RenderStep()
    {
        StepText.Text = $"Step {_step} of 4";
        BackButton.IsEnabled = _step > 1;
        NextButton.Content = _step == 4 ? "Finish" : "Next";

        AiNameStepPanel.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
        AgentNamesStepPanel.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
        BackgroundStepPanel.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;
        FirstChatStepPanel.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed;
        GreetingText.Text = $"Hi, I’m {new AiIdentityService().GetAiName(_settings)}. I’ll stay local-first, keep the pets acting like pets, and help from the chat whenever you’re ready.";
    }
}
