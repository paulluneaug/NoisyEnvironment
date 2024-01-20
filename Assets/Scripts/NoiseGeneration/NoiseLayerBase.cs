using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NoiseLayerBase : ScriptableObject
{
    public abstract float[,] GetHeightMap(Vector2Int zoneToGenerate);
    public abstract bool Changed(Vector2Int zoneToGenerate);
}
