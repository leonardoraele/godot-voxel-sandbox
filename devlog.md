### 30/12/2023
To start the project, I decided to start with the rendering.
To render a voxel space, I need an octree data structure.
I have the tendency of trying to implement my own code whenever I need something that is not supported by the standard
library, but I have implemented an quad-tree (a 2D version of the octree) in C# for another project in the past and,
although I was satisfied with the result, it was a bit complicated to get there, so I'm not very thrilling with the idea
of implementing an octree from scratch.
Also, voxels have the tendency of being very resource consuming, so I know it need a performant implementation, and
testing and optimizing data structures take time. Finally, I don't even know if a C# implementation will be enough to
achieve a satisfactory performance. If it ends up C# is not enough, I will have to throw the work away and resort to a
lower level implementation. With all that in mind, it was clear I should use an existing implementation.
I found https://github.com/mcserep/NetOctree. It looks fitting. It even has functions to check for collision. Maybe I
will be able to use it in my character controller to detect objects.

Now I need data to put in the octree. 3D Perlin Noise should do. I found this package in NuGet
https://github.com/otac0n/RandomAccessPerlinNoise, but it has no instructions on how to use it and the code has no
comments, so it's borderline useless.

### 31/12/2023
- 13:44	I finally discovered Godot has a built-in FastNoiseLite class that helps generating noise of different types,
inclusing Perlin noise, Worley noise, Voronoi diagrams, Simplex noise, and more.
- 14:59 **Checkpoint 1: Drawing 3D dots on screen** I lost some time figuring out how to show the points generated by
the noise on screen. Unfortunately, Godot don't have API (or at least I didn't found) for debug drawing stuff on 3D
space. I know it has support for custom 2D drawing using the `CanvasItem._Draw()`, but I didn't found anything similar
for 3D. I settled on using a Godot addon called Debug Draw 3D (https://github.com/DmitriySalnikov/godot_debug_draw_cs).