using UnityEngine;

public class PlayerRoot : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerConfigSO config;

    [Header("References")]
    [SerializeField] private PlayerReferences references;

    private PlayerContext ctx;

    private void Awake()
    {
        if (references == null)
            references = GetComponent<PlayerReferences>();

        if (references == null)
        {
            Debug.LogError("[PlayerRoot] PlayerReferences is missing.", this);
            return;
        }

        ApplyConfig();
        BuildContext();
        WireEvents();

        references.StateMachine.Initialize(ctx);
    }

    private void ApplyConfig()
    {
        references.Movement?.SetConfig(config);
        references.DashController?.SetConfig(config);
        references.StateMachine?.SetConfig(config);
        references.Health?.SetConfig(config);
    }

    private void BuildContext()
    {
        ctx = new PlayerContext(
            transform,
            references.Input,
            references.Movement,
            references.Combat,
            references.Aim,
            references.Visual,
            references.Health,
            references.DashVfx,
            references.DashController,
            references.ActionRecorder
        );
    }

    private void WireEvents()
    {
        if (references.Input != null && references.Aim != null)
            references.Input.OnAimScreen += references.Aim.SetAim;

        if (references.Aim != null && references.Visual != null)
            references.Aim.OnAimChanged += references.Visual.SetAim;

        if (references.Aim != null && references.Combat != null)
            references.Aim.OnAimChanged += references.Combat.SetAim;
    }
}