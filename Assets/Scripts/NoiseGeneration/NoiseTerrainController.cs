using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityUtility.CustomAttributes;

public class NoiseTerrainController : MonoBehaviour
{
    [Button(nameof(UpdateTerrainProperties), "Update Terrain Properties")]
    [SerializeField] private Renderer m_renderer = null;

    [SerializeField] private NoiseLayerBase m_terrainNoiseLayer = null;

    [SerializeField] private Terrain m_terrain;
    [SerializeField] private int m_terrainHeight;

    [SerializeField] private Material m_terrainMaterial;

    [SerializeField] private Transform m_waterPlane;

    [SerializeField] private Color m_waterColor;
    [SerializeField] private Gradient m_terrainColorAtHeigth;
    [SerializeField] private Gradient m_steepTerrainColorAtHeigth;

    [SerializeField, Range(0, 1)] private float m_waterLevel;
    [SerializeField, Range(0, 1)] private float m_steepTerrainTreshold;
    [SerializeField] private Texture2D m_terrainTexture;


    [Title("City")]
    [SerializeField] private bool m_addCity;
    [SerializeField] private NoiseLayerBase m_cityLayer = null;
    [SerializeField] private NoiseLayerBase m_cityMask = null;
    [SerializeField] private NoiseLayerBase m_m_roadMask = null;


    [SerializeField, Range(0, 1)] private float m_cityMaskTreshold;
    [SerializeField, Range(0, 1)] private float m_roadMaskTreshold;

    [SerializeField] private float m_cityMaxHeight;
    [SerializeField, MinMaxSlider(0, 1)] private Vector2 m_cityHeightRange;

    [SerializeField] private Color m_roadColor;
    [SerializeField] private Gradient m_buildingGradient;

    [Title("Trees")]
    [SerializeField] private bool m_addTrees;
    [SerializeField] private NoiseLayerBase m_treeLayer;
    [SerializeField] private float m_treeProbabilityMultiplier = 1.0f;
    [SerializeField] private GameObject m_treePrfab;


    [NonSerialized] private Vector2Int m_textureSize = Vector2Int.one;


    [NonSerialized] private ScriptExecutionTimeRecorder m_recorder = null;

    private void Awake()
    {
       
        m_terrain.materialTemplate = m_terrainMaterial;

        m_recorder = new ScriptExecutionTimeRecorder();


        UpdateTerrainProperties();
    }

    public unsafe void UpdateTerrainProperties()
    {
        m_recorder ??= new ScriptExecutionTimeRecorder();
        m_recorder.Reset();

        int resolution = m_terrain.terrainData.heightmapResolution;
        m_textureSize = new Vector2Int(resolution - 1, resolution - 1);

        float[,] terrainDatas = GetHeightMap(m_textureSize);
        Color[,] terrainTextureData = new Color[m_textureSize.x, m_textureSize.y];

        Color[] terrainTextureFlatData = new Color[m_textureSize.x * m_textureSize.y];

        m_recorder.AddEvent("Terrain Generation");

        Vector3 waterPos = m_waterPlane.position;
        waterPos.y = m_terrain.terrainData.heightmapScale.y * m_waterLevel;
        m_waterPlane.position = waterPos;

        m_recorder.AddEvent("Water Height");

        if (m_addCity)
        {
            UpdateCityHeight(m_textureSize, terrainDatas);
        }
        m_recorder.AddEvent("City Generation");

        m_terrain.terrainData.heightmapResolution = m_textureSize.y + 1;
        m_terrain.terrainData.SetHeights(0, 0, terrainDatas);
        m_terrain.Flush();
        m_recorder.AddEvent("Terrain height set and texture");

        PaintTerrain(m_textureSize, terrainDatas, terrainTextureData, m_terrain.terrainData);

        m_recorder.AddEvent("Paint terrain");

        PaintCity(m_textureSize, terrainTextureData);

        if (m_addCity)
        {
            m_recorder.AddEvent("Paint city");
        }

        ApplyTexture(terrainTextureData, terrainTextureFlatData);

        m_recorder.AddEvent("Applicatin of the texture");

        if (m_addTrees)
        {
            AddTrees(m_textureSize, terrainDatas, m_terrain);
        }

        m_recorder.AddEvent("Terrain Flush");

        m_recorder.LogAllEventsTimeSpan();
    }

