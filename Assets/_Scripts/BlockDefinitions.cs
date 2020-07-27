using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[CreateAssetMenu(fileName = "BlockDefinitions.asset", menuName = "Minecraft Clone/Block Definitions")]
public class BlockDefinitions : ScriptableObject
{
    public List<BlockData> DefinedBlocks;
    private Dictionary<string, RuntimeBlockData> _blockNameMap;

    private NativeHashMap<ushort, RuntimeBlockData> _blocks;

    public NativeHashMap<ushort, RuntimeBlockData> Blocks => _blocks;

    public RuntimeBlockData this[string i] => _blockNameMap[i];
    public RuntimeBlockData this[ushort i] => _blocks[i];

    public void GenerateBlocks()
    {
        _blocks = new NativeHashMap<ushort, RuntimeBlockData>(DefinedBlocks.Count, Allocator.Persistent);
        _blockNameMap = new Dictionary<string, RuntimeBlockData>();
        foreach (var block in DefinedBlocks)
        {
            var rbd = new RuntimeBlockData(block.TextureTop, block.TextureSide,
                block.TextureBottom, block.Solid);
            _blockNameMap[block.Name] = rbd;
            _blocks.Add((ushort) block.BlockId, rbd);
        }
    }
}

public struct RuntimeBlockData
{
    public int TextureTop;
    public int TextureSide;
    public int TextureBottom;
    public bool Solid;

    public RuntimeBlockData(int textureTop, int textureSide, int textureBottom, bool solid)
    {
        TextureTop = textureTop;
        TextureSide = textureSide;
        TextureBottom = textureBottom;
        Solid = solid;
    }

    public int this[int i]
    {
        get
        {
            switch (i)
            {
                case 0:
                case 1:
                case 4:
                case 5:
                    return TextureSide;

                case 2:
                    return TextureTop;

                case 3:
                    return TextureBottom;
            }

            return TextureSide;
        }
    }
}

[Serializable]
public class BlockData
{
    public TileType BlockId;
    public string Name;
    public bool Solid = true;
    public int TextureTop;
    public int TextureSide;
    public int TextureBottom;
}