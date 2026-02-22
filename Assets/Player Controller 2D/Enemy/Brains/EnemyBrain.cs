using UnityEngine;

public abstract class EnemyBrain : MonoBehaviour
{
    protected EnemyController controller;

    protected virtual void Awake()
    {
        controller = GetComponent<EnemyController>();
        if (controller == null)
        {
            Debug.LogError($"[{GetType().Name}] Missing EnemyController.", this);
            enabled = false;
        }
    }

    protected virtual void OnEnable()
    {
        Enter();
    }

    protected virtual void Update()
    {
        Tick();
    }

    public virtual void Enter() { }
    public virtual void Tick() { }
}
