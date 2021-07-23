Segments is a lightweight and fast line renderer for DOTS tech stack.

# Getting started with Segments:
```csharp
[SerializeField] Material _material = null;
Segments.Batch _segments;
void OnEnable ()// OnCreate
{
    Segments.Core.CreateBatch( out _segments , _material );
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
    
    _segments.Dependency = job.Schedule( dependsOn:_segments.Dependency );
}
void OnDisable ()// OnDestroy
{
	_segments.Dispose();
}
```
# Performance

100.000 segments stress test? No problem.

@todo: details


# Requirements
- Unity 2020.1
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
