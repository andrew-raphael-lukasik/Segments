using UnityEngine;

using Unity.Mathematics;
using Unity.Entities;

namespace EcsLineRenderer
{
	[InternalBufferCapacity(128)]
	public struct SegmentGroupBufferElement : IBufferElementData
	{
		public Entity entity;

		public static implicit operator Entity ( SegmentGroupBufferElement value ) => value.entity;
		public static implicit operator SegmentGroupBufferElement ( Entity e )=> new SegmentGroupBufferElement{ entity = e };
	}
}
