using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lunatic.Voxel
{
    [Serializable]
    public class VoxelMap
    {
        public Vector3Int size;
        public List<Voxel> v;


        public VoxelMap()
        {
            v = new List<Voxel>();
        }


        internal byte GetVoxel(Vector3Int pos)
        {
            for (int i = 0; i < v.Count; i++)
            {
                if (v[i].pos == pos)
                {
                    return v[i].col;
                }
            }

            return 0;
        }

        internal byte GetVoxel(int x, int y, int z)
        {
            Vector3Int v3 = new Vector3Int(x, y, z);
            for (int i = 0; i < v.Count; i++)
            {
                if (v[i].pos == v3)
                {
                    return v[i].col;
                }
            }

            return 0;
        }

        internal byte[,,] ToGrid()
        {
            byte[,,] grid = new byte[size.x, size.y, size.z];
            for (int i = 0; i < v.Count; i++)
            {
                grid[v[i].pos.x, v[i].pos.y, v[i].pos.z] = v[i].col;
            }
            return grid;
        }
    }
}