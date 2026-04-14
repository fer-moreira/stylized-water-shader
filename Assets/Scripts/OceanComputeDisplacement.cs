using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class OceanComputeDisplacement : MonoBehaviour
{
    public ComputeShader computeShader;

    [Header("Wave Settings")]
    public float speed = 2.0f;
    public float bandSize = 2.0f;
    public float spacing = 1.0f;
    public float angle = 45.0f;
    public float strength = 1.0f;
    [Range(0, 1)] public float softness = 0.5f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;

    private ComputeBuffer originalVertexBuffer;
    private ComputeBuffer displacedVertexBuffer;
    private int kernelID;

    public static OceanComputeDisplacement Instance;

    void Awake()
    {
        // Set up the singleton so probes can access this script
        Instance = this;
    }
    
    public float GetWaveHeight(Vector3 worldPosition)
    {
        Vector2 pos2D = new Vector2(worldPosition.x, worldPosition.z);
        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        float projected = Vector2.Dot(pos2D, dir) - (Time.time * speed);
        float period = bandSize + spacing;
        
        float t = projected / Mathf.Max(period, 0.0001f);
        t = t - Mathf.Floor(t); 

        float bandFraction = bandSize / Mathf.Max(period, 0.0001f);
        float edge = bandFraction * softness;

        float step1 = HLSLSmoothstep(0f, edge, t);
        float step2 = HLSLSmoothstep(bandFraction + edge, bandFraction, t);
        
        float band = step1 * step2;
        float waveHeight = Mathf.Clamp01(band) * strength;

        return transform.position.y + waveHeight;
    }

    private float HLSLSmoothstep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.MarkDynamic();

        originalVertices = mesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];

        originalVertexBuffer = new ComputeBuffer(originalVertices.Length, sizeof(float) * 3);
        displacedVertexBuffer = new ComputeBuffer(originalVertices.Length, sizeof(float) * 3);

        originalVertexBuffer.SetData(originalVertices);

        kernelID = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernelID, "originalVertexBuffer", originalVertexBuffer);
        computeShader.SetBuffer(kernelID, "vertexBuffer", displacedVertexBuffer);
    }

    void Update()
    {
        computeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        computeShader.SetMatrix("_WorldToLocal", transform.worldToLocalMatrix);

        computeShader.SetFloat("_Time", Time.time);
        computeShader.SetFloat("_Speed", speed);
        computeShader.SetFloat("_BandSize", bandSize);
        computeShader.SetFloat("_Spacing", spacing);
        computeShader.SetFloat("_Angle", angle);
        computeShader.SetFloat("_Strength", strength);
        computeShader.SetFloat("_Softness", softness);

        int threadGroups = Mathf.CeilToInt((float)originalVertices.Length / 64.0f);
        computeShader.Dispatch(kernelID, threadGroups, 1, 1);

        displacedVertexBuffer.GetData(displacedVertices);

        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
    }

    void OnDestroy()
    {
        if (originalVertexBuffer != null) originalVertexBuffer.Release();
        if (displacedVertexBuffer != null) displacedVertexBuffer.Release();
    }
}
