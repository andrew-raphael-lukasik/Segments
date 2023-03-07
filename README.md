Segments is a lightweight line renderer for DOTS tech stack.

It works by creating mesh batches formatted as `MeshTopology.Lines`, `SystemBase` job system schedules updates with `MeshDataArray`. `Geometry shader` creates output triangles on the GPU.

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

EDIT: Still working on improving this as plotting shapes is not as fast as I wanted

@todo: details

# Requirements
- Unity 2022.x
- `Entities Graphics` package
- Universal Render Pipeline

# Samples
- mesh wireframe (runtime)
<img src="https://i.imgur.com/NCC71mD.gif" height="200">

- drawing mesh bounding boxes (runtime)
<img src="https://i.imgur.com/J1mzvSbl.jpg" height="200">

# Installation Unity 2022.x
Select `Add package from git URL` from `Package Manager` window and pass this address:
```
https://github.com/andrew-raphael-lukasik/segments.git#upm
```
