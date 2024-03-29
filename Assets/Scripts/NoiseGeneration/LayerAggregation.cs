using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityUtility.CustomAttributes;

[CreateAssetMenu(fileName = nameof(LayerAggregation), menuName = "Noise/" + nameof(LayerAggregation))]
public class LayerAggregation : NoiseLayerBase
{
    [Serializable]
    private class LayerAndWeigth
    {
        public NoiseLayerBase Layer;
        public float Weight;

        public void ApplyLayer(float[,] heightMap, float layerWeightMultiplier, Vector2Int zone)
        {
            float[,] layerMap = Layer.GetHeightMap(zone);
            float weight = layerWeightMultiplier * Weight;
            for (int y = 0; y < zone.y; y++)
            {
                Parallel.For(0, zone.x, (x) => ApplyLayerAtCoordinates(x, y, heightMap, layerMap, weight));
            }
        }

        private static void ApplyLayerAtCoordinates(int x, int y, float[,] heightMap, float[,] layerMap, float weight)
        {
            heightMap[x, y] += layerMap[x, y] * weight;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) { return false; };
            if (obj is not LayerAndWeigth other) { return false; }
            return other.Layer == Layer && other.Weight == Weight;
        }

        public LayerAndWeigth Clone()
        {
            return new LayerAndWeigth()
            {
                Layer = Layer,
                Weight = Weight
            };
        }
    }

    [Button(nameof(ShowLayerAsTexture))]
    [SerializeField] private bool _;

    [SerializeField] private LayerAndWeigth[] m_layers = null;
    [SerializeField] private float[,] m_savedMap = null;

    [SerializeField, HideInInspector] private LayerAndWeigth[] m_savedLayers = null;
    [SerializeField, HideInInspector] private Vector2Int m_savedZoneToGenerate;
    [SerializeField] private Texture2D m_texture;

    public override float[,] GetHeightMap(Vector2Int zoneToGenerate)
    {
        if (m_savedZoneToGenerate != zoneToGenerate || Changed(zoneToGenerate))
        {
            m_savedMap = new float[zoneToGenerate.x, zoneToGenerate.y];

            float weightSum = 0.0f;
            foreach (var layer in m_layers)
            {
                weightSum += layer.Weight;
            }
            foreach (var layer in m_layers)
            {
                layer.ApplyLayer(m_savedMap, 1.0f / weightSum, zoneToGenerate);
            }

            m_savedLayers = m_layers.Select(l => l.Clone()).ToArray();
        }
        m_savedZoneToGenerate = zoneToGenerate;
        return m_savedMap;
    }

    public override bool Changed(Vector2Int zoneToGenerate)
    {
        if (m_savedMap == null) { return true; }

        if (m_layers.Length != m_savedLayers.Length) { return true; }

        for (int i = 0; i < m_layers.Length; i++)
        {
            if (!m_layers[i].Equals(m_savedLayers[i])) { return true; }
        }
        foreach (var layer in m_layers)
        {
            if (layer.Layer.Changed(zoneToGenerate))
            {
                return true;
            }
        }
        return false;
    }

    private void ShowLayerAsTexture()
    {
        bool texNull = false;
        if (m_texture == null)
        {
            texNull = true;
            m_texture = new Texture2D(m_savedZoneToGenerate.x, m_savedZoneToGenerate.y, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);
        }

        float[] flatDatas = new float[m_savedZoneToGenerate.x * m_savedZoneToGenerate.y];
        Buffer.BlockCopy(GetHeightMap(m_savedZoneToGenerate), 0, flatDatas, 0, m_savedZoneToGenerate.x * m_savedZoneToGenerate.y * sizeof(float));
        m_texture.SetPixels(flatDatas.Select(f => new Color(f, f, f, 1)).ToArray());

        if (texNull)
        {
            AssetDatabase.CreateAsset(m_texture, $"Assets/NoiseLayersTextures/Layer_{name}.asset");
        }
        AssetDatabase.SaveAssetIfDirty(m_texture);
    }
}
