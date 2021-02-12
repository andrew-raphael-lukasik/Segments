using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Segments
{
	[MaterialProperty("_AspectRatio",MaterialPropertyFormat.Float)]
	public struct SegmentAspectRatio : IComponentData
	{
		public float Value;
	}
}
