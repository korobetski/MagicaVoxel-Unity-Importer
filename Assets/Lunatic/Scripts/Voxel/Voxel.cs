using System;
using UnityEngine;

namespace Lunatic.Voxel
{
     [Serializable]
    public class Voxel
    {
        public Vector3Int pos;
        public byte col;

        public Voxel(byte x, byte y, byte z, byte c)
        {
            pos = new Vector3Int(x, y, z);
            col = c;
        }
    }
}