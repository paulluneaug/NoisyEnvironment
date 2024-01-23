using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityUtility.CustomAttributes;
using static PerlinNoiseGenerator;
using static VornoiNoiseGenerator;

[CreateAssetMenu(fileName = nameof(NoiseLayer), menuName = "Noise/" + nameof(NoiseLayer))]
public class NoiseLayer : NoiseLayerBase
{
    public enum NoiseType
    {
        Perlin,
        Vornoi,
    }

    private bool IsVornoi => m_noiseType == NoiseType.Vornoi;
    private bool VornoiAndMarkSeams => IsVornoi && m_markSeams;
    [Button(nameof(ShowLayerAsTexture))]

    [SerializeField] private bool m_a = false;
    [SerializeField] private bool m_forceRecalculate = false;

    [SerializeField] private NoiseType m_noiseType = NoiseType.Perlin;
    [SerializeField] private int m_noiseScale;
    [SerializeField] private bool m_useSmootherStep;
    [SerializeField] private int m_gradientOffset;
    [SerializeField] private bool m_inverse;
    [SerializeField] private float m_pow = 1.0f;
    [SerializeField] private float m_mul = 1.0f;
    [SerializeField] private float m_offset = 1.0f;

    [ShowIf(nameof(IsVornoi))]
    [SerializeField] private int m_order;
    [ShowIf(nameof(IsVornoi))]
    [SerializeField] private bool m_markSeams;
    [ShowIf(nameof(VornoiAndMarkSeams))]
    [SerializeField] private float m_seamWidth;
    [ShowIf(nameof(IsVornoi))]
    [SerializeField] private bool m_sameCellSameValue;

    [Header("Mask")]
    [SerializeField] private NoiseLayerBase m_mask;
    [SerializeField] private bool m_inverseMask;
    [SerializeField, MinMaxSlider(0, 1)] private Vector2 m_remapInterval;

    [NonSerialized] private float[,] m_generatedValues;


    [SerializeField] private Vector2Int m_savedZoneToGenerate;
    [SerializeField, HideInInspector] private NoiseType m_savedNoiseType;
    [SerializeField, HideInInspector] private int m_savedNoiseScale;
    [SerializeField, HideInInspector] private bool m_savedUseSmootherStep;
    [SerializeField, HideInInspector] private int m_savedGradientOffset;
    [SerializeField, HideInInspector] private float m_savedLayerWeight;
    [SerializeField, HideInInspector] private bool m_savedInverse;
    [SerializeField, HideInInspector] private NoiseLayerBase m_savedMask;
    [SerializeField, HideInInspector] private bool m_savedInverseMask;
    [SerializeField, HideInInspector] private Vector2 m_savedRemapInterval;
    [SerializeField, HideInInspector] private int m_savedOrder;
    [SerializeField, HideInInspector] private bool m_savedMarkSeam;
    [SerializeField, HideInInspector] private float m_savedSeamWidth;
    [SerializeField, HideInInspector] private bool m_savedSameCellSameValue;
    [SerializeField, HideInInspector] private float m_savedPow;

    [SerializeField] private Texture2D m_texture;


    public override float[,] GetHeightMap(Vector2Int zoneToGenerate)
    {
        if (NeedsFullRegeneration(zoneToGenerate))
        {
            GenerateZone(zoneToGenerate);
        }
        else if (InverseChanged())
        {
            RecalculateInverse();
        }
        return m_generatedValues;
    }

    public override bool Changed(Vector2Int zoneToGenerate)
    {
        return NeedsFullRegeneration(zoneToGenerate) || InverseChanged();
    }

    private bool NeedsFullRegeneration(Vector2Int zoneToGenerate)
    {
        return true;
        if (m_forceRecalculate || m_generatedValues == null) { return true; }
        return 
            !(m_savedZoneToGenerate == zoneToGenerate &&
            m_savedNoiseType == m_noiseType &&
            m_savedNoiseScale == m_noiseScale &&
            m_savedUseSmootherStep == m_useSmootherStep &&
            m_savedGradientOffset == m_gradientOffset &&
            m_mask == m_savedMask &&
            m_inverseMask == m_savedInverseMask &&
            m_savedRemapInterval == m_remapInterval &&
            m_savedOrder == m_order &&
            m_savedMarkSeam == m_markSeams &&
            m_savedSeamWidth == m_seamWidth &&
            m_savedSameCellSameValue == m_sameCellSameValue &&
            m_savedPow == m_pow);
    }

