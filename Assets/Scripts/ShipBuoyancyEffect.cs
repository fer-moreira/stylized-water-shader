using UnityEngine;

public class ShipBuoyancyEffect : MonoBehaviour
{

    [SerializeField] private Transform boatTransform;

    [SerializeField] private Transform[] heightDetect;

    public Vector3 faceNormal;
    public float upAngleDegrees;


    void Update()
    {
        if (OceanComputeDisplacement.Instance == null) return;
        CalculatePosition();
        CalculateAngle();
    }

    private void CalculateAngle()
    {
        if (heightDetect.Length < 3) return;

        Vector3 a = heightDetect[0].position;
        Vector3 b = heightDetect[1].position;
        Vector3 c = heightDetect[2].position;

        Vector3 edge1 = b - a;
        Vector3 edge2 = c - a;

        faceNormal = Vector3.Cross(edge1, edge2).normalized;

        if (faceNormal.y < 0) faceNormal = -faceNormal;

        upAngleDegrees = Vector3.Angle(faceNormal, Vector3.up);

        Quaternion tiltRotation = Quaternion.FromToRotation(Vector3.up, faceNormal);

        float currentYRotation = boatTransform.parent.eulerAngles.y;
        Quaternion finalRotation = tiltRotation * Quaternion.Euler(0f, currentYRotation, 0f);

        boatTransform.rotation = finalRotation;
    }

    private void CalculatePosition()
    {
        for (int i = 0; i < heightDetect.Length; i++)
        {
            float _height = OceanComputeDisplacement.Instance.GetWaveHeight(heightDetect[i].position);
            heightDetect[i].position = new Vector3(heightDetect[i].position.x, _height, heightDetect[i].position.z);
        }

        float averageHeight = 0f;
        foreach (Transform corner in heightDetect)
        {
            averageHeight += corner.position.y;
        }

        averageHeight /= heightDetect.Length;

        boatTransform.position = new Vector3(boatTransform.position.x, averageHeight, boatTransform.position.z);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        foreach (Transform corner in heightDetect)
        {
            Gizmos.DrawSphere(corner.position, 0.1f);
            Gizmos.DrawRay(corner.position, Vector3.up * 0.5f);
        }
    }
#endif
}
