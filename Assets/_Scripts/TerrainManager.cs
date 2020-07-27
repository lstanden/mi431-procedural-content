using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    /// <summary>
    /// Used to define the blocks that can be generated
    /// </summary>
    public BlockDefinitions BlockDefinitions;

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
    private static readonly int MainTexture = Shader.PropertyToID("_MainTexture");

    private void Start()
    {
        VoxelData.Initialize();
        BlockDefinitions.GenerateBlocks();
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

                var go = Instantiate(ChunkPrefab, transform);
                var chunk = go.GetComponent<Chunk>();
                chunk.ChunkId = cid;
                chunk.BlockData = BlockDefinitions.Blocks;

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

        var pos = new float2(position.x, position.z);
        var groundnoise = noise.snoise(pos * .005f) * noise.snoise(pos * .001f);
        var groundlevel = (int)math.remap(0, 1, 100, 200, groundnoise);

        var dirtlevel = (int) math.remap(-1f, 1f, 1f, 5f,
            noise.snoise(new float2(position.x + .1f, position.z + .1f) * .01f));

        if (position.y > groundlevel)
            return TileType.AIR;

        var cave = noise.snoise(position * .0035f) > .7f;
        if (cave)
            return TileType.AIR;

        if (position.y == groundlevel)
            return TileType.GRASS;

        if (position.y >= groundlevel - dirtlevel)
            return TileType.DIRT;

        return TileType.STONE;
    }
}