using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[BurstCompile]
struct TerrainGenJob : IJob, IDisposable
{
    private struct VertData
    {
        public VertData(float3 position, float2 uv)
        {
            Position = position;
            UV = uv;
        }

        public float3 Position;
        public float2 UV;
    }

    [ReadOnly]
    public int2 ChunkId;

    [ReadOnly]
    public NativeArray<int3> DirectionChecks;

    [ReadOnly]
    public NativeArray<float3> VoxelVerts;

    [ReadOnly]
    public NativeArray<int> VoxelTris;

    [ReadOnly]
    public NativeArray<float2> VoxelUvs;

    public Mesh.MeshDataArray meshData;
    private NativeList<VertData> vertData;
    private NativeList<int> Triangles;
    private int VertexOffset;

    public TerrainGenJob(int2 _chunkId,
        NativeArray<int3> directionChecks,
        NativeArray<float3> voxelVerts,
        NativeArray<int> voxelTris,
        NativeArray<float2> voxelUvs)
    {
        ChunkId = _chunkId;

        meshData = Mesh.AllocateWritableMeshData(1);

        vertData = new NativeList<VertData>(Allocator.TempJob);
        Triangles = new NativeList<int>(Allocator.TempJob);

        DirectionChecks = directionChecks;
        VoxelVerts = voxelVerts;
        VoxelTris = voxelTris;
        VoxelUvs = voxelUvs;

        VertexOffset = 0;
    }


    public void Execute()
    {
        for (var x = 0; x < VoxelData.ChunkWidth; x++)
        for (var z = 0; z < VoxelData.ChunkWidth; z++)
        for (var y = 0; y < VoxelData.ChunkHeight; y++)
        {
            ProcessBlock(new int3(x, y, z));
        }

        var mesh = meshData[0];
        mesh.SetVertexBufferParams(vertData.Length, VertexDescriptors());
        mesh.GetVertexData<VertData>().CopyFrom(vertData);

        mesh.subMeshCount = 1;
        mesh.SetIndexBufferParams(Triangles.Length, IndexFormat.UInt32);
        mesh.GetIndexData<int>().CopyFrom(Triangles);
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, Triangles.Length));
    }

    NativeArray<VertexAttributeDescriptor> VertexDescriptors()
    {
        var array = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp);
        array[0] = new VertexAttributeDescriptor(VertexAttribute.Position);
        array[1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2);
        return array;
    }

    void ProcessBlock(int3 position)
    {
        var block = TerrainManager.TileForPosition(WorldFromBlock(position));

        // Skip air blocks, we don't need to render anything
        if (block == TileType.AIR) return;

        for (var i = 0; i < DirectionChecks.Length; i++)
        {
            var dir = DirectionChecks[i];
            if (TerrainManager.TileForPosition(WorldFromBlock(position) + dir) != TileType.AIR)
                continue;

            DrawQuad(position, i);
        }
    }

    int3 WorldFromBlock(int3 blockPosition)
    {
        return blockPosition + new int3(ChunkId.x * VoxelData.ChunkWidth, 0, ChunkId.y * VoxelData.ChunkWidth);
    }

    void DrawQuad(int3 position, int offset)
    {
        var trisOffset = offset * 4;
        vertData.Add(new VertData(position + VoxelVerts[VoxelTris[trisOffset]], VoxelUvs[0]));
        vertData.Add(new VertData(position + VoxelVerts[VoxelTris[trisOffset + 1]], VoxelUvs[1]));
        vertData.Add(new VertData(position + VoxelVerts[VoxelTris[trisOffset + 2]], VoxelUvs[2]));
        vertData.Add(new VertData(position + VoxelVerts[VoxelTris[trisOffset + 3]], VoxelUvs[3]));

        Triangles.Add(VertexOffset);
        Triangles.Add(VertexOffset + 1);
        Triangles.Add(VertexOffset + 2);
        Triangles.Add(VertexOffset + 2);
        Triangles.Add(VertexOffset + 1);
        Triangles.Add(VertexOffset + 3);
        VertexOffset += 4;
    }

    public void Dispose()
    {
        vertData.Dispose();
        Triangles.Dispose();
    }
}
