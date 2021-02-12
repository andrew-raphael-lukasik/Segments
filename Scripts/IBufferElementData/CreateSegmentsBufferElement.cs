using UnityEngine;

using Unity.Mathematics;
using Unity.Entities;

namespace EcsLineRenderer
{
	[InternalBufferCapacity(128)]
	public struct CreateSegmentsBufferElement : IBufferElementData
	{
		public float3 start, end;
		public half width;
		public Color color;
	}
}
