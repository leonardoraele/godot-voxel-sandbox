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
		new Vector3(0.5f, 0, 1),
		new Vector3(1, 0.5f, 1),
		new Vector3(0.5f, 1, 1),
		new Vector3(0, 0.5f, 1),
		new Vector3(0, 0, 0.5f),
		new Vector3(1, 0, 0.5f),
		new Vector3(1, 1, 0.5f),
		new Vector3(0, 1, 0.5f),
	};

	public static Vector3[] GetEdgesForCase(int caseIndex) => GetEdgesForMask(Cases[caseIndex]);
	public static Vector3[] GetEdgesForMask(ushort bitmask) {
		return Enumerable.Range(0, EdgePositionsV.Length)
			.Where(edgeIndex => CheckMaskHasEdge(bitmask, edgeIndex))
			.Select(edgeIndex => EdgePositionsV[edgeIndex])
			.ToArray();
	}
	public static bool CheckCaseHasEdge(int caseIndex, int edgeIndex) => CheckMaskHasEdge(Cases[caseIndex], edgeIndex);
	public static bool CheckMaskHasEdge(ushort bitmask, int edgeIndex) => (bitmask & (1 << edgeIndex)) != 0;

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
		0x0, 0x109, 0x203, 0x30a, 0x80c, 0x905, 0xa0f, 0xb06, // 0-7
		0x406, 0x50f, 0x605, 0x70c, 0xc0a, 0xd03, 0xe09, 0xf00, // 8-15
		0x190, 0x99, 0x393, 0x29a, 0x99c, 0x895, 0xb9f, 0xa96, // 16-23
		0x596, 0x49f, 0x795, 0x69c, 0xd9a, 0xc93, 0xf99, 0xe90, // 24-31
		0x230, 0x339, 0x33, 0x13a, 0xa3c, 0xb35, 0x83f, 0x936, // 32-39
		0x636, 0x73f, 0x435, 0x53c, 0xe3a, 0xf33, 0xc39, 0xd30, // 40-47
		0x3a0, 0x2a9, 0x1a3, 0xaa, 0xbac, 0xaa5, 0x9af, 0x8a6, // 48-55
		0x7a6, 0x6af, 0x5a5, 0x4ac, 0xfaa, 0xea3, 0xda9, 0xca0, // 56-63
		0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc, 0x1c5, 0x2cf, 0x3c6, // 64-71
		0xcc6, 0xdcf, 0xec5, 0xfcc, 0x4ca, 0x5c3, 0x6c9, 0x7c0, // 72-79
		0x950, 0x859, 0xb53, 0xa5a, 0x15c, 0x55, 0x35f, 0x256, // 80-87
		0xd56, 0xc5f, 0xf55, 0xe5c, 0x55a, 0x453, 0x759, 0x650, // 88-95
		0xaf0, 0xbf9, 0x8f3, 0x9fa, 0x2fc, 0x3f5, 0xff, 0x1f6, // 96-103
		0xef6, 0xfff, 0xcf5, 0xdfc, 0x6fa, 0x7f3, 0x4f9, 0x5f0, // 104-111
		0xb60, 0xa69, 0x963, 0x86a, 0x36c, 0x265, 0x16f, 0x66, // 112-119
		0xf66, 0xe6f, 0xd65, 0xc6c, 0x76a, 0x663, 0x569, 0x460, // 120-127
		0x460, 0x569, 0x663, 0x76a, 0xc6c, 0xd65, 0xe6f, 0xf66, // 128-135
		0x66, 0x16f, 0x265, 0x36c, 0x86a, 0x963, 0xa69, 0xb60, // 136-143
		0x5f0, 0x4f9, 0x7f3, 0x6fa, 0xdfc, 0xcf5, 0xfff, 0xef6, // 144-151
		0x1f6, 0xff, 0x3f5, 0x2fc, 0x9fa, 0x8f3, 0xbf9, 0xaf0, // 152-159
		0x650, 0x759, 0x453, 0x55a, 0xe5c, 0xf55, 0xc5f, 0xd56, // 160-167
		0x256, 0x35f, 0x55, 0x15c, 0xa5a, 0xb53, 0x859, 0x950, // 168-175
		0x7c0, 0x6c9, 0x5c3, 0x4ca, 0xfcc, 0xec5, 0xdcf, 0xcc6, // 176-183
		0x3c6, 0x2cf, 0x1c5, 0xcc, 0xbca, 0xac3, 0x9c9, 0x8c0, // 184-191
		0xca0, 0xda9, 0xea3, 0xfaa, 0x4ac, 0x5a5, 0x6af, 0x7a6, // 192-199
		0x8a6, 0x9af, 0xaa5, 0xbac, 0xaa, 0x1a3, 0x2a9, 0x3a0, // 200-207
		0xd30, 0xc39, 0xf33, 0xe3a, 0x53c, 0x435, 0x73f, 0x636, // 208-215
		0x936, 0x83f, 0xb35, 0xa3c, 0x13a, 0x33, 0x339, 0x230, // 216-223
		0xe90, 0xf99, 0xc93, 0xd9a, 0x69c, 0x795, 0x49f, 0x596, // 224-231
		0xa96, 0xb9f, 0x895, 0x99c, 0x29a, 0x393, 0x99, 0x190, // 232-239
		0xf00, 0xe09, 0xd03, 0xc0a, 0x70c, 0x605, 0x50f, 0x406, // 240-247
		0xb06, 0xa0f, 0x905, 0x80c, 0x30a, 0x203, 0x109, 0x0, // 248-255
	};
}
