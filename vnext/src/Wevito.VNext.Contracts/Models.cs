namespace Wevito.VNext.Contracts;

public enum CompanionMode
{
    Focused,
    Passive,
    Pinned
}

public enum WindowRole
{
    HomePanel,
    RoamBand,
    ToolPopup
}

public enum PetBehaviorState
{
    Home,
    Roaming,
    Recalling
}

public enum PetAgeStage
{
    Baby,
    Teen,
    Adult
}

public enum PetGender
{
    Female,
    Male
}

public enum PetFacingDirection
{
    Left,
    Right
}

public enum PetAnimationState
{
    Idle,
    Walk,
    Eat,
    Happy,
    Sad,
    Sleep,
    Sick,
    Bathe
}

public enum PetStatusType
{
    Hungry,
    Thirsty,
    Sleepy,
    Sick,
    Happy,
    Dirty,
    Lonely,
    Comforted
}

public sealed record PetPersonalityProfile(
    double FoodLove = 0,
    double CuddleNeed = 0,
    double CleanlinessPreference = 0,
    double ActivityLevel = 0,
    double Cheerfulness = 0,
    double SocialNeed = 0,
    double Playfulness = 0,
    double Stubbornness = 0);

public sealed record PetHabitProfile(
    double Nutrition = 72,
    double Hydration = 72,
    double Exercise = 66,
    double Hygiene = 70,
    double Affection = 70,
    double Rest = 68,
    double Medical = 72,
    double Stress = 18,
    int FeedCount = 0,
    int WaterCount = 0,
    int RestCount = 0,
    int PlayCount = 0,
    int GroomCount = 0,
    int BathCount = 0,
    int MedicineCount = 0,
    int DoctorCount = 0);

public sealed record PetConditionRecord(
    string Id,
    int Severity = 1,
    bool IsInnate = false);

public sealed record ForegroundWindowInfo(
    int ProcessId,
    long Hwnd,
    string ProcessName,
    string Title,
    string ClassName,
    bool IsShellSurface,
    bool IsFullscreenApp);

public sealed record DesktopContext(
    ForegroundWindowInfo ForegroundWindow,
    RectInt WorkArea,
    RectInt PrimaryMonitorBounds,
    PointInt CursorPosition,
    DateTimeOffset TimestampUtc);

public sealed record OverlayRegion(
    WindowRole Role,
    RectInt Bounds,
    bool Interactive);

public sealed record ClipboardPayload(
    string Url,
    string Source,
    DateTimeOffset CapturedAtUtc);

public sealed record DropPayload(
    IReadOnlyList<string> Urls,
    IReadOnlyList<string> FilePaths,
    WindowRole TargetRole,
    DateTimeOffset CapturedAtUtc);

public sealed record BasketItem(
    Guid Id,
    string Url,
    string Label,
    string Source,
    DateTimeOffset CapturedAtUtc);

public sealed record PetActor(
    Guid Id,
    string Name,
    string SpeciesId,
    string AccentColor = "#FFFFFF",
    PetAgeStage AgeStage = PetAgeStage.Adult,
    PetGender Gender = PetGender.Female,
    string ColorVariant = "blue",
    double HomeX = 0,
    double HomeY = 0,
    double CurrentX = 0,
    double CurrentY = 0,
    double TargetX = 0,
    double TargetY = 0,
    double BaseSpeed = 96,
    double Speed = 96,
    PetBehaviorState BehaviorState = PetBehaviorState.Home,
    DateTimeOffset NextDecisionAtUtc = default,
    PetFacingDirection FacingDirection = PetFacingDirection.Right,
    PetAnimationState CurrentAnimationState = PetAnimationState.Idle,
    DateTimeOffset AnimationStartedAtUtc = default,
    PetAnimationState? OverrideAnimationState = null,
    DateTimeOffset? OverrideAnimationEndsAtUtc = null,
    string LastActionId = "",
    DateTimeOffset? LastActionAtUtc = null,
    DateTimeOffset AgeStageStartedAtUtc = default,
    double Hunger = 84,
    double Thirst = 82,
    double Energy = 76,
    double Cleanliness = 78,
    double Affection = 72,
    double Comfort = 74,
    double Health = 88,
    double Fitness = 68,
    double BiologicalAgeMinutes = 0,
    PetPersonalityProfile? Personality = null,
    PetHabitProfile? HabitProfile = null,
    IReadOnlyList<PetConditionRecord>? ActiveConditions = null,
    IReadOnlyList<PetStatusType>? ActiveStatuses = null,
    string SelectedEnvironmentId = "");

public sealed record ToolSession(
    string ToolId,
    bool IsOpen);

public sealed record CompanionState(
    CompanionMode Mode,
    bool IsPinned,
    string ActiveEnvironmentId,
    ToolSession ActiveTool,
    IReadOnlyList<PetActor> ActivePets,
    IReadOnlyList<BasketItem> BasketItems,
    IReadOnlyDictionary<string, string> SettingsSnapshot);
