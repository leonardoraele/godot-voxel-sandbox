using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Raele.VoxelSandbox;

/// <summary>
/// Each vertex and edge of the marching cube is represented by a number from 0 to 7 (for vertexes) or 0 to 11
/// (for edges), as depicted in the following diagram.
///
///            6             7
///            +-------------+               +-----6-------+
///          / |           / |             / |            /|
///        /   |         /   |          11   7         10  5
///    2 +-----+-------+ 3   |         +------2------+     |
///      |     |       |     |         |     |       |     |
///      |   4 +-------+-----+ 5       |     +-----4-|-----+
///      |   /         |   /           3   8         1   9
///      | /           | /             | /           | /
///    0 +-------------+ 1             +------0------+
///
/// Vertex indexes:
///   0: bottom left vertex of front face (1)
///   1: bottom right vertex of front face (2)
///   2: top left vertex of front face (4)
///   3: top right vertex of front face (8)
///   4: bottom left vertex of back face (16)
///   5: bottom right vertex of back face (32)
///   6: top left vertex of back face (64)
///   7: top right vertex of back face (128)
///
/// Edge indexes:
///   0: bottom edge of front face (1)
///   1: right edge of front face (2)
///   2: top edge of front face (4)
///   3: left edge of front face (8)
///   4: bottom edge of back face (16)
///   5: right edge of back face (32)
///   6: top edge of back face (64)
///   7: left edge of back face (128)
///   8: bottom edge of left face (256)
///   9: bottom edge of right face (512)
///   10: top edge of right face (1024)
///   11: top edge of left face (2048)
///
/// Based on https://gist.github.com/dwilliamson/c041e3454a713e58baf6e4f8e5fffecd
/// </summary>
public static class MarchingCubesTable
{
	/// <summary>
	/// For each edge of the marching cube, this Vector3 array has the coordinate position of the edge in the
	/// corresponding index. For example, the bottom edge of the front face is at index 0. The coordinate position of
	/// the vectors are relative to the Zero position, at the bottom left vertex of the front face of the cube. For
	/// example, the edge 1 has a value of (1, 0.5, 0).
	/// </summary>
	// public readonly static float[,] EdgePositions = new float[,] {
	// 	{0.5f, 0, 0},
	// 	{1, 0.5f, 0},
	// 	{0.5f, 1, 0},
	// 	{0, 0.5f, 0},
	// 	{0.5f, 0, 1},
	// 	{1, 0.5f, 1},
	// 	{0.5f, 1, 1},
	// 	{0, 0.5f, 1},
	// 	{0, 0, 0.5f},
	// 	{1, 0, 0.5f},
	// 	{1, 1, 0.5f},
	// 	{0, 1, 0.5f},
	// };

	public readonly static Vector3[] EdgePositionsV = new Vector3[] {
		new Vector3(0.5f, 0, 0),
		new Vector3(1, 0.5f, 0),
		new Vector3(0.5f, 1, 0),
		new Vector3(0, 0.5f, 0),
		new Vector3(0.5f, 0, -1),
		new Vector3(1, 0.5f, -1),
		new Vector3(0.5f, 1, -1),
		new Vector3(0, 0.5f, -1),
		new Vector3(0, 0, -0.5f),
		new Vector3(1, 0, -0.5f),
		new Vector3(1, 1, -0.5f),
		new Vector3(0, 1, -0.5f),
	};

	public static IEnumerable<Vector3> GetEdgesForCase(int caseIndex) => GetEdgesForMask(Cases[caseIndex]);
	public static IEnumerable<Vector3> GetEdgesForMask(ushort bitmask) {
		return Enumerable.Range(0, EdgePositionsV.Length)
			.Where(edgeIndex => CheckMaskHasEdge(bitmask, edgeIndex))
			.Select(edgeIndex => EdgePositionsV[edgeIndex]);
	}
	public static bool CheckCaseHasEdge(int caseIndex, int edgeIndex) => CheckMaskHasEdge(Cases[caseIndex], edgeIndex);
	public static bool CheckMaskHasEdge(ushort bitmask, int edgeIndex) => (bitmask & (1 << edgeIndex)) != 0;

