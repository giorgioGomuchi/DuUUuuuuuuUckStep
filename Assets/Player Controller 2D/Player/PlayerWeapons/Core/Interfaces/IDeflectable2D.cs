using UnityEngine;

public interface IDeflectable2D
{
    bool CanBeDeflected { get; }
    void Deflect(DeflectInfo info);
}

public struct DeflectInfo
{
    public Vector2 newDirection;
    public LayerMask newTargetMask;
    public float speedMultiplier;
    public Object instigator; // opcional, para debug (MeleeHitController, etc)

    public DeflectInfo(Vector2 newDirection, LayerMask newTargetMask, float speedMultiplier = 1f, Object instigator = null)
    {
        this.newDirection = newDirection;
        this.newTargetMask = newTargetMask;
        this.speedMultiplier = Mathf.Max(0.05f, speedMultiplier);
        this.instigator = instigator;
    }
}