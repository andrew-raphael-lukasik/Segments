using Unity.Mathematics;
using Unity.Entities;

namespace Segments
{
    public struct Segment : IBufferElementData
    {
        public float3x2 Value;
        
        public Segment (float3 a, float3 b) => this.Value = new float3x2(a, b);
        public Segment (float3x2 ab) => this.Value = ab;
        
        public static implicit operator Segment ( float3x2 value ) => new Segment{Value = value};
        public static implicit operator float3x2 ( Segment value ) => value.Value;
    }
}
