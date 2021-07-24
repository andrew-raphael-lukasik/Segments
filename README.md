Segments is a lightweight and fast line renderer for DOTS tech stack.

# Getting started with Segments:
```csharp
Segments.Batch _batch;
void OnEnable () => Segments.Core.CreateBatch( out _batch );
void OnDisable () => _batch.Dispose();
void Update ()
{
	_batch.Dependency.Complete();

	var buffer = _batch.buffer;
	buffer.Length = 3;
	Vector3 position = transform.position;
	buffer[0] = new float3x2( position , position+transform.right );
	buffer[1] = new float3x2( position , position+transform.up );
	buffer[2] = new float3x2( position , position+transform.forward );
}
```
# Performance

100_000 segments? No problem!

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
