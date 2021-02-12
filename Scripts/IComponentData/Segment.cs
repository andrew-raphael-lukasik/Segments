using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EcsLineRenderer
{
	[System.Serializable]
	[WriteGroup(typeof(LocalToWorld))]
	public struct Segment : IComponentData
	{
		public float3 start;
		public float3 end;
	}
}
