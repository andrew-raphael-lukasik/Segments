# Segments
Segments is a line renderer for Unity.Entities tech stack

# Systems
---
```csharp
SegmentTransformSystem : SystemBase
```
The fundamental system that makes all this work. Transforms meshes for rendering.

---
```csharp
NativeArrayToSegmentsSystem : SystemBase
```
Fill NativeArray < float3x2 > with data, this system will do the rest.

---
```csharp
NativeListToSegmentsSystem : SystemBase
```
Fill NativeList < float3x2 > with data, this system will do the rest.

---

# requirements
Unity 2020.x

# samples
- mesh wireframe (runtime)
<img src="https://i.imgur.com/NCC71mD.gif" height="200">

- drawing mesh bounding boxes (runtime)
<img src="https://i.imgur.com/J1mzvSbl.jpg" height="200">

# installation Unity 2020.x
Add this line in `manifest.json` / `dependencies`:
```
"com.andrewraphaellukasik.segments": "https://github.com/andrew-raphael-lukasik/segments.git#upm",
```

Or via `Package Manager` / `Add package from git URL`:
```
https://github.com/andrew-raphael-lukasik/segments.git#upm
```
