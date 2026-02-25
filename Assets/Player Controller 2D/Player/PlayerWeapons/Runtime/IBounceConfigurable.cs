using UnityEngine;

public interface IBounceConfigurable
{
    void ConfigureBounce(LayerMask wallMask, int maxBounces, float speedMultiplierPerBounce);
}