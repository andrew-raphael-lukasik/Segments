using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;

namespace Segments
{
    public struct Segment : IComponentData
    {
        public NativeList<float3x2> Buffer;
    }
}
