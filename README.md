Segments is a lightweight line renderer for DOTS tech stack.

How it works: You create and then fill the `Segment` buffer (pairs of points) plotting shapes you want, `SegmentUpdateSystem` then pushes this data to the GPU where `geometry shader` creates output triangles on screen.

# Getting started with Segments:
```csharp
Entity _segments;
void OnEnable () => Segments.Core.Create(out _segments);// creates an Entity that will hold all the vertex data and will be responsible for drawing them
void OnDisable () => Segments.Core.Destroy(_segments);// destroys the entity and all data associated with it
void Update ()
{
    DynamicBuffer<float3x2> segments = Segments.Core.GetBuffer(_segments);
    segments.Length = 3;
    Vector3 pos = transform.position;
    segments[0] = new float3x2( pos , pos+transform.right );
    segments[1] = new float3x2( pos , pos+transform.up );
    segments[2] = new float3x2( pos , pos+transform.forward );
}
```

# Performance

100_000 segments? No problem!

@todo: details

# Requirements
- Unity 6000.0
- URP (not tested with HDRP yet)

# Samples
- mesh wireframe (runtime)
<img src="https://i.imgur.com/NCC71mD.gif" height="200">

- drawing mesh bounding boxes (runtime)
<img src="https://i.imgur.com/J1mzvSbl.jpg" height="200">

# Installation Unity 6000.x
Add this line in `manifest.json` / `dependencies`:
```
"com.andrewraphaellukasik.segments": "https://github.com/andrew-raphael-lukasik/segments.git#upm",
```

Or via `Package Manager` / `Add package from git URL`:
```
https://github.com/andrew-raphael-lukasik/segments.git#upm
```
