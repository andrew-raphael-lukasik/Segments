using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

namespace Samples
{
    [WorldSystemFilter( WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor )]
    [UpdateInGroup( typeof(PresentationSystemGroup) )]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct StressTestSystem : ISystem
    {
        EntityQuery _query;

        [Unity.Burst.BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Segments.Segment, StressTestSettings, LocalToWorld>()
                .Build(ref state); 
            state.RequireForUpdate(_query);
        }

        [Unity.Burst.BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (segment, settings, ltw, entity) in SystemAPI.Query< RefRW<Segments.Segment> , RefRO<StressTestSettings> , RefRO<LocalToWorld> >().WithEntityAccess())
            if (settings.ValueRO.everyFrame==1 || segment.ValueRO.Buffer.Length!=settings.ValueRO.numSegments)
            {
                if (segment.ValueRO.Buffer.Length!=settings.ValueRO.numSegments)
                    segment.ValueRW.Buffer.Length = settings.ValueRO.numSegments;

                state.Dependency = new StressTestJob{
                    ltw = ltw.ValueRO,
                    settings = settings.ValueRO,
                    time = Time.time,// note: picked Time.time here **only** because it happen to change outside play mode where SystemAPI.Time.ElapsedTime is play mode only
                    segmentBuffer = segment.ValueRW.Buffer.AsArray(),
                }.ScheduleParallel(settings.ValueRO.numSegments, 64, state.Dependency);

                // request mesh update
                state.EntityManager.SetComponentEnabled<Segments.SegmentUpdateRequest>(entity, true);
            }
        }
    }

    struct StressTestSettings : IComponentData
    {
        public int numSegments;
        public float frequency;
        public byte everyFrame;
    }

    [Unity.Burst.BurstCompile]
    struct StressTestJob : IJobParallelForBatch
    {
        public LocalToWorld ltw;
        public StressTestSettings settings;
        public float time;
        [WriteOnly] public NativeArray<float3x2> segmentBuffer;
        void IJobParallelForBatch.Execute (int startIndex, int count)
        {
            float3 mag = new float3(math.length(ltw.Right), math.length(ltw.Up), math.length(ltw.Forward));

            float freqTau = settings.frequency * math.TAU;
            float freqPiTau = settings.frequency/math.PI * math.TAU;
            float step = 1f / settings.numSegments;
            
            for( int i=0 ; i<count ; i++ )
            {
                int index = startIndex + i;

                float t0 = index * step;
                float t1 = t0 + step;

                float amp0 = math.sin(time + freqTau * t0);
                float amp1 = math.sin(time + freqTau * t1);
                float amp2 = math.sin(time + freqPiTau * t0);
                float amp3 = math.sin(time + freqPiTau * t1);

                float3 p0 = new float3(0,0,mag.z*t0) + new float3(0,mag.y*amp0,0) + new float3(mag.x*amp2,0,0);
                float3 p1 = new float3(0,0,mag.z*t1) + new float3(0,mag.y*amp1,0) + new float3(mag.x*amp3,0,0);
                segmentBuffer[index] = new float3x2(p0, p1);
            }
        }
    }
}
