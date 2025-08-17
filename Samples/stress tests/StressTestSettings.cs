using Unity.Entities;

namespace Samples
{
    struct StressTestSettings : IComponentData
    {
        public int numSegments;
        public float frequency;
        public bool everyFrame;
    }
}
