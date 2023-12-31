using System.Linq;
using Godot;

namespace Raele.Voxel;

[Tool]
public partial class TerrainBlock : Node3D
{
	[Export] public Vector3I Resolution = new Vector3I(32, 32, 32);
	private FastNoiseLite NoiseGenerator = new FastNoiseLite();
	private Godot.Collections.Array<Image> Image3D = null!;
    private Vector3[] Points = null!;

    public override void _Ready()
	{
		base._Ready();
		this.NoiseGenerator.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		this.Image3D = this.NoiseGenerator.GetImage3D(this.Resolution.X, this.Resolution.Y, this.Resolution.Z);
        this.Points = Enumerable.Range(0, this.Resolution.Z)
			.SelectMany(z =>
				Enumerable.Range(0, this.Resolution.Y)
					.SelectMany(y =>
						Enumerable.Range(0, this.Resolution.X)
							.Select(x => new Vector3(x, y, z))
					)
			)
			.ToArray();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		DebugDraw3D.DrawPoints(this.Points, .1f, Colors.White);
	}
}
