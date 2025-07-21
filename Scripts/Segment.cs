using Unity.Mathematics;
using Unity.Entities;

namespace Segments
{
    public struct Segment : IBufferElementData
    {
        public float3x2 Value;
    }
}
