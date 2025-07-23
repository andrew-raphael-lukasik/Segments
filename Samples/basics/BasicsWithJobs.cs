using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Entities;

namespace Samples
{
    /// <summary>
    /// This is how you make line-plotting faster with Burst and Jobs.
    /// </summary>
    [ExecuteAlways]
    public class BasicsWithJobs : MonoBehaviour
    {
        Entity _segments;

        void OnEnable () => Segments.Core.Create( out _segments );
        void OnDisable () => Segments.Core.Destroy( _segments );

        void Update ()
        {
            // accesses the Segment beffer component of our Entity where every Segment is a pair of float3 values (start & end of a line segment)
            var segments = Segments.Core.GetBuffer( _segments );

            // we already know ahead of time that we want 3 segments here
            segments.Length = 3;

            var jobHandle = new MyBasicJob{
                SegmentBuffer   = segments.AsNativeArray() ,
                LocalToWorld    = transform.localToWorldMatrix// this matrix holds directions (scale per axis) and position of the transform
            }.Schedule();
            
            Segments.Core.AddDependency( jobHandle );
        }

        [Unity.Burst.BurstCompile]
        struct MyBasicJob : IJob
        {
            [WriteOnly] public NativeArray<float3x2> SegmentBuffer;
            public float4x4 LocalToWorld;
            void IJob.Execute ()
            {
                // chops the matrix up into separate collumns
                float4 c0 = LocalToWorld.c0;// stores x direction
                float4 c1 = LocalToWorld.c1;// stores y direction
                float4 c2 = LocalToWorld.c2;// stores z direction
                float4 c3 = LocalToWorld.c3;// stores position

                // converts float4s to float3s, names for convenience
                float3 pos    = new float3( c3.x , c3.y , c3.z );
                float3 right    = new float3( c0.x , c0.y , c0.z );
                float3 up        = new float3( c1.x , c1.y , c1.z );
                float3 forward    = new float3( c2.x , c2.y , c2.z );

                // set points where all these segments will start and end
                SegmentBuffer[0] = new float3x2( pos , pos+right );
                SegmentBuffer[1] = new float3x2( pos , pos+up );
                SegmentBuffer[2] = new float3x2( pos , pos+forward );
            }
        }

        #if UNITY_EDITOR
        void OnDrawGizmos () => Gizmos.DrawIcon(transform.position, "");// draws a white square icon to help with object selection in Scene view
        #endif

    }
}
