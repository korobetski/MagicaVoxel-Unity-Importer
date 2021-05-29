using System;
using UnityEngine;

namespace Lunatic.Voxel
{
    [Serializable]
    public class VoxModel : MonoBehaviour
    {
        public Vector3Int size;
        public byte[,,] grid;
    }
}