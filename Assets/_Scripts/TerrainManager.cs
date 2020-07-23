using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    /// <summary>
    /// Used to track the player location so chunks can be spawned around them
    /// </summary>
    public Transform player;

    /// <summary>
    /// Prefab used to generate new chunks and render them in the world
    /// </summary>
    public GameObject ChunkPrefab;

    /// <summary>
    /// Controls at what range new chunks should be loaded and rendered to the world
    /// </summary>
    public int RenderRange = 16;

    /// <summary>
    /// Controls at what range chunks should be removed from the world because the player is too far away from them
    /// </summary>
    public int UnloadRange = 20;

    private Dictionary<int2, GameObject> LoadedChunks = new Dictionary<int2, GameObject>();
    private float3 PlayerLocation;

    private void Start()
    {
        VoxelData.Initialize();
        StartCoroutine(LoadChunksIfNecessary());
        StartCoroutine(UnloadChunksIfNecessary());
    }

    void Update()
    {
        // The two coroutines read this value
        PlayerLocation = player.transform.position;
    }

    IEnumerator LoadChunksIfNecessary()
    {
        while (true)
        {
            var playerChunk = new int2((int) PlayerLocation.x / VoxelData.ChunkWidth,
                (int) PlayerLocation.z / VoxelData.ChunkWidth);
            for (var x = -RenderRange; x < RenderRange; x++)
            for (var z = -RenderRange; z < RenderRange; z++)
            {
                var cid = new int2(playerChunk.x - x, playerChunk.y - z);
                if (LoadedChunks.ContainsKey(cid)) continue;

                var go    = Instantiate(ChunkPrefab, transform);
                var chunk = go.GetComponent<Chunk>();
                chunk.ChunkId = cid;

                LoadedChunks.Add(cid, go);
            }

            // Skip to next frame
            yield return null;
        }
    }

    IEnumerator UnloadChunksIfNecessary()
    {
        while (true)
        {
            var playerChunk = new int2((int) PlayerLocation.x / VoxelData.ChunkWidth,
                (int) PlayerLocation.z / VoxelData.ChunkWidth);
            var keys = LoadedChunks.Keys.ToArray();
            foreach (var key in keys)
            {
                if (math.distance(playerChunk, key) < UnloadRange)
                    continue;

                var go = LoadedChunks[key];
                Destroy(go);
                LoadedChunks.Remove(key);
            }

            // Skip to next frame
            yield return null;
        }
    }

    /// <summary>
    /// This is effectively our World Generation.  math.lerp is being derpy, so I'm using Mathf.lerp for now, which means this isn't Burst compatible.
    /// </summary>
    /// <param name="position">Player position in the world</param>
    /// <returns></returns>
    public static TileType TileForPosition(float3 position)
    {
        // Bottom of the world
        if (position.y < 0) return TileType.AIR;

        // Top of the world
        if (position.y > 255) return TileType.AIR;

        var scale = 0.01f;
        var x     = (position.x + 0.1f) * scale;
        var y     = (position.z + 0.1f) * scale;

        var groundnoise = (noise.srnoise(new float2(position.x, position.z) * .01f) + 1) / 2;
        
        var groundlevel = (int) math.lerp(128, 200, groundnoise);

        if (position.y > groundlevel)
            return TileType.AIR;
        return TileType.DIRT;
    }
}
