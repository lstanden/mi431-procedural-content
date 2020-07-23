using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

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

        public Mesh.MeshDataArray meshData;

        private NativeList<VertData> vertData;

        private NativeList<int> Triangles;

        private int VertexOffset;

        public TerrainGenJob(int2 _chunkId)
        {
            ChunkId = _chunkId;

            meshData = Mesh.AllocateWritableMeshData(1);

            vertData = new NativeList<VertData>(Allocator.TempJob);
            Triangles = new NativeList<int>(Allocator.TempJob);

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
            mesh.SetVertexBufferParams(vertData.Length,
                new VertexAttributeDescriptor(VertexAttribute.Position),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2));
            mesh.GetVertexData<VertData>().CopyFrom(vertData);

            mesh.subMeshCount = 1;
            mesh.SetIndexBufferParams(Triangles.Length, IndexFormat.UInt32);
            mesh.GetIndexData<int>().CopyFrom(Triangles);
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, Triangles.Length));
        }

        void ProcessBlock(int3 position)
        {
            var block = TerrainManager.TileForPosition(WorldFromBlock(position));

            // Skip air blocks, we don't need to render anything
            if (block == TileType.AIR) return;

            for (var i = 0; i < VoxelData.DirectionChecks.Length; i++)
            {
                var dir = VoxelData.DirectionChecks[i];
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
            vertData.Add(new VertData(position + VoxelData.VoxelVerts[VoxelData.VoxelTris[offset, 0]],
                VoxelData.VoxelUvs[0]));
            vertData.Add(new VertData(position + VoxelData.VoxelVerts[VoxelData.VoxelTris[offset, 1]],
                VoxelData.VoxelUvs[1]));
            vertData.Add(new VertData(position + VoxelData.VoxelVerts[VoxelData.VoxelTris[offset, 2]],
                VoxelData.VoxelUvs[2]));
            vertData.Add(new VertData(position + VoxelData.VoxelVerts[VoxelData.VoxelTris[offset, 3]],
                VoxelData.VoxelUvs[3]));

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