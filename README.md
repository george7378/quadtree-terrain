# quadtree-terrain
>For a similar terrain system which uses chunks, check out [this project](https://github.com/george7378/chunked-terrain). Also take a look at [this article](http://www.gkristiansen.co.uk/2018/04/an-algorithm-for-infinite-worlds.html) which goes into more high-level detail regarding the algorithm.

QuadTree Terrain is a C#/XNA program demonstrating a quadtree terrain splitting algorithm which can be used to render very large-scale worlds.

As you move around the terrain, the level of detail will increase/decrease based on camera distance. The scene is built using deterministic noise which can be modified by changing the parameters in the code. There is also a water plane which adds a bit of variety to the landscape.

You can explore the world using the mouse and keyboard. When the program starts, you must press **C** to attach the mouse to the camera. You can then use the **WASD** keys to fly around. If you press **Space**, the camera will follow the contours of the terrain as you move.

![Sunlight reflecting off the scene](https://raw.githubusercontent.com/george7378/quadtree-terrain/master/_img/1.png)
![Some calm lakes](https://raw.githubusercontent.com/george7378/quadtree-terrain/master/_img/2.png)
![Naked terrain](https://raw.githubusercontent.com/george7378/quadtree-terrain/master/_img/3.png)
