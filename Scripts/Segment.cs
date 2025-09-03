using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace Segments
{
    public struct Segment : IComponentData
    {
        public NativeList<float3x2> Buffer;
        public NativeReference<JobHandle> Dependency;
    }
}