	/// <summary>
	/// Calculates the marching cube case by evaluating the density of each of the vertexes against the surface level.
	/// </summary>
    public static int GetCaseIndex(float surfaceLevel, float v0, float v1, float v2, float v3, float v4, float v5, float v6, float v7)
		=> (v0 > surfaceLevel ? 0b00000001 : 0) + (v1 > surfaceLevel ? 0b00000010 : 0)
			+ (v2 > surfaceLevel ? 0b00000100 : 0) + (v3 > surfaceLevel ? 0b00001000 : 0)
			+ (v4 > surfaceLevel ? 0b00010000 : 0) + (v5 > surfaceLevel ? 0b00100000 : 0)
			+ (v6 > surfaceLevel ? 0b01000000 : 0) + (v7 > surfaceLevel ? 0b10000000 : 0);

	/// <summary>
	/// Calculates the marching cube case by evaluating the state of each vertex. If the vertex is active, it is
	/// considered to be above the surface level. If it is inactive, it is considered to be below the surface level.
	/// </summary>
    public static int GetCaseIndex(bool v0, bool v1, bool v2, bool v3, bool v4, bool v5, bool v6, bool v7)
		=> (v0 ? 0b00000001 : 0) + (v1 ? 0b00000010 : 0) + (v2 ? 0b00000100 : 0) + (v3 ? 0b00001000 : 0)
			+ (v4 ? 0b00010000 : 0) + (v5 ? 0b00100000 : 0) + (v6 ? 0b01000000 : 0) + (v7 ? 0b10000000 : 0);

