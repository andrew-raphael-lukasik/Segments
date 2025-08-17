using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

namespace Samples
{
    partial struct CopyEditorStateJob : IJobEntity
    {
        void Execute(StressTestComponent comp, ref StressTestSettings settings, ref LocalToWorld ltw)
        {
            settings = new StressTestSettings{
                numSegments = comp._numSegments,
                frequency = comp._frequency,
                everyFrame = comp._everyFrame,
            };
            ltw.Value = comp.transform.localToWorldMatrix;
        }
    }

    [WorldSystemFilter( WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor )]
    [UpdateInGroup( typeof(PresentationSystemGroup) , OrderFirst=true )]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct StressTestCopyEditorStateSystem : ISystem
    {
        public void OnUpdate ( ref SystemState state )
        {
            state.CompleteDependency();
            new CopyEditorStateJob().Run();
        }
    }
}
