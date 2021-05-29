using System.Collections.Generic;
using UnityEngine;

namespace Lunatic.Voxel
{
    public class VoxDatas:ScriptableObject
    {
        public VoxelMap map;
        public VoxPalette palette;


        public List<byte> GetNeighborsOf(Vector3Int position, byte range = 1)
        {
            List<byte> neighbors = new List<byte>();
            byte t1 = GetVoxelAt(position + Vector3Int.right);
            byte t2 = GetVoxelAt(position + Vector3Int.left);
            byte t3 = GetVoxelAt(position + Vector3Int.forward);
            byte t4 = GetVoxelAt(position + Vector3Int.back);
            byte t5 = GetVoxelAt(position + Vector3Int.up);
            byte t6 = GetVoxelAt(position + Vector3Int.down);

            if (t1 > 0)
            {
                if (!neighbors.Contains(t1)) neighbors.Add(t1);
                if (range > 1) neighbors.AddRange(GetNeighborsOf(position + Vector3Int.right, (byte)(range - 1)));
            }
            if (t2 > 0)
            {
                if (!neighbors.Contains(t2)) neighbors.Add(t2);
                if (range > 1) neighbors.AddRange(GetNeighborsOf(position + Vector3Int.left, (byte)(range - 1)));
            }
            if (t3 > 0)
            {
                if (!neighbors.Contains(t3)) neighbors.Add(t3);
                if (range > 1) neighbors.AddRange(GetNeighborsOf(position + Vector3Int.forward, (byte)(range - 1)));
            }
            if (t4 > 0)
            {
                if (!neighbors.Contains(t4)) neighbors.Add(t4);
                if (range > 1) neighbors.AddRange(GetNeighborsOf(position + Vector3Int.back, (byte)(range - 1)));
            }
            if (t5 > 0)
            {
                if (!neighbors.Contains(t5)) neighbors.Add(t5);
                if (range > 1) neighbors.AddRange(GetNeighborsOf(position + Vector3Int.up, (byte)(range - 1)));
            }
            if (t6 > 0)
            {
                if (!neighbors.Contains(t6)) neighbors.Add(t6);
                if (range > 1) neighbors.AddRange(GetNeighborsOf(position + Vector3Int.down, (byte)(range - 1)));
            }


            return neighbors;
        }

        public void GetReachableNeighborsOf(List<Vector3Int> _tilesInRange, Vector3Int position, byte range = 1, byte upStep = 1, byte fallStep = 2)
        {
            Vector3Int? t1 = GetReachableVoxelAt(position, new Vector2Int(position.x + 1, position.z), upStep, fallStep);
            Vector3Int? t2 = GetReachableVoxelAt(position, new Vector2Int(position.x - 1, position.z), upStep, fallStep);
            Vector3Int? t3 = GetReachableVoxelAt(position, new Vector2Int(position.x, position.z + 1), upStep, fallStep);
            Vector3Int? t4 = GetReachableVoxelAt(position, new Vector2Int(position.x, position.z - 1), upStep, fallStep);

            if (t1 != null)
            {
                if (!_tilesInRange.Contains((Vector3Int)t1)) _tilesInRange.Add((Vector3Int)t1);
                if (range > 1) GetReachableNeighborsOf(_tilesInRange, position + Vector3Int.right, (byte)(range - 1), upStep, fallStep);
            }
            if (t2 != null)
            {
                if (!_tilesInRange.Contains((Vector3Int)t2)) _tilesInRange.Add((Vector3Int)t2);
                if (range > 1) GetReachableNeighborsOf(_tilesInRange, position + Vector3Int.left, (byte)(range - 1), upStep, fallStep);
            }
            if (t3 != null)
            {
                if (!_tilesInRange.Contains((Vector3Int)t3)) _tilesInRange.Add((Vector3Int)t3);
                if (range > 1) GetReachableNeighborsOf(_tilesInRange, position + Vector3Int.forward, (byte)(range - 1), upStep, fallStep);
            }
            if (t4 != null)
            {
                if (!_tilesInRange.Contains((Vector3Int)t4)) _tilesInRange.Add((Vector3Int)t4);
                if (range > 1) GetReachableNeighborsOf(_tilesInRange, position + Vector3Int.back, (byte)(range - 1), upStep, fallStep);
            }

            _tilesInRange.Remove(position);
        }

        internal Vector3Int? GetReachableVoxelAt(Vector3 pos, Vector2Int xz, byte upStep = 1, byte fallStep = 2)
        {
            for (int i = -fallStep; i <= upStep; i++)
            {
                //if (grid[xz.x, (int)(pos.y + i), xz.y] > 0)
                if (map.GetVoxel(xz.x, (int)(pos.y + i), xz.y) > 0)
                {
                    // there is a tile
                    if (map.GetVoxel(xz.x, (int)(pos.y + i + 1), xz.y) == 0 && map.GetVoxel(xz.x, (int)(pos.y + i + 2), xz.y) == 0)
                    {
                        // we can walk here
                        return new Vector3Int(xz.x, (int)(pos.y + i), xz.y);
                    }
                }
            }
            return null;
        }

        public byte GetVoxelAt(Vector3Int v)
        {
            return map.GetVoxel(v);
        }

        public Color32 GetVoxelColorAt(Vector3Int v)
        {
            byte c = map.GetVoxel(v);
            return (palette != null) ? palette.colors[c] : new Color32(c, c, c, 255);
        }
    }
}