    /// <summary>
    /// Each item in this array represents a case of the marching cube.
    ///
    /// To map a case of the marching cube to a index of this array, add up the binary representation of the index of
    /// the vertex. To get the binary representation of the vertex, power 2 to the exponent of the index of the vertex.
    /// For example, for the vertex 0, the binary would be 1 (== 2^0). Do this for each vertex, then add them up. The
    /// sum is the index of the case in this array. For example, for the case where vertexes 4 and 5 are active, sum
    /// 2^4 + 2^5 = 48. Lookup for index 48 in this array for this case.
    ///
    /// For each marching cube case, the corresponding value in this array is a bit mask with the edges that form the
    /// triangle to be meshed. Each bit in the mask represents an edge, in the order of the edges as described in this
    /// class' header (at the top of this doc). For example, the case 0x109 (== 0b100001001) has the edges 0, 3 and 8,
    /// which form a triangle. Once you know which edges form the triangle, you can use the EdgePositions array to get
    /// the coordinates of the edges.
    /// </summary>
    public readonly static ushort[] Cases = {
		0x0, 0x109, 0x203, 0x30a, 0x80c, 0x905, 0xa0f, 0xb06,   // 0-7
		0x406, 0x50f, 0x605, 0x70c, 0xc0a, 0xd03, 0xe09, 0xf00, // 8-15
		0x190, 0x99, 0x393, 0x29a, 0x99c, 0x895, 0xb9f, 0xa96,  // 16-23
		0x596, 0x49f, 0x795, 0x69c, 0xd9a, 0xc93, 0xf99, 0xe90, // 24-31
		0x230, 0x339, 0x33, 0x13a, 0xa3c, 0xb35, 0x83f, 0x936,  // 32-39
		0x636, 0x73f, 0x435, 0x53c, 0xe3a, 0xf33, 0xc39, 0xd30, // 40-47
		0x3a0, 0x2a9, 0x1a3, 0xaa, 0xbac, 0xaa5, 0x9af, 0x8a6,  // 48-55
		0x7a6, 0x6af, 0x5a5, 0x4ac, 0xfaa, 0xea3, 0xda9, 0xca0, // 56-63
		0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc, 0x1c5, 0x2cf, 0x3c6,  // 64-71
		0xcc6, 0xdcf, 0xec5, 0xfcc, 0x4ca, 0x5c3, 0x6c9, 0x7c0, // 72-79
		0x950, 0x859, 0xb53, 0xa5a, 0x15c, 0x55, 0x35f, 0x256,  // 80-87
		0xd56, 0xc5f, 0xf55, 0xe5c, 0x55a, 0x453, 0x759, 0x650, // 88-95
		0xaf0, 0xbf9, 0x8f3, 0x9fa, 0x2fc, 0x3f5, 0xff, 0x1f6,  // 96-103
		0xef6, 0xfff, 0xcf5, 0xdfc, 0x6fa, 0x7f3, 0x4f9, 0x5f0, // 104-111
		0xb60, 0xa69, 0x963, 0x86a, 0x36c, 0x265, 0x16f, 0x66,  // 112-119
		0xf66, 0xe6f, 0xd65, 0xc6c, 0x76a, 0x663, 0x569, 0x460, // 120-127
		0x460, 0x569, 0x663, 0x76a, 0xc6c, 0xd65, 0xe6f, 0xf66, // 128-135
		0x66, 0x16f, 0x265, 0x36c, 0x86a, 0x963, 0xa69, 0xb60,  // 136-143
		0x5f0, 0x4f9, 0x7f3, 0x6fa, 0xdfc, 0xcf5, 0xfff, 0xef6, // 144-151
		0x1f6, 0xff, 0x3f5, 0x2fc, 0x9fa, 0x8f3, 0xbf9, 0xaf0,  // 152-159
		0x650, 0x759, 0x453, 0x55a, 0xe5c, 0xf55, 0xc5f, 0xd56, // 160-167
		0x256, 0x35f, 0x55, 0x15c, 0xa5a, 0xb53, 0x859, 0x950,  // 168-175
		0x7c0, 0x6c9, 0x5c3, 0x4ca, 0xfcc, 0xec5, 0xdcf, 0xcc6, // 176-183
		0x3c6, 0x2cf, 0x1c5, 0xcc, 0xbca, 0xac3, 0x9c9, 0x8c0,  // 184-191
		0xca0, 0xda9, 0xea3, 0xfaa, 0x4ac, 0x5a5, 0x6af, 0x7a6, // 192-199
		0x8a6, 0x9af, 0xaa5, 0xbac, 0xaa, 0x1a3, 0x2a9, 0x3a0,  // 200-207
		0xd30, 0xc39, 0xf33, 0xe3a, 0x53c, 0x435, 0x73f, 0x636, // 208-215
		0x936, 0x83f, 0xb35, 0xa3c, 0x13a, 0x33, 0x339, 0x230,  // 216-223
		0xe90, 0xf99, 0xc93, 0xd9a, 0x69c, 0x795, 0x49f, 0x596, // 224-231
		0xa96, 0xb9f, 0x895, 0x99c, 0x29a, 0x393, 0x99, 0x190,  // 232-239
		0xf00, 0xe09, 0xd03, 0xc0a, 0x70c, 0x605, 0x50f, 0x406, // 240-247
		0xb06, 0xa0f, 0x905, 0x80c, 0x30a, 0x203, 0x109, 0x0,   // 248-255
	};

