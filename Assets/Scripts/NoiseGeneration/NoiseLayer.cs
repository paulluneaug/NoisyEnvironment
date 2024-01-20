using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityUtility.CustomAttributes;
using static PerlinNoiseGenerator;
using static VornoiNoiseGenerator;

[CreateAssetMenu(fileName = nameof(NoiseLayer), menuName = "Noise/" + nameof(NoiseLayer))]
public class NoiseLayer : ScriptableObject
{
    public enum NoiseType
    {
        Perlin,
        Vornoi,
    }

    [SerializeField] private NoiseType m_noiseType = NoiseType.Perlin;
    [SerializeField] private int m_noiseScale;
    [SerializeField] private bool m_useSmootherStep;
    [SerializeField] private int m_gradientOffset;
    [SerializeField] private float m_layerWeight;
    [SerializeField] private bool m_inverse;

    [Header("Mask")]
    [SerializeField] private NoiseLayer m_mask;
    [SerializeField] private bool m_inverseMask;
    [SerializeField, MinMaxSlider(0, 1)] private Vector2 m_remapInterval;

    [NonSerialized] private float[,] m_generatedValues;


    [NonSerialized] private Vector2Int m_savedZoneToGenerate;
    [NonSerialized] private NoiseType m_savedNoiseType;
    [NonSerialized] private int m_savedNoiseScale;
    [NonSerialized] private bool m_savedUseSmootherStep;
    [NonSerialized] private int m_savedGradientOffset;
    [NonSerialized] private float m_savedLayerWeight;
    [NonSerialized] private bool m_savedInverse;
    [NonSerialized] private NoiseLayer m_savedMask;
    [NonSerialized] private bool m_savedInverseMask;


    public float[,] Generate(Vector2Int zoneToGenerate)
    {
        if (NeedsFullRegeneration(zoneToGenerate))
        {
            GenerateZone(zoneToGenerate);
        }
        else if (WeightChanged())
        {
            RecalculateWeigth();
        }
        else if (InverseChanged())
        {
            RecalculateInverse();
        }
        return m_generatedValues;
    }

    private bool NeedsFullRegeneration(Vector2Int zoneToGenerate)
    {
        return 
            !(m_savedZoneToGenerate == zoneToGenerate &&
            m_savedNoiseType == m_noiseType &&
            m_savedNoiseScale == m_noiseScale &&
            m_savedUseSmootherStep == m_useSmootherStep &&
            m_savedGradientOffset == m_gradientOffset &&
            m_mask == m_savedMask &&
            m_inverseMask == m_savedMask);
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
                PerlinNoiseLayer[] perlinLayers = new PerlinNoiseLayer[1];
                perlinLayers[0] = new PerlinNoiseLayer(m_layerWeight, m_gradientOffset, m_noiseScale, m_useSmootherStep, m_inverse);
                PerlinNoiseGenerationParameters perlinParameter = new(zoneToGenerate, perlinLayers);
                m_generatedValues = PerlinNoiseGenerator.GenerateZone(perlinParameter);
                break;

            case NoiseType.Vornoi:
                VornoiNoiseLayer[] vornoiLayers = new VornoiNoiseLayer[1];
                vornoiLayers[0] = new VornoiNoiseLayer(m_layerWeight, m_gradientOffset, m_noiseScale, m_useSmootherStep, m_inverse);
                VornoiNoiseGenerationParameters vornoiParameters = new(zoneToGenerate, vornoiLayers);
                m_generatedValues = VornoiNoiseGenerator.GenerateZone(vornoiParameters);
                break;
        }

        if (m_mask != null)
        {
            float[,] maskValues = m_mask.Generate(zoneToGenerate);
            ApplyMask(maskValues);
        }


        m_savedZoneToGenerate = zoneToGenerate;
        m_savedNoiseType = m_noiseType;
        m_savedNoiseScale = m_noiseScale;
        m_savedUseSmootherStep = m_useSmootherStep;
        m_savedGradientOffset = m_gradientOffset;
        m_savedLayerWeight = m_layerWeight;
        m_savedInverse = m_inverse;

        m_savedMask = m_mask;
        m_savedInverseMask = m_inverseMask;
    }

    private bool WeightChanged()
    {
        return m_savedLayerWeight != m_layerWeight;
    }

    private void RecalculateWeigth()
    {
        for (int y = 0; y < m_savedZoneToGenerate.y; y++)
        {
            Parallel.For(0, m_savedZoneToGenerate.x, (x) => m_generatedValues[x, y] *= m_layerWeight / m_savedLayerWeight);
        }
        m_savedLayerWeight = m_layerWeight;
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

}
