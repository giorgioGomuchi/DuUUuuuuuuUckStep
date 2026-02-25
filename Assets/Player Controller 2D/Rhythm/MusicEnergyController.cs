using UnityEngine;

public class MusicEnergyController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RhythmCombatController rhythm;
    [SerializeField] private MusicDirector director;

    [Header("Energy")]
    [SerializeField] private float energy;
    [SerializeField] private float decayPerSecond = 4f;

    private const float MAX_ENERGY = 100f;

    private void OnEnable()
    {
        if (rhythm != null)
        {
            rhythm.onInputEvaluated.AddListener(OnInput);
            rhythm.onComboTriggered.AddListener(OnCombo);
        }
    }

    private void OnDisable()
    {
        if (rhythm != null)
        {
            rhythm.onInputEvaluated.RemoveListener(OnInput);
            rhythm.onComboTriggered.RemoveListener(OnCombo);
        }
    }

    private void Update()
    {
        energy -= decayPerSecond * Time.deltaTime;
        energy = Mathf.Clamp(energy, 0, MAX_ENERGY);
    }

    private void OnInput(RhythmInputResult result)
    {
        float delta = 0;

        if (result.quality == RhythmHitQuality.Perfect)
            delta = 10;
        else if (result.quality == RhythmHitQuality.Good)
            delta = 5;
        else
            delta = -5;

        energy += delta;
    }

    private void OnCombo(RhythmComboRecipeSO recipe)
    {
        energy += 25f;

        if (director != null)
            director.TriggerDrop();
    }

    public float GetEnergy01()
    {
        return energy / MAX_ENERGY;
    }
}