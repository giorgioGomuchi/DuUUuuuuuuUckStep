using UnityEngine;

public class PlayerRoot : MonoBehaviour
{
    [SerializeField] private PlayerRefs refs;
    [SerializeField] private PlayerHealth health; // si lo tienes en el mismo GO

    private PlayerContext ctx;

    private void Awake()
    {
        if (refs == null) refs = GetComponent<PlayerRefs>();
        if (health == null) health = GetComponent<PlayerHealth>();

        // Context
        ctx = new PlayerContext(
            transform,
            refs.input,
            refs.movement,
            refs.combat,
            refs.aim,
            refs.visual,
            health
        );

        // Cableado VISUAL/AIM (NO gameplay)
        // Input da screen position; Aim lo traduce a dirección mundo y notifica.
        refs.input.OnAimScreen += refs.aim.SetAim;

        refs.aim.OnAimChanged += refs.visual.SetAim;
        refs.aim.OnAimChanged += refs.combat.SetAim;

        // Inicializa StateMachine con context
        refs.stateMachine.Initialize(ctx);
    }
}