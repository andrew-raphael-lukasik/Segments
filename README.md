Segments is a lightweight line renderer for Unity.Entities tech stack.

# Getting started with Segments:
```csharp
var entityManager = Segments.Core.GetWorld().EntityManager;
Entity prefab = Segments.Core.GetSegmentPrefabCopy( material:null , width:0.01f );
Entity x_axis = entityManager.Instantiate( prefab );
    entityManager.AddComponentData( x_axis , new MaterialColor{ Value=new float4{ x=1 , w=1 } });
    entityManager.SetComponentData( x_axis , new Segments.Segment{ start = float3.zero , end = new float3{ x=1 } } );
Entity y_axis = entityManager.Instantiate( prefab );
    entityManager.AddComponentData( y_axis , new MaterialColor{ Value=new float4{ y=1 , w=1 } });
    entityManager.SetComponentData( y_axis , new Segments.Segment{ start = float3.zero , end = new float3{ y=1 } } );
Entity z_axis = entityManager.Instantiate( prefab );
    entityManager.AddComponentData( z_axis , new MaterialColor{ Value=new float4{ z=1 , w=1 } });
    entityManager.SetComponentData( z_axis , new Segments.Segment{ start = float3.zero , end = new float3{ z=1 } } );
```

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
# Performance

100.000 segments stress test:

<img src="https://i.imgur.com/ZKUyzFa.jpg" height="200">

Cons: Very costly for this number of entities at the moment.

Pros: Threaded jobs schedule pretty well to spread that cost.

Conclusion: I recommend staying in 1-10k segments range until fixed.

# Systems
`SegmentTransformSystem` - The main system that makes all this work. Calculates `LocalToWorld` matrices for rendering.

`NativeListToSegmentsSystem` - Simplifies entity pool management to a single `NativeList<float3x2>` you fill with data however you need.

`NativeArrayToSegmentsSystem` - Simplifies entity pool management to a single `NativeArray<float3x2>` you fill with data however you need.

---

# Requirements
- Unity 2020.x
- Hybrid Renderer

# Samples
- mesh wireframe (runtime)
<img src="https://i.imgur.com/NCC71mD.gif" height="200">

- drawing mesh bounding boxes (runtime)
<img src="https://i.imgur.com/J1mzvSbl.jpg" height="200">

# Installation Unity 2020.x
Add this line in `manifest.json` / `dependencies`:
```
"com.andrewraphaellukasik.segments": "https://github.com/andrew-raphael-lukasik/segments.git#upm",
```

Or via `Package Manager` / `Add package from git URL`:
```
https://github.com/andrew-raphael-lukasik/segments.git#upm
```
