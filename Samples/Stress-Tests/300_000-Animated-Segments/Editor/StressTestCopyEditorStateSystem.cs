using Unity.Entities;
using Unity.Transforms;

namespace Samples
{
    [WorldSystemFilter( WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor )]
    [UpdateInGroup( typeof(PresentationSystemGroup) , OrderFirst=true )]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct StressTestCopyEditorStateSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            new CopyDataJob().Run();
        }

        partial struct CopyDataJob : IJobEntity
        {
            void Execute(StressTestAuthoring comp, ref StressTestSettings settings, ref LocalToWorld ltw)
            {
                settings = new StressTestSettings{
                    numSegments = comp._numSegments,
                    frequency   = comp._frequency,
                    everyFrame  = comp._everyFrame,
                };
                ltw.Value = comp.transform.localToWorldMatrix;
            }
        }
    }
}