    private void GenerateZone(Vector2Int zoneToGenerate)
    {
        if (m_mask == this)
        {
            throw new ArgumentException("Invalid Mask");
        }

        switch (m_noiseType)
        {
            case NoiseType.Perlin:
                PerlinNoiseLayer perlinLayer = new PerlinNoiseLayer(m_gradientOffset, m_noiseScale, m_useSmootherStep, m_inverse, m_pow, m_mul, m_offset);
                PerlinNoiseGenerationParameters perlinParameter = new(zoneToGenerate, perlinLayer);
                m_generatedValues = PerlinNoiseGenerator.GenerateZone(perlinParameter);
                break;

            case NoiseType.Vornoi:
                VornoiNoiseLayer vornoiLayer = new VornoiNoiseLayer(m_order, m_gradientOffset, m_noiseScale, m_useSmootherStep, m_inverse, m_markSeams, m_seamWidth, m_sameCellSameValue, m_pow, m_mul, m_offset);
                VornoiNoiseGenerationParameters vornoiParameters = new(zoneToGenerate, vornoiLayer);
                m_generatedValues = VornoiNoiseGenerator.GenerateZone(vornoiParameters);
                break;
        }

        if (m_mask != null)
        {
            float[,] maskValues = m_mask.GetHeightMap(zoneToGenerate);
            ApplyMask(maskValues);
        }

        m_forceRecalculate = false;

        m_savedZoneToGenerate = zoneToGenerate;
        m_savedNoiseType = m_noiseType;
        m_savedNoiseScale = m_noiseScale;
        m_savedUseSmootherStep = m_useSmootherStep;
        m_savedGradientOffset = m_gradientOffset;
        m_savedInverse = m_inverse;
        m_savedRemapInterval = m_remapInterval;
        m_savedPow = m_pow;

        m_savedOrder = m_order;
        m_savedMarkSeam = m_markSeams;
        m_savedSeamWidth = m_seamWidth;
        m_savedSameCellSameValue = m_sameCellSameValue;

        m_savedMask = m_mask;                  
        m_savedInverseMask = m_inverseMask;
    }

    private bool InverseChanged()
    {
        return m_savedInverse ^ m_inverse;
    }

    private void RecalculateInverse()
    {
        for (int y = 0; y < m_savedZoneToGenerate.y; y++)
        {
            Parallel.For(0, m_savedZoneToGenerate.x, (x) => m_generatedValues[x, y] = 1 - m_generatedValues[x, y]);
        }
        m_savedInverse = m_inverse;
    }

    private void ApplyMask(float[,] mask)
    {
        for (int y = 0; y < m_savedZoneToGenerate.y; y++)
        {
            Parallel.For(0, m_savedZoneToGenerate.x, (x) => m_generatedValues[x, y] *= GetMaskValue(x, y, mask));
        }
    }

    private float GetMaskValue(int x, int y, float[,] mask)
    {
        float maskValue = mask[x, y];
        maskValue = Mathf.Clamp01(Mathf.InverseLerp(m_remapInterval.x, m_remapInterval.y, maskValue));
        return m_inverseMask ? 1 - maskValue : maskValue;
    }

    private void ShowLayerAsTexture()
    {
        m_texture = new Texture2D(m_savedZoneToGenerate.x, m_savedZoneToGenerate.y, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);

        float[] flatDatas = new float[m_savedZoneToGenerate.x * m_savedZoneToGenerate.y];
        Buffer.BlockCopy(GetHeightMap(m_savedZoneToGenerate), 0, flatDatas, 0, m_savedZoneToGenerate.x * m_savedZoneToGenerate.y * sizeof(float));
        m_texture.SetPixels(flatDatas.Select(f => new Color(f, f, f, 1)).ToArray());

        AssetDatabase.CreateAsset(m_texture, $"Assets/NoiseLayersTextures/Layer_{name}.asset");
        AssetDatabase.SaveAssetIfDirty(m_texture);
    }
}
