using Godot;

namespace Raele.VoxelSandbox;

public partial record VoxelData
{
	public static VoxelData GeneratePerlin(Aabb space)
	{
        FastNoiseLite noiseGenerator = new FastNoiseLite {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Seed = (int) Time.GetUnixTimeFromSystem(),
        };
		space.Position.Round();
		space.Size.Round();
		int width = Mathf.RoundToInt(space.Size.X);
		int height = Mathf.RoundToInt(space.Size.Y);
		int depth = Mathf.RoundToInt(space.Size.Z);
		float[,,] values = new float[width, height, depth];
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				for (int z = 0; z < depth; z++) {
					values[x, y, z] = noiseGenerator.GetNoise3D(x, y, z);
				}
			}
		}
		return new VoxelData { Values = values, Space = space };
	}

	public Aabb Space { get; init; }
    public float SurfaceLevel { get; init; }
    public float[,,] Values { get; init; } = null!;

	public int Width => this.Values.GetLength(0);
	public int Height => this.Values.GetLength(1);
	public int Depth => this.Values.GetLength(2);
}