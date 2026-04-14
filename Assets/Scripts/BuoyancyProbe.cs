using UnityEngine;

public class BuoyancyProbe : MonoBehaviour
{
    // Changed from Update to LateUpdate
    void LateUpdate() 
    {
        if (OceanComputeDisplacement.Instance == null) return;

        float waveY = OceanComputeDisplacement.Instance.GetWaveHeight(transform.position);

        transform.position = new Vector3(transform.position.x, waveY, transform.position.z);
    }
}
