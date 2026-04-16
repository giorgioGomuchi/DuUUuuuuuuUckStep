using UnityEngine;

public enum GlobalRhythmWeaponHint
{
    None = 0,
    Default = 1,
    Boomerang = 2,
    Shotgun = 3,
    Sniper = 4
}

[CreateAssetMenu(
    fileName = "GlobalRhythmTheme",
    menuName = "Game/Rhythm/Global Rhythm Theme")]
public class GlobalRhythmThemeSO : ScriptableObject
{
    [Header("Weapon States")]
    [SerializeField] private GlobalRhythmVisualState defaultWeaponState = GlobalRhythmVisualState.Default;
    [SerializeField] private GlobalRhythmVisualState boomerangWeaponState = GlobalRhythmVisualState.Default;
    [SerializeField] private GlobalRhythmVisualState shotgunWeaponState = GlobalRhythmVisualState.Default;
    [SerializeField] private GlobalRhythmVisualState sniperWeaponState = GlobalRhythmVisualState.Default;

    [Header("Prompt Overrides")]
    [SerializeField] private GlobalRhythmVisualState holdPromptState = GlobalRhythmVisualState.Default;
    [SerializeField] private GlobalRhythmVisualState releasePromptState = GlobalRhythmVisualState.Default;
    [SerializeField] private GlobalRhythmVisualState tapPromptState = GlobalRhythmVisualState.Default;
    [SerializeField] private GlobalRhythmVisualState reflectPromptState = GlobalRhythmVisualState.Default;
    [SerializeField] private GlobalRhythmVisualState dangerPromptState = GlobalRhythmVisualState.Default;

    public GlobalRhythmVisualState GetWeaponState(GlobalRhythmWeaponHint weaponHint)
    {
        return weaponHint switch
        {
            GlobalRhythmWeaponHint.Boomerang => boomerangWeaponState,
            GlobalRhythmWeaponHint.Shotgun => shotgunWeaponState,
            GlobalRhythmWeaponHint.Sniper => sniperWeaponState,
            _ => defaultWeaponState
        };
    }

    public GlobalRhythmVisualState GetPromptState(GlobalRhythmPromptType promptType)
    {
        return promptType switch
        {
            GlobalRhythmPromptType.Hold => holdPromptState,
            GlobalRhythmPromptType.Release => releasePromptState,
            GlobalRhythmPromptType.Tap => tapPromptState,
            GlobalRhythmPromptType.Reflect => reflectPromptState,
            GlobalRhythmPromptType.Danger => dangerPromptState,
            _ => defaultWeaponState
        };
    }
}