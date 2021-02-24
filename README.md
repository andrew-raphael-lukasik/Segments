Segments is a small line renderer for Unity.Entities tech stack.

# Getting started with Segments.NativeListToSegmentsSystem:
```csharp
NativeList<float3x2> _segments;
Segments.NativeListToSegmentsSystem _segmentsSystem;
public JobHandle Dependency;
void OnEnable ()// OnCreate
{
    _segmentsSystem = Segments.Core.GetWorld().GetExistingSystem<Segments.NativeListToSegmentsSystem>();
    Entity prefab = Segments.Core.GetSegmentPrefabCopy();// prefab entity for you to modify & customize
    _segmentsSystem.CreateBatch( prefab , out _segments );
}
void Update ()// OnUpdate
{
    int index = 0;
    var job = new Segments.Plot.CircleJob(
        segments:       _segments ,
        index:          ref index ,
        r:              transform.localScale.x ,
        pos:            transform.position ,
        rot:            transform.rotation ,
        numSegments:    64
    );
    
    Dependency = job.Schedule( dependsOn:Dependency );
    _segmentsSystem.Dependencies.Add( Dependency );
}
void OnDisable ()// OnDestroy
{
    Dependency.Complete();
    _segmentsSystem.DestroyBatch( ref _segments , true );
}
```

# Systems
---
```csharp
SegmentTransformSystem : SystemBase
```
The fundamental system that makes all this work. Transforms meshes for rendering.

---
```csharp
NativeListToSegmentsSystem : SystemBase
```
Simplifies entity pool management to a single `NativeList<float3x2>` you fill with data however you need.

---
```csharp
NativeArrayToSegmentsSystem : SystemBase
```
Simplifies entity pool management to a single `NativeArray<float3x2>` you fill with data however you need.

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
