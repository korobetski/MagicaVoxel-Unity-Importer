using UnityEngine;

namespace Lunatic.Voxel
{
    public class VoxPalette : ScriptableObject
    {
        public enum MaterialType { DIFFUSE, METAL, GLASS, EMIT };


        public Color32[] colors;
        public MaterialType[] matRefs;

        internal Texture2D GetTexture()
        {
            Texture2D paletteTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false);
            paletteTexture.SetPixels32(colors, 0);
            paletteTexture.filterMode = FilterMode.Point;
            paletteTexture.Apply();
            return paletteTexture;
        }
    }

}
