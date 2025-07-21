Segments is a lightweight line renderer for DOTS tech stack.

- You create and then fill the `Segment` buffer (pairs of points) plotting shapes you want, `SegmentUpdateSystem` then pushes this data to the GPU where `geometry shader` creates output triangles on screen.
- Shape lifetime control and plotting can happen either in a system, job, editor window or a monobehaviour - your choice.
- Can be used for runtime shapes only or in the editor for debug gizmos.

![Screencast_20250721_000319-ezgif com-video-to-gif-converter (2)](https://github.com/user-attachments/assets/8ab05960-c7dc-420b-9300-fadd06554574)

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

Stress tested with 300k segments on my laptop (i7-7700HQ, GTX1060M) and bottleneck turned out to be the GPU (shader).
See for yourself, this exact test scene is provided as one of the samples.

@todo: details

# Requirements
- Unity 6000.0
- URP (not tested with HDRP yet)

# Samples
- mesh wireframe
<img height="300" alt="image" src="https://github.com/user-attachments/assets/b401f24c-e612-4d2e-9640-27e0b330f982" />

- drawing mesh bounding boxes
<img height="300" alt="image" src="https://github.com/user-attachments/assets/3ee90180-6176-469c-8cea-ffa49bd41c76" />


# Installation Unity 6000.x
Add this line in `manifest.json` / `dependencies`:
```
"com.andrewraphaellukasik.segments": "https://github.com/andrew-raphael-lukasik/segments.git#upm",
```

Or via `Package Manager` / `Add package from git URL`:
```
https://github.com/andrew-raphael-lukasik/segments.git#upm
```
