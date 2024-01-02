using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

namespace Raele.VoxelSandbox;

[Tool]
public partial class VoxelMesh : MeshInstance3D {
	[Export] public MeshingAlgorithm Algorithm = MeshingAlgorithm.SimpleMarchingCubes;
	[Export] public bool Regenerate = false;

	// TODO See https://transvoxel.org/ For a LOD solution for voxels
	// TODO See https://en.wikipedia.org/wiki/Mesh_generation#Techniques for more meshing algorithms.
	// TODO More resources:
	// https://paulbourke.net/geometry/polygonise/
	// https://developer.nvidia.com/gpugems/gpugems3/part-i-geometry/chapter-1-generating-complex-procedural-terrains-using-gpu
	// https://people.eecs.berkeley.edu/~jrs/meshpapers/LorensenCline.pdf
	public enum MeshingAlgorithm {
		/// <summary>
		/// The simplest algorithm. It takes note of each connecing vertices that are higher than the surface level and
		/// looks up the triangles to be meshed from a table.
		/// </summary>
		SimpleMarchingCubes,
		/// <summary>
		/// Similar to the simple marching cubes, but it linearly interpolates between the vertices based on the value
		/// of the voxel to find the exact position for the triangles, so two meshing cubes will rarely look the same.
		/// e.g. simple marching cubes will always put a triangle vertice in the middle point between two vertices of
		/// the meshing cube, while lerping merching cubes can put the triangle vertice anywhere between the two meshing
		/// cube vertices.
		/// </summary>
		LerpingMarchingCubes,
		/// <summary>
		/// In marching tetrahedra, each cube is split into six irregular tetrahedra by cutting the cube in half three
		/// times, cutting diagonally through each of the three pairs of opposing faces. In this way, the tetrahedra all
		/// share one of the main diagonals of the cube. Instead of the twelve edges of the cube, we now have nineteen
		/// edges: the original twelve, six face diagonals, and the main diagonal. Just like in marching cubes, the
		/// intersections of these edges with the isosurface are approximated by linearly interpolating the values at
		/// the grid points.
		/// </summary>
		MarchingTetrahedrons,
		/// <summary>
		/// Similar to the marching tetrahedra, sliced into 5 tetrahedra, using a (Diamond cubic) lattice as a basis.
		/// See https://en.wikipedia.org/wiki/Marching_tetrahedra#Diamond_Lattice_Cell_-_Alternative_Cube_Slicing_Method
		/// </summary>
		DiamondLatticeCubes,
		/// <summary>
		/// https://en.wikipedia.org/wiki/Asymptotic_decider
		/// </summary>
		AsymptoticDecider,
	}

    public override void _Ready()
    {
        base._Ready();
		this.BuildMesh();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
		if (this.Regenerate) {
			this.Regenerate = false;
			this.BuildMesh();
		}
    }

    private void BuildMesh()
    {
        if (this.Algorithm == MeshingAlgorithm.SimpleMarchingCubes) {
			this.BuildMesh_SimpleMarchingCubes();
		} else {
			throw new NotImplementedException();
		}
    }

    private void BuildMesh_SimpleMarchingCubes() {
		VoxelData data = VoxelData.GeneratePerlin(new Aabb(Vector3.Zero, Vector3.One * 32));
		SurfaceTool builder = new SurfaceTool();
		builder.Begin(Mesh.PrimitiveType.Triangles);
		builder.SetColor(Colors.White);
		// builder.SetUV(Vector2.Zero);
		for (int z = 0; z < data.Depth - 1; z++) {
			for (int y = 0; y < data.Height - 1; y++) {
				for (int x = 0; x < data.Width - 1; x++) {
					int mcCaseIndex =
						(data.Values[x, y, z] > data.SurfaceLevel ? 0b00000001 : 0)
						+ (data.Values[x + 1, y, z] > data.SurfaceLevel ? 0b00000010 : 0)
						+ (data.Values[x, y + 1, z] > data.SurfaceLevel ? 0b00000100 : 0)
						+ (data.Values[x + 1, y + 1, z] > data.SurfaceLevel ? 0b00001000 : 0)
						+ (data.Values[x, y, z + 1] > data.SurfaceLevel ? 0b00010000 : 0)
						+ (data.Values[x + 1, y, z + 1] > data.SurfaceLevel ? 0b00100000 : 0)
						+ (data.Values[x, y + 1, z + 1] > data.SurfaceLevel ? 0b01000000 : 0)
						+ (data.Values[x + 1, y + 1, z + 1] > data.SurfaceLevel ? 0b10000000 : 0);
					MarchingCubesTable.GetEdgesForCase(mcCaseIndex)
						.Select(edge => new Vector3(x, y, z) + edge)
						.ForEach(vertex => builder.AddVertex(vertex));
				}
			}
		}
		ArrayMesh mesh = new ArrayMesh();
		builder.Commit(mesh);
		this.Mesh = mesh;
	}
}
