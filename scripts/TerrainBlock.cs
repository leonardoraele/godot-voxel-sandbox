using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Raele.Voxel;

[Tool]
public partial class TerrainBlock : Node3D
{
	[Export] public Vector3I Resolution = new Vector3I(32, 32, 32);
	private FastNoiseLite NoiseGenerator = new FastNoiseLite();
	private Godot.Collections.Array<Image> Image3D = null!;
    private IEnumerable<Vector3I> Points;
    [Export] private float Threshold = 0.5f;
	[Export] bool ForceReset = false;

    public override void _Ready()
	{
		base._Ready();
		this.NoiseGenerator.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		this.Reset();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (this.ForceReset) {
			this.Reset();
		}
		DebugDraw3D.DrawPoints(
			this.Points
				.Where(point => this.Image3D[point.Z].GetPixel(point.X, point.Y).R > this.Threshold)
				.Select(point => new Vector3(point.X, point.Y, point.Z))
				.ToArray(),
			.1f,
			Colors.White
		);
	}

    private void Reset()
    {
		this.ForceReset = false;
		this.NoiseGenerator.Seed = (int) Time.GetTicksMsec();
		this.Image3D = this.NoiseGenerator.GetImage3D(this.Resolution.X, this.Resolution.Y, this.Resolution.Z);
		this.Points = Enumerable.Range(0, this.Image3D.Count)
			.SelectMany(z =>
				Enumerable.Range(0, this.Image3D[z].GetHeight())
					.SelectMany(y =>
						Enumerable.Range(0, this.Image3D[z].GetWidth())
							.Select(x => new Vector3I(x, y, z))
					)
			);
    }
}
