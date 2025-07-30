# Segments

Segments is a lightweight line renderer for DOTS tech stack.

- You create and then fill the `Segment` buffer (pairs of points) plotting shapes you want, `SegmentUpdateSystem` then pushes this data to the GPU where `geometry shader` creates output triangles on screen.
- `Segment` buffer's lifetime control and plotting, being part of an `Entity`, can happen either in a system, job, editor window or a monobehaviour - your choice.
- Can be used for runtime shapes only or in the editor for debug gizmos as well.
- To develop the look of the lines to your specific needs you are expected to know shader programming basics to be able to fork and modify the base shader on your own


<img width="1894" height="837" alt="image" src="https://github.com/user-attachments/assets/dfc38b18-52c0-4e91-af14-1fb9fa2d14a0" />


## Getting started:

Here is a minimum code that will draw lines on screen:

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

Code above is just an illustration of the workflow to get you started. The best way of doing this is with use of job system as these will result in the best performance. Look into Samples to learn more.


## Performance

Stress tested with 300k segments on my laptop (i7-7700HQ, GTX1060M) and bottleneck turned out to be the GPU (shader).

RenderDoc debugger shows that stress test scene generates very high amount of PS invocations (at 1920x876 res). I thought about maybe adding depth-only prepass to reduce this but that would require changes to URP renderer which is something I don't find fitting for this project - I want it to be plug&play, with minimum dependencies.

<img height="300" alt="image" src="https://github.com/user-attachments/assets/c6b02d4f-1620-4708-a37f-56171a2e56c2" />

I'm investigating whenever replacing geometry shader with a compute shader will reduce the GPU time.

Also, this exact test scene is provided as one of the samples so you can test it yourself.

## Samples
- stress test
<img height="300" alt="image" src="https://github.com/user-attachments/assets/8ab05960-c7dc-420b-9300-fadd06554574" />

- mesh wireframe
<img height="300" alt="image" src="https://github.com/user-attachments/assets/b401f24c-e612-4d2e-9640-27e0b330f982" />

- drawing mesh bounding boxes
<img height="300" alt="image" src="https://github.com/user-attachments/assets/3ee90180-6176-469c-8cea-ffa49bd41c76" />


## Requirements
- Unity 6000.0
- URP (not tested with HDRP yet)


## Installation Unity 6000.x
Add this line in `manifest.json` / `dependencies`:
```
"com.andrewraphaellukasik.segments": "https://github.com/andrew-raphael-lukasik/segments.git#upm",
```

Or via `Package Manager` / `Add package from git URL`:
```
https://github.com/andrew-raphael-lukasik/segments.git#upm
```
