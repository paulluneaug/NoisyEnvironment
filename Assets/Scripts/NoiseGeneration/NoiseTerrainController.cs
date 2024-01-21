using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityUtility.CustomAttributes;
using static NoiseLayer;

public class NoiseTerrainController : MonoBehaviour
{
    [Button(nameof(UpdateTerrainProperties), "Update Terrain Properties")]
    [SerializeField] private Renderer m_renderer = null;

    [SerializeField] private NoiseLayerBase m_terrainNoiseLayer = null;

    [SerializeField] private Terrain m_terrain;
    [SerializeField] private int m_terrainHeight;

    [SerializeField] private Material m_terrainMaterial;


    [Title("City")]
    [SerializeField] private NoiseLayerBase m_cityLayer = null;
    [SerializeField] private NoiseLayerBase m_cityMask = null;


    [SerializeField, Range(0, 1)] private float m_cityMaskTreshold;

    [SerializeField] private float m_cityMaxHeight;
    [SerializeField, MinMaxSlider(0, 1)] private Vector2 m_cityHeightRange;


    [NonSerialized] private Vector2Int m_textureSize = Vector2Int.one;


    [NonSerialized] private ScriptExecutionTimeRecorder m_recorder = null;


    private void Awake()
    {
       
        m_terrain.materialTemplate = m_terrainMaterial;

        m_recorder = new ScriptExecutionTimeRecorder();


        UpdateTerrainProperties();
    }

    public void UpdateTerrainProperties()
    {
        m_recorder ??= new ScriptExecutionTimeRecorder();
        m_recorder.Reset();

        int resolution = m_terrain.terrainData.heightmapResolution;
        m_textureSize = new Vector2Int(resolution - 1, resolution - 1);

        float[,] terrainDatas = GetHeightMap(m_textureSize);


        m_recorder.AddEvent("Terrain Generation");

        UpdateCity(m_textureSize, terrainDatas);

        m_recorder.AddEvent("City Generation");

        m_terrain.terrainData.heightmapResolution = m_textureSize.y + 1;
        m_terrain.terrainData.SetHeights(0, 0, terrainDatas);

        m_recorder.AddEvent("Terrain height set and texture");

        m_terrain.Flush();
        m_recorder.AddEvent("Terrain Flush");

        m_recorder.LogAllEventsTimeSpan();
    }

    private void UpdateCity(Vector2Int zone, float[,] heightMap)
    {
        var cityHeightMap = m_cityLayer.GetHeightMap(zone);
        var cityMask = m_cityMask.GetHeightMap(zone);

        for (int y = 0; y < zone.y; y++)
        {
            Action<int> act = (x) =>
            {
                if (cityMask[x, y] < m_cityMaskTreshold || cityHeightMap[x, y] == 0.0f) { return; }
                heightMap[x, y] += Mathf.Lerp(m_cityHeightRange.x, m_cityHeightRange.y, cityHeightMap[x, y]) * m_cityMaxHeight * Mathf.Pow(cityMask[x, y], 3);
            };
            Parallel.For(0, zone.x, act);
        }
    }

    private float[,] GetHeightMap(Vector2Int zone)
    {
        var result = m_terrainNoiseLayer.GetHeightMap(zone);
        float[,] heightMap = new float[zone.x, zone.y];

        Buffer.BlockCopy(result, 0, heightMap, 0, zone.x * zone.y * sizeof(float));
        return heightMap;
    }
}
