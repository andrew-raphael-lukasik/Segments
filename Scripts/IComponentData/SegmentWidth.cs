using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Segments
{
	public struct SegmentWidth : IComponentData
	{
		public half Value;
	}
}
