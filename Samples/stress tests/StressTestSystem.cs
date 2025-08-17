using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

namespace Samples
{
    [Unity.Burst.BurstCompile]
    partial struct StressTestJob : IJobEntity
    {
        public float time;
        void Execute(ref DynamicBuffer<Segments.Segment> buffer, in StressTestSettings settings, in LocalToWorld ltw)
        {
            if( settings.everyFrame || buffer.Length!=settings.numSegments )
            {
                buffer.Length = settings.numSegments;

                float3 mag = new float3(math.length(ltw.Right), math.length(ltw.Up), math.length(ltw.Forward));
                float freqTau = settings.frequency * math.TAU;
                float freqPiTau = settings.frequency/math.PI * math.TAU;
                float step = 1f / settings.numSegments;

                for( int i = 0 ; i<buffer.Length ; i++ )
                // {
                //     float2 t = new float2(i, i+1) / settings.numSegments;
                //     float2 amp = math.sin(time + settings.frequency * t * math.PI*2f);
                //     float2 amp2 = math.sin(time + settings.frequency/math.PI * t  * math.PI*2f);
                //     float3 p0 = forward*t.x + up*amp.x + right*amp2.x;
                //     float3 p1 = forward*t.y + up*amp.y + right*amp2.y;
                //     buffer[i] = new Segments.Segment(p0, p1);
                // }
                {
                    float t0 = i * step;
                    float t1 = t0 + step;

                    float amp0 = math.sin(time + freqTau * t0);
                    float amp1 = math.sin(time + freqTau * t1);
                    float amp2 = math.sin(time + freqPiTau * t0);
                    float amp3 = math.sin(time + freqPiTau * t1);

                    float3 p0 = new float3(0,0,mag.z*t0) + new float3(0,mag.y*amp0,0) + new float3(mag.x*amp2,0,0);
                    float3 p1 = new float3(0,0,mag.z*t1) + new float3(0,mag.y*amp1,0) + new float3(mag.x*amp3,0,0);
                    buffer[i] = new Segments.Segment(p0, p1);
                }
            }
        }
    }

    [WorldSystemFilter( WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor )]
    [UpdateInGroup( typeof(PresentationSystemGroup) )]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct StressTestSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        public void OnUpdate ( ref SystemState state )
        {
            state.Dependency = new StressTestJob{
                time = Time.time,// note: picked Time.time here **only** because it happen to change outside play mode where SystemAPI.Time.ElapsedTime is play mode only
            }.ScheduleParallel(state.Dependency);
        }
    }

}
