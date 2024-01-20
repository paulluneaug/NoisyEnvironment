using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityUtility.CustomAttributes;
using static NoiseLayer;

public class NoiseTerrainController : MonoBehaviour
{
    //[Serializable]
    //private struct NoiseLayer
    //{
    //    public bool Enabled;

    //    public int NoiseScale;
    //    public bool UseSmootherStep;
    //    public int GradientOffset;
    //    public float LayerWeight;

    //    public PerlinNoiseGenerator.PerlinNoiseLayer ToPerlinNoiseLayer()
    //    {
    //        return new PerlinNoiseGenerator.PerlinNoiseLayer(LayerWeight, GradientOffset, NoiseScale, UseSmootherStep);
    //    }
    //    public VornoiNoiseGenerator.VornoiNoiseLayer ToVornoiNoiseLayer()
    //    {
    //        return new VornoiNoiseGenerator.VornoiNoiseLayer(LayerWeight, GradientOffset, NoiseScale, UseSmootherStep);
    //    }
    //}

    [Button(nameof(UpdateTerrainProperties), "Update Terrain Properties")]
    [SerializeField] private Renderer m_renderer = null;

    [SerializeField] private NoiseType m_noiseType = NoiseType.Perlin;

    [SerializeField] private NoiseLayer m_noiseLayer = null;

    [SerializeField] private Terrain m_terrain;
    [SerializeField] private int m_terrainHeight;

    [SerializeField] private Material m_terrainMaterial;

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

        m_recorder.AddEvent("Noise Generation");

        m_terrain.terrainData.heightmapResolution = m_textureSize.y + 1;
        m_terrain.terrainData.SetHeights(0, 0, terrainDatas);

        m_recorder.AddEvent("Terrain height set and texture");

        m_terrain.Flush();
        m_recorder.AddEvent("Terrain Flush");

        m_recorder.LogAllEventsTimeSpan();
    }

    private float[,] GetHeightMap(Vector2Int zone)
    {
        return m_noiseLayer.Generate(zone);
    }
}
