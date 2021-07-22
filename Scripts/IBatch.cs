using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

namespace Segments
{
	public interface IBatch
	{
		NativeArray<float3x2> buffer {get;}
		Material material {get;}
		Mesh mesh {get;}
		JobHandle Dependency {get;}
		bool isDisposed {get;set;}
		void Dispose ();
	}
}
