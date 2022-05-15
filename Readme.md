# Procedural Maze/Dungeon Generation Script-set

This is a simple bare-bones dungeon/maze generation script-set that can be used to create dynamically aligned, tile-based, procedural levels.
The algorithm works completely independent of the given tiles and uses "connector" marker objects to tie multiple tiles together.
Although not shown in the sample scene, this dynamic generation also allows for elevation between tiles, as long as they can be aligned.

![gif1](Media/m1.gif)

Adding new tiles is easy but requires some tweaking, here is a video that shows how to implement a new tile called 'Room2'. 
(Github doesn't like me embeding a video but the video link can be access from [HERE](Media/addingNewTile.mp4))
![gif1](Media/addingNewTile.gif)