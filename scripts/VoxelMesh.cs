using System;
using Godot;

namespace Raele.VoxelSandbox;

[Tool]
public partial class VoxelMesh : MeshInstance3D {
	[Export] public MeshingAlgorithm Algorithm = MeshingAlgorithm.SimpleMarchingCubes;
	[Export] public Material? TopFaceMaterial;
	[Export] public Material? SideFaceMaterial;
	[Export] public Material? BottomFaceMaterial;
	[Export] public bool Regenerate = false;
	[Export] public bool SetSimpleGeometry = false;

	private MeshingAlgorithm LastAlgorithm = MeshingAlgorithm.SimpleMarchingCubes;
    private VoxelData Data = new VoxelData();

    // TODO See https://transvoxel.org/ For a LOD solution for voxels
    // TODO See https://en.wikipedia.org/wiki/Mesh_generation#Techniques for more meshing algorithms.
    // TODO More resources:
    // https://paulbourke.net/geometry/polygonise/
    // https://developer.nvidia.com/gpugems/gpugems3/part-i-geometry/chapter-1-generating-complex-procedural-terrains-using-gpu
    // https://people.eecs.berkeley.edu/~jrs/meshpapers/LorensenCline.pdf
    public enum MeshingAlgorithm {
		Minecraft,
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
			this.RegenerateGeometry();
		} else if (this.SetSimpleGeometry) {
			this.SetSimpleGeometry = false;
			this.GenerateSimpleGeometry();
		}
		if (this.LastAlgorithm != this.Algorithm) {
			this.LastAlgorithm = this.Algorithm;
			this.BuildMesh();
		}
    }

    private void RegenerateGeometry()
    {
		this.Data = VoxelData.GeneratePerlin(new Aabb(Vector3.Zero, Vector3.One * 32));
		this.BuildMesh();
    }

    private void GenerateSimpleGeometry()
    {
        this.Data = new VoxelData {
			Densities = new float[,,] {
				{ { -1, -1, -1 }, { -1, -1, -1 }, { -1, -1, -1 } },
				{ { -1, -1, -1 }, { -1,  1, -1 }, { -1, -1, -1 } },
				{ { -1, -1, -1 }, { -1, -1, -1 }, { -1, -1, -1 } }
			}
		};
		this.BuildMesh();
    }

    private void BuildMesh()
    {
		switch(this.Algorithm) {
			case MeshingAlgorithm.SimpleMarchingCubes:
				this.BuildMesh_SimpleMarchingCubes();
				break;
			case MeshingAlgorithm.Minecraft:
				this.BuildMesh_Minecraft();
				break;
			default:
				GD.PushError(new NotImplementedException($"Meshing algorithm {this.Algorithm} not implemented."));
				break;
		}
    }

    private void BuildMesh_Minecraft()
    {
		ArrayMesh mesh = new ArrayMesh();
		SurfaceTool builder;

		// Top face
		builder = new SurfaceTool();
		builder.Begin(Mesh.PrimitiveType.Triangles);
		builder.SetMaterial(this.TopFaceMaterial);
		for (int z = 0; z < this.Data.Depth; z++) {
			for (int y = 0; y < this.Data.Height; y++) {
				for (int x = 0; x < this.Data.Width; x++) {
					if (this.Data.Densities[x, y, z] < this.Data.SurfaceLevel) {
						continue;
					}
					// Top face
					if (y < this.Data.Height - 1 && this.Data.Densities[x, y + 1, z] < this.Data.SurfaceLevel) {
						builder.SetNormal(Vector3.Up);
						builder.SetUV(Vector2.Down);
						builder.AddVertex(new Vector3(x - 0.5f, y + 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.One);
						builder.AddVertex(new Vector3(x + 0.5f, y + 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Zero);
						builder.AddVertex(new Vector3(x - 0.5f, y + 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.Zero);
						builder.AddVertex(new Vector3(x - 0.5f, y + 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.One);
						builder.AddVertex(new Vector3(x + 0.5f, y + 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Right);
						builder.AddVertex(new Vector3(x + 0.5f, y + 0.5f, -z + 0.5f));
					}
				}
			}
		}
		builder.GenerateTangents();
		builder.Commit(mesh);

		// Side faces
		builder = new SurfaceTool();
		builder.Begin(Mesh.PrimitiveType.Triangles);
		builder.SetMaterial(this.SideFaceMaterial);
		for (int z = 0; z < this.Data.Depth; z++) {
			for (int y = 0; y < this.Data.Height; y++) {
				for (int x = 0; x < this.Data.Width; x++) {
					if (this.Data.Densities[x, y, z] < this.Data.SurfaceLevel) {
						continue;
					}
					// Right face
					if (x < this.Data.Width - 1 && this.Data.Densities[x + 1, y, z] < this.Data.SurfaceLevel) {
						builder.SetNormal(Vector3.Right);
						builder.SetUV(Vector2.One);
						builder.AddVertex(new Vector3(x + 0.5f, y - 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Down);
						builder.AddVertex(new Vector3(x + 0.5f, y - 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.Right);
						builder.AddVertex(new Vector3(x + 0.5f, y + 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Right);
						builder.AddVertex(new Vector3(x + 0.5f, y + 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Down);
						builder.AddVertex(new Vector3(x + 0.5f, y - 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.Zero);
						builder.AddVertex(new Vector3(x + 0.5f, y + 0.5f, -z + 0.5f));
					}
					// Back face (Z-)
					if (z < this.Data.Depth - 1 && this.Data.Densities[x, y, z + 1] < this.Data.SurfaceLevel) {
						builder.SetNormal(Vector3.Forward);
						builder.SetUV(Vector2.Down);
						builder.AddVertex(new Vector3(x - 0.5f, y - 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.One);
						builder.AddVertex(new Vector3(x + 0.5f, y - 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Zero);
						builder.AddVertex(new Vector3(x - 0.5f, y + 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Zero);
						builder.AddVertex(new Vector3(x - 0.5f, y + 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.One);
						builder.AddVertex(new Vector3(x + 0.5f, y - 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Right);
						builder.AddVertex(new Vector3(x + 0.5f, y + 0.5f, -z - 0.5f));
					}
					// Left face
					if (x > 0 && this.Data.Densities[x - 1, y, z] < this.Data.SurfaceLevel) {
						builder.SetNormal(Vector3.Left);
						builder.SetUV(Vector2.Down);
						builder.AddVertex(new Vector3(x - 0.5f, y - 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Zero);
						builder.AddVertex(new Vector3(x - 0.5f, y + 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.One);
						builder.AddVertex(new Vector3(x - 0.5f, y - 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.One);
						builder.AddVertex(new Vector3(x - 0.5f, y - 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.Zero);
						builder.AddVertex(new Vector3(x - 0.5f, y + 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Right);
						builder.AddVertex(new Vector3(x - 0.5f, y + 0.5f, -z + 0.5f));
					}
					// Front face (Z+)
					if (z > 0 && this.Data.Densities[x, y, z - 1] < this.Data.SurfaceLevel) {
						builder.SetNormal(Vector3.Back);
						builder.SetUV(Vector2.Down);
						builder.AddVertex(new Vector3(x - 0.5f, y - 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.Zero);
						builder.AddVertex(new Vector3(x - 0.5f, y + 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.One);
						builder.AddVertex(new Vector3(x + 0.5f, y - 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.One);
						builder.AddVertex(new Vector3(x + 0.5f, y - 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.Zero);
						builder.AddVertex(new Vector3(x - 0.5f, y + 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.Right);
						builder.AddVertex(new Vector3(x + 0.5f, y + 0.5f, -z + 0.5f));
					}
				}
			}
		}
		builder.GenerateTangents();
		builder.Commit(mesh);

		// Bottom face
		builder = new SurfaceTool();
		builder.Begin(Mesh.PrimitiveType.Triangles);
		builder.SetMaterial(this.BottomFaceMaterial);
		for (int z = 0; z < this.Data.Depth; z++) {
			for (int y = 0; y < this.Data.Height; y++) {
				for (int x = 0; x < this.Data.Width; x++) {
					if (this.Data.Densities[x, y, z] < this.Data.SurfaceLevel) {
						continue;
					}
					// Bottom face
					if (y > 0 && this.Data.Densities[x, y - 1, z] < this.Data.SurfaceLevel) {
						builder.SetNormal(Vector3.Down);
						builder.SetUV(Vector2.Down);
						builder.AddVertex(new Vector3(x - 0.5f, y - 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Zero);
						builder.AddVertex(new Vector3(x - 0.5f, y - 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.One);
						builder.AddVertex(new Vector3(x + 0.5f, y - 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.One);
						builder.AddVertex(new Vector3(x + 0.5f, y - 0.5f, -z - 0.5f));
						builder.SetUV(Vector2.Zero);
						builder.AddVertex(new Vector3(x - 0.5f, y - 0.5f, -z + 0.5f));
						builder.SetUV(Vector2.Right);
						builder.AddVertex(new Vector3(x + 0.5f, y - 0.5f, -z + 0.5f));
					}
				}
			}
		}
		builder.GenerateTangents();
		builder.Commit(mesh);

		this.Mesh = mesh;
    }

    private void BuildMesh_SimpleMarchingCubes()
	{
		ArrayMesh mesh = new ArrayMesh();
		SurfaceTool builder;

		// Top surfaces
		builder = new SurfaceTool();
		builder.Begin(Mesh.PrimitiveType.Triangles);
		builder.SetMaterial(this.TopFaceMaterial);
		for (int z = 0; z < this.Data.Depth - 1; z++) {
			for (int y = 0; y < this.Data.Height - 1; y++) {
				for (int x = 0; x < this.Data.Width - 1; x++) {
					int mcCaseIndex = MarchingCubesTable.GetCaseIndex(
						this.Data.Densities[x, y, z],
						this.Data.Densities[x + 1, y, z],
						this.Data.Densities[x, y + 1, z],
						this.Data.Densities[x + 1, y + 1, z],
						this.Data.Densities[x, y, z + 1],
						this.Data.Densities[x + 1, y, z + 1],
						this.Data.Densities[x, y + 1, z + 1],
						this.Data.Densities[x + 1, y + 1, z + 1],
						this.Data.SurfaceLevel
					);
					for (int i = 0; i < 12 && MarchingCubesTable.CaseArrays[mcCaseIndex, i] != -1; i += 3) {
						Vector3 v = new Vector3(x, y, -z),
							v0 = v + MarchingCubesTable.EdgePositionsV[MarchingCubesTable.CaseArrays[mcCaseIndex, i]],
							v1 = v + MarchingCubesTable.EdgePositionsV[MarchingCubesTable.CaseArrays[mcCaseIndex, i + 1]],
							v2 = v + MarchingCubesTable.EdgePositionsV[MarchingCubesTable.CaseArrays[mcCaseIndex, i + 2]],
							cross = (v1 - v0).Cross(v2 - v0);
						if (cross.Y < 0) {
							builder.SetUV(Vector2.Down);
							builder.AddVertex(v0);
							builder.SetUV(Vector2.Right);
							builder.AddVertex(v1);
							builder.SetUV(Vector2.Zero);
							builder.AddVertex(v2);
						}
					}
				}
			}
		}
		builder.GenerateNormals();
		builder.GenerateTangents();
		builder.Commit(mesh);

		// Side surfaces
		builder = new SurfaceTool();
		builder.Begin(Mesh.PrimitiveType.Triangles);
		builder.SetMaterial(this.SideFaceMaterial);
		for (int z = 0; z < this.Data.Depth - 1; z++) {
			for (int y = 0; y < this.Data.Height - 1; y++) {
				for (int x = 0; x < this.Data.Width - 1; x++) {
					int mcCaseIndex = MarchingCubesTable.GetCaseIndex(
						this.Data.Densities[x, y, z],
						this.Data.Densities[x + 1, y, z],
						this.Data.Densities[x, y + 1, z],
						this.Data.Densities[x + 1, y + 1, z],
						this.Data.Densities[x, y, z + 1],
						this.Data.Densities[x + 1, y, z + 1],
						this.Data.Densities[x, y + 1, z + 1],
						this.Data.Densities[x + 1, y + 1, z + 1],
						this.Data.SurfaceLevel
					);
					for (int i = 0; i < 12 && MarchingCubesTable.CaseArrays[mcCaseIndex, i] != -1; i += 3) {
						Vector3 v = new Vector3(x, y, -z),
							v0 = v + MarchingCubesTable.EdgePositionsV[MarchingCubesTable.CaseArrays[mcCaseIndex, i]],
							v1 = v + MarchingCubesTable.EdgePositionsV[MarchingCubesTable.CaseArrays[mcCaseIndex, i + 1]],
							v2 = v + MarchingCubesTable.EdgePositionsV[MarchingCubesTable.CaseArrays[mcCaseIndex, i + 2]],
							cross = (v1 - v0).Cross(v2 - v0);
						if (cross.Y == 0) {
							builder.SetUV(Vector2.Down);
							builder.AddVertex(v0);
							builder.SetUV(Vector2.Right);
							builder.AddVertex(v1);
							builder.SetUV(Vector2.Zero);
							builder.AddVertex(v2);
						}
					}
				}
			}
		}
		builder.GenerateNormals();
		builder.GenerateTangents();
		builder.Commit(mesh);

		// Bottom surfaces
		builder = new SurfaceTool();
		builder.Begin(Mesh.PrimitiveType.Triangles);
		builder.SetMaterial(this.BottomFaceMaterial);
		for (int z = 0; z < this.Data.Depth - 1; z++) {
			for (int y = 0; y < this.Data.Height - 1; y++) {
				for (int x = 0; x < this.Data.Width - 1; x++) {
					int mcCaseIndex = MarchingCubesTable.GetCaseIndex(
						this.Data.Densities[x, y, z],
						this.Data.Densities[x + 1, y, z],
						this.Data.Densities[x, y + 1, z],
						this.Data.Densities[x + 1, y + 1, z],
						this.Data.Densities[x, y, z + 1],
						this.Data.Densities[x + 1, y, z + 1],
						this.Data.Densities[x, y + 1, z + 1],
						this.Data.Densities[x + 1, y + 1, z + 1],
						this.Data.SurfaceLevel
					);
					for (int i = 0; i < 12 && MarchingCubesTable.CaseArrays[mcCaseIndex, i] != -1; i += 3) {
						Vector3 v = new Vector3(x, y, -z),
							v0 = v + MarchingCubesTable.EdgePositionsV[MarchingCubesTable.CaseArrays[mcCaseIndex, i]],
							v1 = v + MarchingCubesTable.EdgePositionsV[MarchingCubesTable.CaseArrays[mcCaseIndex, i + 1]],
							v2 = v + MarchingCubesTable.EdgePositionsV[MarchingCubesTable.CaseArrays[mcCaseIndex, i + 2]],
							cross = (v1 - v0).Cross(v2 - v0);
						if (cross.Y > 0) {
							builder.SetUV(Vector2.Down);
							builder.AddVertex(v0);
							builder.SetUV(Vector2.Right);
							builder.AddVertex(v1);
							builder.SetUV(Vector2.Zero);
							builder.AddVertex(v2);
						}
					}
				}
			}
		}
		builder.GenerateNormals();
		builder.GenerateTangents();
		builder.Commit(mesh);

		this.Mesh = mesh;
	}
}
