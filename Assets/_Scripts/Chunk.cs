using System;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class Chunk : MonoBehaviour
{
    public int2 ChunkId;
    private MeshFilter meshFilter;

    void Start()
    {
        name = $"Chunk [{ChunkId.x}, {ChunkId.y}]";
        transform.position = new Vector3(ChunkId.x * VoxelData.ChunkWidth, 0, ChunkId.y * VoxelData.ChunkWidth);
        meshFilter = GetComponent<MeshFilter>();
        StartCoroutine(GenerateMesh());
    }

    /// <summary>
    /// Generates the mesh using the C# Job system, and sets the mesh as appropriate when the job is completed
    /// </summary>
    /// <returns></returns>
    IEnumerator GenerateMesh()
    {
        var job = new TerrainGenJob(ChunkId,
            VoxelData.Instance.DirectionChecks,
            VoxelData.Instance.VoxelVerts,
            VoxelData.Instance.VoxelTris,
            VoxelData.Instance.VoxelUvs);
        var handle = job.Schedule();

        // This magic allows us to not block frame rendering while the job runs
        while (!handle.IsCompleted)
            yield return null;

        handle.Complete();

        // Each mesh needs to be unique
        var mesh = new Mesh();
        Mesh.ApplyAndDisposeWritableMeshData(job.meshData, mesh);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        job.Dispose();
    }
}