    private void AddTrees(Vector2Int zone, float[,] heightMap, Terrain terrain)
    {
        float[,] treeDensity = m_treeLayer.GetHeightMap(zone);

        uint seed = 564318651;

        List<TreeInstance> treeInstances = new List<TreeInstance>();
        for (int y = 0; y < zone.y; y++)
        {
            for (int x = 0; x < zone.x; x++)
            {
                if (heightMap[x, y] < m_waterLevel)
                {
                    continue;
                }

                if (treeDensity[x, y] * m_treeProbabilityMultiplier > NoiseUtils.RandomFloat01(ref seed))
                {
                    TreeInstance tree = new TreeInstance()
                    {
                        position = new Vector3((float)y / zone.y, 0, (float)x / zone.x),
                        rotation = NoiseUtils.RandomFloat01(ref seed) * 360,
                        prototypeIndex = 0,
                        heightScale = 1,
                        widthScale = 1,
                        color = Color.white,
                        lightmapColor = Color.white,
                    };
                    treeInstances.Add(tree);
                }
            }
        }
        var a = terrain.terrainData.treeInstances;
        terrain.terrainData.SetTreeInstances(treeInstances.ToArray(), true);

    }

    private unsafe void ApplyTexture(Color[,] terrainTextureData, Color[] terrainTextureFlatData)
    {
        fixed (Color* destPtr = &terrainTextureFlatData[0])
        {
            fixed (Color* srcPtr = &terrainTextureData[0, 0])
            {
                UnsafeUtility.MemCpy(destPtr, srcPtr, m_textureSize.x * m_textureSize.y * UnsafeUtility.SizeOf<Color>());
            }
        }

        bool texNull = m_terrainTexture == null;
        if (texNull)
        {
            m_terrainTexture = new Texture2D(m_textureSize.x, m_textureSize.y);
        }

        m_terrainTexture.SetPixels(terrainTextureFlatData);
        m_terrainTexture.Apply();
        if (texNull)
        {
            AssetDatabase.CreateAsset(m_terrainTexture, $"Assets/Textures/Terrain.asset");
        }

        AssetDatabase.SaveAssetIfDirty(m_terrainTexture);

        m_terrainMaterial.SetTexture("_BaseMap", m_terrainTexture);
    }

    private void UpdateCityHeight(Vector2Int zone, float[,] heightMap)
    {
        var cityHeightMap = m_cityLayer.GetHeightMap(zone);
        var cityMask = m_cityMask.GetHeightMap(zone);

        for (int y = 0; y < zone.y; y++)
        {
            Action<int> act = (x) =>
            {
                if (cityMask[x, y] >= m_cityMaskTreshold)
                {
                    if (cityHeightMap[x, y] != 0.0f)
                    {
                        heightMap[x, y] += Mathf.Lerp(m_cityHeightRange.x, m_cityHeightRange.y, cityHeightMap[x, y]) * m_cityMaxHeight;
                    }
                }
            };
            Parallel.For(0, zone.x, act);
        }
    }

    private void PaintCity(Vector2Int zone, Color[,] terrainTextureData)
    {
        var cityHeightMap = m_cityLayer.GetHeightMap(zone);
        var cityMask = m_cityMask.GetHeightMap(zone);
        var roadMask = m_m_roadMask.GetHeightMap(zone);

        for (int y = 0; y < zone.y; y++)
        {
            Action<int> act = (x) =>
            {
                if (cityMask[x, y] >= m_cityMaskTreshold)
                {
                    uint buidingID = NoiseUtils.Hash((uint)(roadMask[x, y] * uint.MaxValue));
                    terrainTextureData[x, y] = m_buildingGradient.Evaluate(NoiseUtils.RandomFloat01(ref buidingID));
                }
                if (cityMask[x, y] >= m_roadMaskTreshold)
                {
                    float isNotRoad = roadMask[x, y] != 0.0f ? 1.0f : 0.0f;
                    float roadIntensity = Mathf.InverseLerp(m_roadMaskTreshold, m_cityMaskTreshold, Mathf.Clamp(cityMask[x, y], m_roadMaskTreshold, m_cityMaskTreshold));
                    Color roadColor = Color.Lerp(terrainTextureData[x, y], m_roadColor, roadIntensity);
                    terrainTextureData[x, y] = Color.Lerp(roadColor, terrainTextureData[x, y], isNotRoad);
                }
            };
            Parallel.For(0, zone.x, act);
        }
    }

    private void PaintTerrain(Vector2Int zone, float[,] terrainHeightMap, Color[,] terrainTextureData, TerrainData terrainData)
    {
        for (int y = 0; y < zone.y; y++)
        {
            for (int x = 0; x < zone.x; x++)
            {
                Vector3 normal = terrainData.GetInterpolatedNormal((float)y / zone.y, (float)x / zone.x);
                bool isSteepTerrain = Vector3.Dot(normal, Vector3.up) < m_steepTerrainTreshold;

                float overWaterHeight = terrainHeightMap[x, y] - m_waterLevel;

                Color c;
                if (overWaterHeight < 0.0f)
                {
                    c = m_waterColor;
                }
                else if (isSteepTerrain)
                {
                    c = m_steepTerrainColorAtHeigth.Evaluate(overWaterHeight);
                }
                else
                {
                    c = m_terrainColorAtHeigth.Evaluate(overWaterHeight);
                }
                terrainTextureData[x, y] = c;
            }
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
