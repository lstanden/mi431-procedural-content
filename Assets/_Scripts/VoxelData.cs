using Unity.Collections;
using Unity.Mathematics;

public class VoxelData
{
    private static VoxelData _instance;
    private static object _lock = new object();

    public static VoxelData Instance => _instance ?? Initialize();

    public static VoxelData Initialize()
    {
        lock (_lock)
        {
            _instance = new VoxelData();
            return _instance;
        }
    }

    public static void Reset()
    {
        lock (_lock)
        {
            _instance = null;
        }
    }


    public const int ChunkHeight = 256;
    public const int ChunkWidth = 16;

    /// <summary>
    /// This is basically some vectors to allow us to calculate coordinates easily
    /// </summary>
    public readonly NativeArray<int3> DirectionChecks = new NativeArray<int3>(new[]
        {
            new int3(0, 0, -1), // Back 
            new int3(0, 0, 1), // Forward 
            new int3(0, 1, 0), // Up 
            new int3(0, -1, 0), // Down 
            new int3(-1, 0, 0), // Left 
            new int3(1, 0, 0), // Right 
        },
        Allocator.Persistent);

    /// <summary>
    /// All possible vertices for a block in the world.  We only add the ones we need based on DirectionChecks and VoxelTris
    /// </summary>
    public readonly NativeArray<float3> VoxelVerts = new NativeArray<float3>(new[]
        {
            new float3(0.0f, 0.0f, 0.0f),
            new float3(1.0f, 0.0f, 0.0f),
            new float3(1.0f, 1.0f, 0.0f),
            new float3(0.0f, 1.0f, 0.0f),
            new float3(0.0f, 0.0f, 1.0f),
            new float3(1.0f, 0.0f, 1.0f),
            new float3(1.0f, 1.0f, 1.0f),
            new float3(0.0f, 1.0f, 1.0f),
        },
        Allocator.Persistent);

    /// <summary>
    /// Indices of the VoxelVerts array that are needed based on the DirectionCheck we're currently looking at
    /// </summary>
    public readonly NativeArray<int> VoxelTris = new NativeArray<int>(new[]
        {
            0,
            3,
            1,
            2, // Back Face

            5,
            6,
            4,
            7, // Front Face

            3,
            7,
            2,
            6, // Top Face

            1,
            5,
            0,
            4, // Bottom Face

            4,
            7,
            0,
            3, // Left Face

            1,
            2,
            5,
            6 // Right Face
        },
        Allocator.Persistent);

    /// <summary>
    /// UVs are basically static, but we list them out here just to avoid duplication
    /// </summary>
    public readonly NativeArray<float2> VoxelUvs = new NativeArray<float2>(new[]
        {
            new float2(0.0f, 0.0f), new float2(0.0f, 1.0f), new float2(1.0f, 0.0f), new float2(1.0f, 1.0f)
        },
        Allocator.Persistent);
}
