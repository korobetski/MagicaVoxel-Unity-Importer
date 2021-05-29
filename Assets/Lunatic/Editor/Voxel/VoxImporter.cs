using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lunatic.Voxel
{
    [ScriptedImporter(150, "vox")]
    public class VoxImporter: ScriptedImporter
    {
        // https://github.com/ephtracy/voxel-model/blob/master/MagicaVoxel-file-format-vox.txt
        // https://github.com/ephtracy/voxel-model/blob/master/MagicaVoxel-file-format-vox-extension.txt

        public bool buildModel = true;
        public bool buildScriptableObjects = true;
        public bool buildPaletteTexture = true;
        public bool buildMaterial = true;



        private int version;
        private int numModels = 0;
        private List<Vector3Int> sizes;
        private int totalVoxels = 0;
        private List<GameObject> _models = new List<GameObject>();
        private Material[] _materials;
        private VoxDict[] _materialsProps;
        private Color32[] _palette;


        public override void OnImportAsset(AssetImportContext ctx)
        {
            sizes = new List<Vector3Int>();
            _models = new List<GameObject>();
            _palette = new Color32[256];
            _materialsProps = new VoxDict[257];
            numModels = 1;
            _materials = new Material[4];
            _materials[0] = Resources.Load<Material>("Materials/_diffuse");
            _materials[1] = Resources.Load<Material>("Materials/_metal");
            _materials[2] = Resources.Load<Material>("Materials/_glass");
            _materials[3] = Resources.Load<Material>("Materials/_emit");

            List<VoxNode> nodes = new List<VoxNode>();




            FileStream fs = File.OpenRead(ctx.assetPath);
            BinaryReader buffer = new BinaryReader(fs);

            char[] voxSign = buffer.ReadChars(4); // should be "VOX "
            string vox = String.Join("", voxSign);
            if (vox != "VOX ")
            {
                return;
            }
            version = buffer.ReadInt32();



            GameObject voxContainer = new GameObject(ctx.assetPath);
            //VoxPalette palette = voxContainer.AddComponent<VoxPalette>();
            if (buildModel)
            {
                ctx.AddObjectToAsset("main obj", voxContainer);
                ctx.SetMainObject(voxContainer);
            }
            totalVoxels = 0;


            VoxChunk mainChunk = new VoxChunk();
            mainChunk.ReadChunkHeader(buffer);
            // SIZE and XYZI chunks should be one after another and represent a single model

            if (mainChunk.chunkChildrenSize > 12)
            {
                long pointer = buffer.BaseStream.Position;
                while (buffer.BaseStream.Position < pointer + mainChunk.chunkChildrenSize)
                {
                    // we check if we have at least 12 bytes to reader a chunk header
                    if (buffer.BaseStream.Position + 12 < buffer.BaseStream.Length)
                    {
                        VoxChunk childChunk = new VoxChunk();
                        childChunk.ReadChunkHeader(buffer);

                        // here we read datas of sub chunks
                        switch (childChunk.chunkId)
                        {
                            case "PACK":
                                // there are several models in the file
                                numModels = buffer.ReadInt32();
                                break;
                            case "SIZE":
                                // model "area"
                                int sizeX = buffer.ReadInt32();
                                int sizeZ = buffer.ReadInt32();
                                int sizeY = buffer.ReadInt32();

                                sizes.Add( new Vector3Int(sizeX, sizeY, sizeZ) );
                                break;
                            case "XYZI":
                                int numVoxels = buffer.ReadInt32();
                                GameObject voxModel = new GameObject(string.Concat("vox_model #", (string)_models.Count.ToString()));
                                //voxModel.transform.parent = voxContainer.transform;
                                VoxModel mod = voxModel.AddComponent<VoxModel>();
                                _models.Add(voxModel);
                                totalVoxels += numVoxels;

                                Vector3Int size = sizes[sizes.Count - 1];
                                byte[,,] grid = new byte[size.x+1, size.y+1, size.z+1]; // filled with 0

                                for (int j = 0; j < numVoxels; j++)
                                {
                                    byte[] xzyi = buffer.ReadBytes(4);
                                    grid[xzyi[0], xzyi[2], xzyi[1]] = xzyi[3];
                                }

                                mod.size = size;
                                mod.grid = grid;
                                break;
                            case "RGBA":
                                // this is a palette Chunk
                                _palette[0] = Color.clear;
                                for (int i = 0; i < 255; i++)
                                {
                                    byte[] rgba = buffer.ReadBytes(4);
                                    _palette[i + 1] = new Color32(rgba[0], rgba[1], rgba[2], rgba[3]);
                                }
                                buffer.ReadBytes(4);
                                //_palette[255] = Color.black;
                                Texture2D paletteTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false);
                                paletteTexture.SetPixels32(_palette, 0);
                                paletteTexture.filterMode = FilterMode.Point;
                                paletteTexture.Apply();
                                if (buildPaletteTexture) ctx.AddObjectToAsset("vox_texture", paletteTexture);
                                _materials[0].mainTexture = paletteTexture;
                                paletteTexture.Apply();
                                if (buildMaterial) ctx.AddObjectToAsset("mat", _materials[0]);
                                //palette.colors = _palette;
                                break;
                            case "MATL":
                                //buffer.ReadBytes(childChunk.chunkSize);
                                int matId = buffer.ReadInt32();
                                VoxDict materialAttributes = new VoxDict();
                                materialAttributes.Read(buffer);
                                _materialsProps[matId] = materialAttributes;
                                break;
                            case "nTRN": // Transform Node Chunk
                                VoxTransformNode nodeTRN = new VoxTransformNode();
                                nodeTRN.Read(buffer);
                                nodes.Add(nodeTRN);
                                break;
                            case "nGRP": // Group Node Chunk
                                VoxGroupNode nodeGRP = new VoxGroupNode();
                                nodeGRP.Read(buffer);
                                nodes.Add(nodeGRP);
                                break;
                            case "nSHP": //  Shape Node Chunk 
                                VoxShapeNode nodeSHP = new VoxShapeNode();
                                nodeSHP.Read(buffer);
                                nodes.Add(nodeSHP);
                                break;
                            case "LAYR": // Layer Chunk 
                                VoxLayerNode nodeLAYR = new VoxLayerNode();
                                nodeLAYR.Read(buffer);
                                nodes.Add(nodeLAYR);
                                break;
                            case "rOBJ":
                                //buffer.ReadBytes(childChunk.chunkSize);
                                VoxDict objAttributes = new VoxDict();
                                objAttributes.Read(buffer);
                                break;
                            case "rCAM":
                                //buffer.ReadBytes(childChunk.chunkSize);
                                int unk = buffer.ReadInt32();
                                VoxDict camAttributes = new VoxDict();
                                camAttributes.Read(buffer);
                                break;
                            case "NOTE":
                                Encoding.UTF8.GetString(buffer.ReadBytes(childChunk.chunkSize));
                                break;
                            default:
                                Debug.Log(string.Concat("childChunk.chunkId : ", childChunk.chunkId));
                                childChunk = null;
                                break;

                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            fs.Close();

            BuildMeshes(ctx);
            if (nodes.Count > 0)
            {
                NodeHierachy(nodes, voxContainer, 0);
            }


            if(buildModel == false) DestroyImmediate(voxContainer);
        }

        private void BuildMeshes(AssetImportContext ctx)
        {
            for (int i = 0; i < sizes.Count; i++)
            {
                List<Vector3> vertices = new List<Vector3>();
                //List<int> triangles = new List<int>();
                List<int>[] subMeshTriangles = new List<int>[4]; // one submesh for each type of material, (ligther than one material for each color index)
                // we will manage unique material properties with propsBlocks and GPU instancing
                List<Vector3> normals = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                int d = vertices.Count;

                Vector3Int size = sizes[i];
                Vector3 pivot = size / 2;
                GameObject voxModel = _models[i];
                byte[,,] grid = voxModel.GetComponent<VoxModel>().grid;

                VoxDatas so = ScriptableObject.CreateInstance<VoxDatas>();
                so.map = new VoxelMap();
                so.map.size = size;
                //so.grid = grid;
                so.palette = ScriptableObject.CreateInstance<VoxPalette>();
                so.palette.colors = _palette;
                so.palette.matRefs = new VoxPalette.MaterialType[256];
                for (int j = 0; j < 256; j++)
                {
                    string matType = "_diffuse";
                    if (_materialsProps[j] != null) matType = _materialsProps[j].GetValue("_type");
                    VoxPalette.MaterialType mat = VoxPalette.MaterialType.DIFFUSE;
                    switch (matType)
                    {
                        case "_diffuse":
                            mat = VoxPalette.MaterialType.DIFFUSE;
                            break;
                        case "_metal":
                            mat = VoxPalette.MaterialType.METAL;
                            break;
                        case "_glass":
                            mat = VoxPalette.MaterialType.GLASS;
                            break;
                        case "_emit":
                            mat = VoxPalette.MaterialType.EMIT;
                            break;
                        default:
                            mat = VoxPalette.MaterialType.DIFFUSE;
                            break;

                    }
                    so.palette.matRefs[j] = mat;
                }
                so.name = string.Concat(voxModel.name, " datas");
                so.palette.name = string.Concat(voxModel.name, " palette");

                for (byte x = 0; x < size.x; x++)
                {
                    for (byte y = 0; y < size.y; y++)
                    {
                        for (byte z = 0; z < size.z; z++)
                        {
                            byte c = grid[x, y, z];


                            string matType = "_diffuse";
                            if (_materialsProps[c] != null) matType = _materialsProps[c].GetValue("_type");
                            byte matIndex = 0;
                            switch(matType)
                            {
                                case "_diffuse":
                                    matIndex = 0;
                                    break;
                                case "_metal":
                                    matIndex = 1;
                                    break;
                                case "_glass":
                                    matIndex = 2;
                                    break;
                                case "_emit":
                                    matIndex = 3;
                                    break;
                                default:
                                    matIndex = 0;
                                    break;

                            }


                            Vector2[] voxeluvs = new Vector2[4] {
                                                            new Vector2((c + 0.5f) / 256, 0.5f),
                                                            new Vector2((c + 0.5f) / 256, 0.5f),
                                                            new Vector2((c + 0.5f) / 256, 0.5f),
                                                            new Vector2((c + 0.5f) / 256, 0.5f),
                                                        };
                            if (subMeshTriangles[matIndex] == null) subMeshTriangles[matIndex] = new List<int>();
                            if (c > 0)
                            {
                                so.map.v.Add(new Voxel(x, y, z, c));
                                // there is a voxel here
                                // so we will check each 6 orientations to see if we need to draw the corresponding face
                                if (y == 0 || grid[x, y - 1, z] == 0)
                                {
                                    // we need bottom face
                                    d = vertices.Count;
                                    vertices.AddRange(new Vector3[4]{
                                                            new Vector3(x+0, y+0, z+0) - pivot,
                                                            new Vector3(x+1, y+0, z+0) - pivot,
                                                            new Vector3(x+0, y+0, z+1) - pivot,
                                                            new Vector3(x+1, y+0, z+1) - pivot,
                                                        });
                                    //triangles.AddRange(indices);
                                    subMeshTriangles[matIndex].AddRange(new int[6] { d + 0, d + 1, d + 2, d + 3, d + 2, d + 1 });
                                    normals.AddRange(new Vector3[4]
                                    {
                                                            Vector3.down,
                                                            Vector3.down,
                                                            Vector3.down,
                                                            Vector3.down,
                                    });
                                    uv.AddRange(voxeluvs);
                                }

                                if (y == 255 || grid[x, y + 1, z] == 0)
                                {
                                    // we need up face
                                    d = vertices.Count;
                                    vertices.AddRange(new Vector3[4]{
                                                            new Vector3(x+0, y+1, z+1) - pivot,
                                                            new Vector3(x+1, y+1, z+1) - pivot,
                                                            new Vector3(x+0, y+1, z+0) - pivot,
                                                            new Vector3(x+1, y+1, z+0) - pivot,
                                                        });
                                    //triangles.AddRange(indices);
                                    subMeshTriangles[matIndex].AddRange(new int[6] { d + 0, d + 1, d + 2, d + 3, d + 2, d + 1 });
                                    normals.AddRange(new Vector3[4]
                                    {
                                                            Vector3.up,
                                                            Vector3.up,
                                                            Vector3.up,
                                                            Vector3.up,
                                    });
                                    uv.AddRange(voxeluvs);
                                }

                                if (x == 255 || grid[x + 1, y, z] == 0)
                                {
                                    // we need right face
                                    d = vertices.Count;
                                    vertices.AddRange(new Vector3[4]{
                                                            new Vector3(x+1, y+1, z+0) - pivot,
                                                            new Vector3(x+1, y+1, z+1) - pivot,
                                                            new Vector3(x+1, y+0, z+0) - pivot,
                                                            new Vector3(x+1, y+0, z+1) - pivot,
                                                        });
                                    //triangles.AddRange(indices);
                                    subMeshTriangles[matIndex].AddRange(new int[6] { d + 0, d + 1, d + 2, d + 3, d + 2, d + 1 });
                                    normals.AddRange(new Vector3[4]
                                    {
                                                            Vector3.right,
                                                            Vector3.right,
                                                            Vector3.right,
                                                            Vector3.right,
                                    });
                                    uv.AddRange(voxeluvs);
                                }

                                if (x == 0 || grid[x - 1, y, z] == 0)
                                {
                                    // we need left face
                                    d = vertices.Count;
                                    vertices.AddRange(new Vector3[4]{
                                                            new Vector3(x+0, y+0, z+0) - pivot,
                                                            new Vector3(x+0, y+0, z+1) - pivot,
                                                            new Vector3(x+0, y+1, z+0) - pivot,
                                                            new Vector3(x+0, y+1, z+1) - pivot,
                                                        });
                                    //triangles.AddRange(indices);
                                    subMeshTriangles[matIndex].AddRange(new int[6] { d + 0, d + 1, d + 2, d + 3, d + 2, d + 1 });
                                    normals.AddRange(new Vector3[4]
                                    {
                                                            Vector3.left,
                                                            Vector3.left,
                                                            Vector3.left,
                                                            Vector3.left,
                                    });
                                    uv.AddRange(voxeluvs);
                                }

                                if (z == 255 || grid[x, y, z + 1] == 0)
                                {
                                    // we need forward face
                                    d = vertices.Count;
                                    vertices.AddRange(new Vector3[4]{
                                                            new Vector3(x+0, y+0, z+1) - pivot,
                                                            new Vector3(x+1, y+0, z+1) - pivot,
                                                            new Vector3(x+0, y+1, z+1) - pivot,
                                                            new Vector3(x+1, y+1, z+1) - pivot,
                                                        });
                                    //triangles.AddRange(indices);
                                    subMeshTriangles[matIndex].AddRange(new int[6] { d + 0, d + 1, d + 2, d + 3, d + 2, d + 1 });
                                    normals.AddRange(new Vector3[4]
                                    {
                                                            Vector3.forward,
                                                            Vector3.forward,
                                                            Vector3.forward,
                                                            Vector3.forward,
                                    });
                                    uv.AddRange(voxeluvs);
                                }

                                if (z == 0 || grid[x, y, z - 1] == 0)
                                {
                                    // we need back face
                                    d = vertices.Count;
                                    vertices.AddRange(new Vector3[4]{
                                                            new Vector3(x+0, y+1, z+0) - pivot,
                                                            new Vector3(x+1, y+1, z+0) - pivot,
                                                            new Vector3(x+0, y+0, z+0) - pivot,
                                                            new Vector3(x+1, y+0, z+0) - pivot,
                                                        });
                                    //triangles.AddRange(indices);
                                    subMeshTriangles[matIndex].AddRange(new int[6] { d + 0, d + 1, d + 2, d + 3, d + 2, d + 1 });
                                    normals.AddRange(new Vector3[4]
                                    {
                                                            Vector3.back,
                                                            Vector3.back,
                                                            Vector3.back,
                                                            Vector3.back,
                                    });
                                    uv.AddRange(voxeluvs);
                                }


                            }

                        }
                    }
                }


                Mesh modelMesh = new Mesh();


                var layout = new[]
                {
                                        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
                                        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0),
                                        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 0),
                                    };
                var vertexCount = vertices.Count;
                if (vertexCount > UInt16.MaxValue) modelMesh.indexFormat = IndexFormat.UInt32;
                else modelMesh.indexFormat = IndexFormat.UInt16;
                modelMesh.name = string.Concat(ctx.assetPath, "_mesh#", _models.Count);
                modelMesh.vertices = vertices.ToArray();
                //modelMesh.triangles = triangles.ToArray();

                byte subMeshes = 0;
                modelMesh.subMeshCount = 4;
                List<Material> actifMats = new List<Material>();
                for (int j = 0; j < 4; j++)
                {
                    if (subMeshTriangles[j] != null)
                    {
                        modelMesh.SetTriangles(subMeshTriangles[j].ToArray(), subMeshes);
                        actifMats.Add(_materials[j]);
                        subMeshes++;
                    }
                }
                modelMesh.subMeshCount = subMeshes;


                modelMesh.normals = normals.ToArray();
                modelMesh.uv = uv.ToArray();
                modelMesh.SetVertexBufferParams(vertexCount, layout);

                if (buildModel) ctx.AddObjectToAsset(string.Concat("mesh ", voxModel.name), modelMesh);
                if (buildScriptableObjects)
                {
                    ctx.AddObjectToAsset(string.Concat(Utils.Toolbox.NameFromPath(ctx.assetPath), " datas"), so);
                    ctx.AddObjectToAsset(string.Concat(Utils.Toolbox.NameFromPath(ctx.assetPath), " palette"), so.palette);
                }

                MeshFilter mf = voxModel.AddComponent<MeshFilter>();
                mf.mesh = modelMesh;
                MeshRenderer mr = voxModel.AddComponent<MeshRenderer>();
                mr.materials = actifMats.ToArray();

            }
        }

        private void NodeHierachy(List<VoxNode> nodes, GameObject rootGo, int v)
        {
            string nodeName = string.Concat(nodes[v].GetAttribute("_name"), " #", v);
            if (nodes[v].GetAttribute("_name") == null) nodeName = string.Concat(nodes[v].GetType().Name," #", v);


            GameObject go = new GameObject(nodeName);
            go.transform.parent = rootGo.transform;
            go.transform.localPosition = Vector3Int.zero;

            switch (nodes[v].GetType().Name)
            {
                case nameof(VoxTransformNode):
                    VoxTransformNode nodeTRN = nodes[v] as VoxTransformNode;
                    if (nodeTRN.numFrames > 0)
                    {
                        string nodeTranslation = nodeTRN.pose.GetValue("_t");
                        string nodeRotation = nodeTRN.pose.GetValue("_r");

                        if (nodeTranslation != null)
                        {
                            //Debug.LogWarning(nodeTranslation);
                            // we should apply translation
                            string[] xzy = nodeTranslation.Split(' '); // X Z Y
                            go.transform.localPosition = new Vector3Int(int.Parse(xzy[0], System.Globalization.NumberStyles.Integer), int.Parse(xzy[2], System.Globalization.NumberStyles.Integer), int.Parse(xzy[1], System.Globalization.NumberStyles.Integer));
                        }
                        if (nodeRotation != null)
                        {
                            // we should apply rotation
                        }

                    }

                    NodeHierachy(nodes, go, nodeTRN.childNodeId);
                    break;
                case nameof(VoxGroupNode):
                    VoxGroupNode nodeGRP = nodes[v] as VoxGroupNode;
                    for (int i = 0; i < nodeGRP.numChildren; i++)
                    {
                        NodeHierachy(nodes, go, nodeGRP.children[i]);
                    }
                    break;
                case nameof(VoxShapeNode):
                    VoxShapeNode nodeSHP = nodes[v] as VoxShapeNode;
                    for (int i = 0; i < nodeSHP.numModels; i++)
                    {
                        // maybe we should check modelAttrs as well (nodeSHP.modelRefs[i].Value)
                        _models[nodeSHP.modelRefs[i].Key].transform.parent = go.transform;
                        _models[nodeSHP.modelRefs[i].Key].transform.localPosition = Vector3Int.zero;
                        //_models[nodeSHP.modelRefs[i].Key].transform.localPosition = -sizes[nodeSHP.modelRefs[i].Key] / 2;
                    }
                    break;
            }
        }
    }

    internal class VoxChunk
    {
        public string chunkId;
        public int chunkSize;
        public int chunkChildrenSize;

        public void ReadChunkHeader(BinaryReader buffer)
        {
            char[] chars = buffer.ReadChars(4);
            //Debug.Log(string.Concat("chars : ", chars[0], chars[1], chars[2], chars[3]));
            chunkId = String.Join("", chars);
            chunkSize = buffer.ReadInt32();
            chunkChildrenSize = buffer.ReadInt32();
        }
    }

    internal class VoxNode
    {
        public int id;
        public VoxDict attributes;

        public virtual void Read(BinaryReader buffer)
        {
            id = buffer.ReadInt32();
            attributes = new VoxDict();
            attributes.Read(buffer);
        }

        public string GetAttribute(string key)
        {
            return attributes.GetValue(key);
        }
    }

    internal class VoxTransformNode:VoxNode
    {
        public int childNodeId;
        public int reservedId; 
        public int layerId;
        public int numFrames;
        public VoxDict pose;
        public VoxDict[] frames;

        public override void Read(BinaryReader buffer)
        {
            base.Read(buffer);

            childNodeId = buffer.ReadInt32();
            reservedId = buffer.ReadInt32(); // (must be - 1)
            layerId = buffer.ReadInt32();
            numFrames = buffer.ReadInt32(); //  (must be 1)
            VoxDict[] frames = new VoxDict[numFrames];
            for (int i = 0; i < numFrames; i++)
            {
                frames[i] = new VoxDict();
                frames[i].Read(buffer);
            }
            pose = frames[0];
        }
    }

    internal class VoxGroupNode : VoxNode
    {
        public int numChildren;
        public int[] children;

        public override void Read(BinaryReader buffer)
        {
            base.Read(buffer);
            numChildren = buffer.ReadInt32();
            children = new int[numChildren];
            for (int i = 0; i < numChildren; i++)
            {
                children[i] = buffer.ReadInt32();
            }
        }
    }

    internal class VoxShapeNode : VoxNode
    {
        public int numModels; // must be one
        public KeyValuePair<int, VoxDict>[] modelRefs;

        public override void Read(BinaryReader buffer)
        {
            base.Read(buffer);

            numModels = buffer.ReadInt32(); // must be one
            modelRefs = new KeyValuePair<int, VoxDict>[numModels];
            for (int i = 0; i < numModels; i++)
            {
                int modelId = buffer.ReadInt32();
                VoxDict modelAttrs = new VoxDict();
                modelAttrs.Read(buffer);
                modelRefs[i] = new KeyValuePair<int, VoxDict>(modelId, modelAttrs);
            }
        }
}

    internal class VoxLayerNode : VoxNode
    {
        public int reservedId;

        public override void Read(BinaryReader buffer)
        {
            base.Read(buffer);
            reservedId = buffer.ReadInt32();
        }
    }

    internal class VoxString
    {
        public int size;
        public string value;
        public void Read(BinaryReader buffer)
        {
            size = buffer.ReadInt32();
            value = Encoding.UTF8.GetString(buffer.ReadBytes(size));
        }

        public override string ToString()
        {
            return value;
        }
    }

    internal class VoxDict
    {
        public int count;
        public KeyValuePair<VoxString, VoxString>[] ionary;

        public void Read(BinaryReader buffer)
        {
            count = buffer.ReadInt32();
            ionary = new KeyValuePair<VoxString, VoxString>[count];
            for (int i = 0; i < count; i++)
            {
                VoxString key = new VoxString();
                key.Read(buffer);
                VoxString value = new VoxString();
                value.Read(buffer);
                //Debug.Log(string.Concat("key : ", key.ToString(), "  - value : ", value.ToString()));
                ionary[i] = (new KeyValuePair<VoxString, VoxString>(key, value));
            }
        }

        internal string GetValue(string key)
        {
            for (int i = 0; i < count; i++)
            {
                //Debug.LogWarning(string.Concat("GetValue : ", ionary[i].Key.ToString(),  "  -  ", key));
                if (ionary[i].Key.ToString() == key)
                {
                    return ionary[i].Value.ToString();
                }
            }

            return null;
        }

        public override string ToString()
        {
            string output = "";
            for (int i = 0; i < count; i++)
            {
                output = string.Concat(output, ionary[i].Key.ToString(), " = ", ionary[i].Value.ToString(), "\r\n");
            }
            return output;
        }
    }



    [CustomEditor(typeof(VoxImporter))]
    public class VoxImporterEditor : ScriptedImporterEditor
    {
        // bool importPalette ?
        // bool importMaterials ?
        // bool buildModel ?
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildModel"), new GUIContent("Build 3D Models ?"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildPaletteTexture"), new GUIContent("Build a palette Texture ?"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildMaterial"), new GUIContent("Instantiate Materials ?"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildScriptableObjects"), new GUIContent("Build Voxel Datas ?"));
            // Apply the changes so Undo/Redo is working.
            serializedObject.ApplyModifiedProperties();

            base.ApplyRevertGUI();
        }
    }
}