	// public readonly static short[,] CasesA = new short[256,12] {
	// 	{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
	// 	{ 0, 3, 8 },
	// 	{ 0, 9, 1 },
	// 	{ 3, 8, 1, 1, 8, 9 },
	// 	{ 2, 11, 3 },
	// 	{ 8, 0, 11, 11, 0, 2 },
	// 	{ 3, 2, 11, 1, 0, 9 },
	// 	{ 11, 1, 2, 11, 9, 1, 11, 8, 9 },
	// 	{ 1, 10, 2 },
	// 	{ 0, 3, 8, 2, 1, 10 },
	// 	{ 10, 2, 9, 9, 2, 0 },
	// 	{ 8, 2, 3, 8, 10, 2, 8, 9, 10 },
	// 	{ 11, 3, 10, 10, 3, 1 },
	// 	{ 10, 0, 1, 10, 8, 0, 10, 11, 8 },
	// 	{ 9, 3, 0, 9, 11, 3, 9, 10, 11 },
	// 	{ 8, 9, 11, 11, 9, 10 },
	// 	{ 4, 8, 7 },
	// 	{ 7, 4, 3, 3, 4, 0 },
	// 	{ 4, 8, 7, 0, 9, 1 },
	// 	{ 1, 4, 9, 1, 7, 4, 1, 3, 7 },
	// 	{ 8, 7, 4, 11, 3, 2 },
	// 	{ 4, 11, 7, 4, 2, 11, 4, 0, 2 },
	// 	{ 0, 9, 1, 8, 7, 4, 11, 3, 2 },
	// 	{ 7, 4, 11, 11, 4, 2, 2, 4, 9, 2, 9, 1 },
	// 	{ 4, 8, 7, 2, 1, 10 },
	// 	{ 7, 4, 3, 3, 4, 0, 10, 2, 1 },
	// 	{ 10, 2, 9, 9, 2, 0, 7, 4, 8 },
	// 	{ 10, 2, 3, 10, 3, 4, 3, 7, 4, 9, 10, 4 },
	// 	{ 1, 10, 3, 3, 10, 11, 4, 8, 7 },
	// 	{ 10, 11, 1, 11, 7, 4, 1, 11, 4, 1, 4, 0 },
	// 	{ 7, 4, 8, 9, 3, 0, 9, 11, 3, 9, 10, 11 },
	// 	{ 7, 4, 11, 4, 9, 11, 9, 10, 11 },
	// 	{ 9, 4, 5 },
	// 	{ 9, 4, 5, 8, 0, 3 },
	// 	{ 4, 5, 0, 0, 5, 1 },
	// 	{ 5, 8, 4, 5, 3, 8, 5, 1, 3 },
	// 	{ 9, 4, 5, 11, 3, 2 },
	// 	{ 2, 11, 0, 0, 11, 8, 5, 9, 4 },
	// 	{ 4, 5, 0, 0, 5, 1, 11, 3, 2 },
	// 	{ 5, 1, 4, 1, 2, 11, 4, 1, 11, 4, 11, 8 },
	// 	{ 1, 10, 2, 5, 9, 4 },
	// 	{ 9, 4, 5, 0, 3, 8, 2, 1, 10 },
	// 	{ 2, 5, 10, 2, 4, 5, 2, 0, 4 },
	// 	{ 10, 2, 5, 5, 2, 4, 4, 2, 3, 4, 3, 8 },
	// 	{ 11, 3, 10, 10, 3, 1, 4, 5, 9 },
	// 	{ 4, 5, 9, 10, 0, 1, 10, 8, 0, 10, 11, 8 },
	// 	{ 11, 3, 0, 11, 0, 5, 0, 4, 5, 10, 11, 5 },
	// 	{ 4, 5, 8, 5, 10, 8, 10, 11, 8 },
	// 	{ 8, 7, 9, 9, 7, 5 },
	// 	{ 3, 9, 0, 3, 5, 9, 3, 7, 5 },
	// 	{ 7, 0, 8, 7, 1, 0, 7, 5, 1 },
	// 	{ 7, 5, 3, 3, 5, 1 },
	// 	{ 5, 9, 7, 7, 9, 8, 2, 11, 3 },
	// 	{ 2, 11, 7, 2, 7, 9, 7, 5, 9, 0, 2, 9 },
	// 	{ 2, 11, 3, 7, 0, 8, 7, 1, 0, 7, 5, 1 },
	// 	{ 2, 11, 1, 11, 7, 1, 7, 5, 1 },
	// 	{ 8, 7, 9, 9, 7, 5, 2, 1, 10 },
	// 	{ 10, 2, 1, 3, 9, 0, 3, 5, 9, 3, 7, 5 },
	// 	{ 7, 5, 8, 5, 10, 2, 8, 5, 2, 8, 2, 0 },
	// 	{ 10, 2, 5, 2, 3, 5, 3, 7, 5 },
	// 	{ 8, 7, 5, 8, 5, 9, 11, 3, 10, 3, 1, 10 },
	// 	{ 5, 11, 7, 10, 11, 5, 1, 9, 0 },
	// 	{ 11, 5, 10, 7, 5, 11, 8, 3, 0 },
	// 	{ 5, 11, 7, 10, 11, 5 },
	// 	{ 6, 7, 11 },
	// 	{ 7, 11, 6, 3, 8, 0 },
	// 	{ 6, 7, 11, 0, 9, 1 },
	// 	{ 9, 1, 8, 8, 1, 3, 6, 7, 11 },
	// 	{ 3, 2, 7, 7, 2, 6 },
	// 	{ 0, 7, 8, 0, 6, 7, 0, 2, 6 },
	// 	{ 6, 7, 2, 2, 7, 3, 9, 1, 0 },
	// 	{ 6, 7, 8, 6, 8, 1, 8, 9, 1, 2, 6, 1 },
	// 	{ 11, 6, 7, 10, 2, 1 },
	// 	{ 3, 8, 0, 11, 6, 7, 10, 2, 1 },
	// 	{ 0, 9, 2, 2, 9, 10, 7, 11, 6 },
	// 	{ 6, 7, 11, 8, 2, 3, 8, 10, 2, 8, 9, 10 },
	// 	{ 7, 10, 6, 7, 1, 10, 7, 3, 1 },
	// 	{ 8, 0, 7, 7, 0, 6, 6, 0, 1, 6, 1, 10 },
	// 	{ 7, 3, 6, 3, 0, 9, 6, 3, 9, 6, 9, 10 },
	// 	{ 6, 7, 10, 7, 8, 10, 8, 9, 10 },
	// 	{ 11, 6, 8, 8, 6, 4 },
	// 	{ 6, 3, 11, 6, 0, 3, 6, 4, 0 },
	// 	{ 11, 6, 8, 8, 6, 4, 1, 0, 9 },
	// 	{ 1, 3, 9, 3, 11, 6, 9, 3, 6, 9, 6, 4 },
	// 	{ 2, 8, 3, 2, 4, 8, 2, 6, 4 },
	// 	{ 4, 0, 6, 6, 0, 2 },
	// 	{ 9, 1, 0, 2, 8, 3, 2, 4, 8, 2, 6, 4 },
	// 	{ 9, 1, 4, 1, 2, 4, 2, 6, 4 },
	// 	{ 4, 8, 6, 6, 8, 11, 1, 10, 2 },
	// 	{ 1, 10, 2, 6, 3, 11, 6, 0, 3, 6, 4, 0 },
	// 	{ 11, 6, 4, 11, 4, 8, 10, 2, 9, 2, 0, 9 },
	// 	{ 10, 4, 9, 6, 4, 10, 11, 2, 3 },
	// 	{ 4, 8, 3, 4, 3, 10, 3, 1, 10, 6, 4, 10 },
	// 	{ 1, 10, 0, 10, 6, 0, 6, 4, 0 },
	// 	{ 4, 10, 6, 9, 10, 4, 0, 8, 3 },
	// 	{ 4, 10, 6, 9, 10, 4 },
	// 	{ 6, 7, 11, 4, 5, 9 },
	// 	{ 4, 5, 9, 7, 11, 6, 3, 8, 0 },
	// 	{ 1, 0, 5, 5, 0, 4, 11, 6, 7 },
	// 	{ 11, 6, 7, 5, 8, 4, 5, 3, 8, 5, 1, 3 },
	// 	{ 3, 2, 7, 7, 2, 6, 9, 4, 5 },
	// 	{ 5, 9, 4, 0, 7, 8, 0, 6, 7, 0, 2, 6 },
	// 	{ 3, 2, 6, 3, 6, 7, 1, 0, 5, 0, 4, 5 },
	// 	{ 6, 1, 2, 5, 1, 6, 4, 7, 8 },
	// 	{ 10, 2, 1, 6, 7, 11, 4, 5, 9 },
	// 	{ 0, 3, 8, 4, 5, 9, 11, 6, 7, 10, 2, 1 },
	// 	{ 7, 11, 6, 2, 5, 10, 2, 4, 5, 2, 0, 4 },
	// 	{ 8, 4, 7, 5, 10, 6, 3, 11, 2 },
	// 	{ 9, 4, 5, 7, 10, 6, 7, 1, 10, 7, 3, 1 },
	// 	{ 10, 6, 5, 7, 8, 4, 1, 9, 0 },
	// 	{ 4, 3, 0, 7, 3, 4, 6, 5, 10 },
	// 	{ 10, 6, 5, 8, 4, 7 },
	// 	{ 9, 6, 5, 9, 11, 6, 9, 8, 11 },
	// 	{ 11, 6, 3, 3, 6, 0, 0, 6, 5, 0, 5, 9 },
	// 	{ 11, 6, 5, 11, 5, 0, 5, 1, 0, 8, 11, 0 },
	// 	{ 11, 6, 3, 6, 5, 3, 5, 1, 3 },
	// 	{ 9, 8, 5, 8, 3, 2, 5, 8, 2, 5, 2, 6 },
	// 	{ 5, 9, 6, 9, 0, 6, 0, 2, 6 },
	// 	{ 1, 6, 5, 2, 6, 1, 3, 0, 8 },
	// 	{ 1, 6, 5, 2, 6, 1 },
	// 	{ 2, 1, 10, 9, 6, 5, 9, 11, 6, 9, 8, 11 },
	// 	{ 9, 0, 1, 3, 11, 2, 5, 10, 6 },
	// 	{ 11, 0, 8, 2, 0, 11, 10, 6, 5 },
	// 	{ 3, 11, 2, 5, 10, 6 },
	// 	{ 1, 8, 3, 9, 8, 1, 5, 10, 6 },
	// 	{ 6, 5, 10, 0, 1, 9 },
	// 	{ 8, 3, 0, 5, 10, 6 },
	// 	{ 6, 5, 10 },
	// 	{ 10, 5, 6 },
	// 	{ 0, 3, 8, 6, 10, 5 },
	// 	{ 10, 5, 6, 9, 1, 0 },
	// 	{ 3, 8, 1, 1, 8, 9, 6, 10, 5 },
	// 	{ 2, 11, 3, 6, 10, 5 },
	// 	{ 8, 0, 11, 11, 0, 2, 5, 6, 10 },
	// 	{ 1, 0, 9, 2, 11, 3, 6, 10, 5 },
	// 	{ 5, 6, 10, 11, 1, 2, 11, 9, 1, 11, 8, 9 },
	// 	{ 5, 6, 1, 1, 6, 2 },
	// 	{ 5, 6, 1, 1, 6, 2, 8, 0, 3 },
	// 	{ 6, 9, 5, 6, 0, 9, 6, 2, 0 },
	// 	{ 6, 2, 5, 2, 3, 8, 5, 2, 8, 5, 8, 9 },
	// 	{ 3, 6, 11, 3, 5, 6, 3, 1, 5 },
	// 	{ 8, 0, 1, 8, 1, 6, 1, 5, 6, 11, 8, 6 },
	// 	{ 11, 3, 6, 6, 3, 5, 5, 3, 0, 5, 0, 9 },
	// 	{ 5, 6, 9, 6, 11, 9, 11, 8, 9 },
	// 	{ 5, 6, 10, 7, 4, 8 },
	// 	{ 0, 3, 4, 4, 3, 7, 10, 5, 6 },
	// 	{ 5, 6, 10, 4, 8, 7, 0, 9, 1 },
	// 	{ 6, 10, 5, 1, 4, 9, 1, 7, 4, 1, 3, 7 },
	// 	{ 7, 4, 8, 6, 10, 5, 2, 11, 3 },
	// 	{ 10, 5, 6, 4, 11, 7, 4, 2, 11, 4, 0, 2 },
	// 	{ 4, 8, 7, 6, 10, 5, 3, 2, 11, 1, 0, 9 },
	// 	{ 1, 2, 10, 11, 7, 6, 9, 5, 4 },
	// 	{ 2, 1, 6, 6, 1, 5, 8, 7, 4 },
	// 	{ 0, 3, 7, 0, 7, 4, 2, 1, 6, 1, 5, 6 },
	// 	{ 8, 7, 4, 6, 9, 5, 6, 0, 9, 6, 2, 0 },
	// 	{ 7, 2, 3, 6, 2, 7, 5, 4, 9 },
	// 	{ 4, 8, 7, 3, 6, 11, 3, 5, 6, 3, 1, 5 },
	// 	{ 5, 0, 1, 4, 0, 5, 7, 6, 11 },
	// 	{ 9, 5, 4, 6, 11, 7, 0, 8, 3 },
	// 	{ 11, 7, 6, 9, 5, 4 },
	// 	{ 6, 10, 4, 4, 10, 9 },
	// 	{ 6, 10, 4, 4, 10, 9, 3, 8, 0 },
	// 	{ 0, 10, 1, 0, 6, 10, 0, 4, 6 },
	// 	{ 6, 10, 1, 6, 1, 8, 1, 3, 8, 4, 6, 8 },
	// 	{ 9, 4, 10, 10, 4, 6, 3, 2, 11 },
	// 	{ 2, 11, 8, 2, 8, 0, 6, 10, 4, 10, 9, 4 },
	// 	{ 11, 3, 2, 0, 10, 1, 0, 6, 10, 0, 4, 6 },
	// 	{ 6, 8, 4, 11, 8, 6, 2, 10, 1 },
	// 	{ 4, 1, 9, 4, 2, 1, 4, 6, 2 },
	// 	{ 3, 8, 0, 4, 1, 9, 4, 2, 1, 4, 6, 2 },
	// 	{ 6, 2, 4, 4, 2, 0 },
	// 	{ 3, 8, 2, 8, 4, 2, 4, 6, 2 },
	// 	{ 4, 6, 9, 6, 11, 3, 9, 6, 3, 9, 3, 1 },
	// 	{ 8, 6, 11, 4, 6, 8, 9, 0, 1 },
	// 	{ 11, 3, 6, 3, 0, 6, 0, 4, 6 },
	// 	{ 8, 6, 11, 4, 6, 8 },
	// 	{ 10, 7, 6, 10, 8, 7, 10, 9, 8 },
	// 	{ 3, 7, 0, 7, 6, 10, 0, 7, 10, 0, 10, 9 },
	// 	{ 6, 10, 7, 7, 10, 8, 8, 10, 1, 8, 1, 0 },
	// 	{ 6, 10, 7, 10, 1, 7, 1, 3, 7 },
	// 	{ 3, 2, 11, 10, 7, 6, 10, 8, 7, 10, 9, 8 },
	// 	{ 2, 9, 0, 10, 9, 2, 6, 11, 7 },
	// 	{ 0, 8, 3, 7, 6, 11, 1, 2, 10 },
	// 	{ 7, 6, 11, 1, 2, 10 },
	// 	{ 2, 1, 9, 2, 9, 7, 9, 8, 7, 6, 2, 7 },
	// 	{ 2, 7, 6, 3, 7, 2, 0, 1, 9 },
	// 	{ 8, 7, 0, 7, 6, 0, 6, 2, 0 },
	// 	{ 7, 2, 3, 6, 2, 7 },
	// 	{ 8, 1, 9, 3, 1, 8, 11, 7, 6 },
	// 	{ 11, 7, 6, 1, 9, 0 },
	// 	{ 6, 11, 7, 0, 8, 3 },
	// 	{ 11, 7, 6 },
	// 	{ 7, 11, 5, 5, 11, 10 },
	// 	{ 10, 5, 11, 11, 5, 7, 0, 3, 8 },
	// 	{ 7, 11, 5, 5, 11, 10, 0, 9, 1 },
	// 	{ 7, 11, 10, 7, 10, 5, 3, 8, 1, 8, 9, 1 },
	// 	{ 5, 2, 10, 5, 3, 2, 5, 7, 3 },
	// 	{ 5, 7, 10, 7, 8, 0, 10, 7, 0, 10, 0, 2 },
	// 	{ 0, 9, 1, 5, 2, 10, 5, 3, 2, 5, 7, 3 },
	// 	{ 9, 7, 8, 5, 7, 9, 10, 1, 2 },
	// 	{ 1, 11, 2, 1, 7, 11, 1, 5, 7 },
	// 	{ 8, 0, 3, 1, 11, 2, 1, 7, 11, 1, 5, 7 },
	// 	{ 7, 11, 2, 7, 2, 9, 2, 0, 9, 5, 7, 9 },
	// 	{ 7, 9, 5, 8, 9, 7, 3, 11, 2 },
	// 	{ 3, 1, 7, 7, 1, 5 },
	// 	{ 8, 0, 7, 0, 1, 7, 1, 5, 7 },
	// 	{ 0, 9, 3, 9, 5, 3, 5, 7, 3 },
	// 	{ 9, 7, 8, 5, 7, 9 },
	// 	{ 8, 5, 4, 8, 10, 5, 8, 11, 10 },
	// 	{ 0, 3, 11, 0, 11, 5, 11, 10, 5, 4, 0, 5 },
	// 	{ 1, 0, 9, 8, 5, 4, 8, 10, 5, 8, 11, 10 },
	// 	{ 10, 3, 11, 1, 3, 10, 9, 5, 4 },
	// 	{ 3, 2, 8, 8, 2, 4, 4, 2, 10, 4, 10, 5 },
	// 	{ 10, 5, 2, 5, 4, 2, 4, 0, 2 },
	// 	{ 5, 4, 9, 8, 3, 0, 10, 1, 2 },
	// 	{ 2, 10, 1, 4, 9, 5 },
	// 	{ 8, 11, 4, 11, 2, 1, 4, 11, 1, 4, 1, 5 },
	// 	{ 0, 5, 4, 1, 5, 0, 2, 3, 11 },
	// 	{ 0, 11, 2, 8, 11, 0, 4, 9, 5 },
	// 	{ 5, 4, 9, 2, 3, 11 },
	// 	{ 4, 8, 5, 8, 3, 5, 3, 1, 5 },
	// 	{ 0, 5, 4, 1, 5, 0 },
	// 	{ 5, 4, 9, 3, 0, 8 },
	// 	{ 5, 4, 9 },
	// 	{ 11, 4, 7, 11, 9, 4, 11, 10, 9 },
	// 	{ 0, 3, 8, 11, 4, 7, 11, 9, 4, 11, 10, 9 },
	// 	{ 11, 10, 7, 10, 1, 0, 7, 10, 0, 7, 0, 4 },
	// 	{ 3, 10, 1, 11, 10, 3, 7, 8, 4 },
	// 	{ 3, 2, 10, 3, 10, 4, 10, 9, 4, 7, 3, 4 },
	// 	{ 9, 2, 10, 0, 2, 9, 8, 4, 7 },
	// 	{ 3, 4, 7, 0, 4, 3, 1, 2, 10 },
	// 	{ 7, 8, 4, 10, 1, 2 },
	// 	{ 7, 11, 4, 4, 11, 9, 9, 11, 2, 9, 2, 1 },
	// 	{ 1, 9, 0, 4, 7, 8, 2, 3, 11 },
	// 	{ 7, 11, 4, 11, 2, 4, 2, 0, 4 },
	// 	{ 4, 7, 8, 2, 3, 11 },
	// 	{ 9, 4, 1, 4, 7, 1, 7, 3, 1 },
	// 	{ 7, 8, 4, 1, 9, 0 },
	// 	{ 3, 4, 7, 0, 4, 3 },
	// 	{ 7, 8, 4 },
	// 	{ 11, 10, 8, 8, 10, 9 },
	// 	{ 0, 3, 9, 3, 11, 9, 11, 10, 9 },
	// 	{ 1, 0, 10, 0, 8, 10, 8, 11, 10 },
	// 	{ 10, 3, 11, 1, 3, 10 },
	// 	{ 3, 2, 8, 2, 10, 8, 10, 9, 8 },
	// 	{ 9, 2, 10, 0, 2, 9 },
	// 	{ 8, 3, 0, 10, 1, 2 },
	// 	{ 2, 10, 1 },
	// 	{ 2, 1, 11, 1, 9, 11, 9, 8, 11 },
	// 	{ 11, 2, 3, 9, 0, 1 },
	// 	{ 11, 0, 8, 2, 0, 11 },
	// 	{ 3, 11, 2 },
	// 	{ 1, 8, 3, 9, 8, 1 },
	// 	{ 1, 9, 0 },
	// 	{ 8, 3, 0 },
	// 	{ }
	// };
}
