using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Diagnostics;
using static NoiseUtils;

public static class VornoiNoiseGenerator
{
    public class VornoiNoiseGenerationParameters
    {
        public Vector2Int ZoneToGenerate;
        public VornoiNoiseLayer Layer;

        public VornoiNoiseGenerationParameters(Vector2Int zoneToGenerate, VornoiNoiseLayer layer)
        {
            ZoneToGenerate = zoneToGenerate;
            Layer = layer;
        }
    }

    public struct VornoiNoiseLayer
    {
        public int Order;
        public int GradientOffset;
        public int NoiseScale;

        public float Pow;
        public float Mul;
        public float Offset;

        public bool UseSmootherStep;
        public bool Inverse;
        public bool MarkSeams;
        public float SeamsWidth;

        public bool SameCellSameValue;

        public VornoiNoiseLayer(int order, int gradientOffset, int noiseScale, bool useSmootherStep, bool inverse, bool markSeams, float seamsWidth, bool sameCellSameValue, float pow, float mul, float offset)
        {
            Order = order;
            GradientOffset = gradientOffset;
            NoiseScale = noiseScale;
            UseSmootherStep = useSmootherStep;
            Inverse = inverse;
            MarkSeams = markSeams;
            SeamsWidth = seamsWidth;
            SameCellSameValue = sameCellSameValue;
            Pow = pow;
            Mul = mul;
            Offset = offset;
        }
    }

    public static float[,] GenerateZone(VornoiNoiseGenerationParameters parameters)
    {
        Vector2Int zoneToGenerate = parameters.ZoneToGenerate;
        float[,] result = new float[zoneToGenerate.x, zoneToGenerate.y];

        Parallel.For(0, zoneToGenerate.x * zoneToGenerate.y, i => GetNoiseValue(i, zoneToGenerate.x, parameters, ref result));

        return result;
    }

    public static void GetNoiseValue(int index, int xSize, VornoiNoiseGenerationParameters parameters, ref float[,] result)
    {
        int ix = index % xSize;
        int iy = index / xSize;

        float x = (float)ix / parameters.Layer.NoiseScale;
        float y = (float)iy / parameters.Layer.NoiseScale;

        float2 position = new float2(x, y);

        // Determine grid cell coordinates
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);

        float layerValue;
        uint cellSeed;

        (float, uint)[] dists = new (float, uint)[9];

        for (int jx = -1;  jx <= 1; ++jx) 
        {
            for (int jy = -1; jy <= 1; ++jy)
            {
                cellSeed = GetCellSeed2D(x0 + jx, y0 + jy, parameters.Layer.GradientOffset);
                dists[(jx + 1) * 3 + (jy + 1)] = (SqrMagnitude(GetCellPointCoordinates(x0 + jx, y0 + jy, cellSeed) - position), cellSeed);
            }
        }

        Array.Sort(dists, (a, b) => a.Item1.CompareTo(b.Item1));


        if (parameters.Layer.SameCellSameValue)
        {
            layerValue = RandomFloat01(ref dists[parameters.Layer.Order].Item2);
        }
        else
        {
            layerValue = dists[parameters.Layer.Order].Item1;
            layerValue /= (parameters.Layer.Order == 0 ? 2 : 4);// Mathf.Sqrt(minSqrDist);
        }

        if (parameters.Layer.MarkSeams)
        {
            bool isOnSeam = false;
            if (parameters.Layer.Order > 0)
            {
                if (Mathf.Abs(dists[parameters.Layer.Order - 1].Item1 - dists[parameters.Layer.Order].Item1) < parameters.Layer.SeamsWidth)
                {
                    isOnSeam = true;
                }
            }
            if (!isOnSeam && parameters.Layer.Order < 8)
            {
                if (Mathf.Abs(dists[parameters.Layer.Order + 1].Item1 - dists[parameters.Layer.Order].Item1) < parameters.Layer.SeamsWidth)
                {
                    isOnSeam = true;
                }
            }
            layerValue = isOnSeam ? 0.0f : layerValue;
        }


        if (parameters.Layer.Inverse)
        {
            layerValue = 1.0f - layerValue;
        }

        layerValue = Mathf.Pow(layerValue, parameters.Layer.Pow);
        layerValue *= parameters.Layer.Mul;
        layerValue += parameters.Layer.Offset;

        result[ix, iy] = layerValue;
    }

    private static float SqrMagnitude(float2 v)
    {
        return v.x * v.x + v.y * v.y;
    }

    private static float2 GetCellPointCoordinates(int cellX, int cellY, uint cellSeed)
    {
        return new float2(cellX + RandomFloat01(ref cellSeed), cellY + RandomFloat01(ref cellSeed));
    }
}
