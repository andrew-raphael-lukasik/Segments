using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EcsLineRenderer
{
	public struct SegmentWidth : IComponentData
	{
		public half Value;
	}
}
