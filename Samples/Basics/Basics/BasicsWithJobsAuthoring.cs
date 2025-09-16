using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Samples
{
    /// <summary>
    /// This is how you make line-plotting faster with Burst and Jobs.
    /// </summary>
    [ExecuteAlways]
    public class BasicsWithJobsAuthoring : MonoBehaviour
    {
        Entity _segments;

        void OnEnable()
        {
            Segments.Core.Create(out _segments);

            // create local-space line segments:
            {
                // accesses the Segment component of our Entity
                Segments.Segment segment = Segments.Core.GetSegment(_segments);

                // we already know ahead of time that we want 3 line segments here
                segment.Buffer.Length = 3;

                segment.Dependency.Value = new MyBasicJob{
                    buffer          = segment.Buffer.AsArray() ,
                }.Schedule(segment.Dependency.Value);
            }
        }
        void OnDisable() => Segments.Core.Destroy(_segments);

        void Update()
        {
            // update transform so lines follow it (because lines are in local-space)
            Segments.Core.GetWorld().EntityManager.SetComponentData(_segments, new LocalToWorld{
                Value = transform.localToWorldMatrix
            });
        }

        [Unity.Burst.BurstCompile]
        struct MyBasicJob : IJob
        {
            [WriteOnly] public NativeArray<float3x2> buffer;
            void IJob.Execute()
            {
                buffer[0] = new float3x2(float3.zero, new float3(1, 0, 0));
                buffer[1] = new float3x2(float3.zero, new float3(0, 1, 0));
                buffer[2] = new float3x2(float3.zero, new float3(0, 0, 1));
            }
        }

        #if UNITY_EDITOR
        void OnDrawGizmos() => Gizmos.DrawIcon(transform.position, "");// draws a white square icon to help with object selection in Scene view
        #endif

    }
}
