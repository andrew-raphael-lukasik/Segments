using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

namespace Samples
{
    [ExecuteAlways]
    class StressTests : MonoBehaviour
    {

        [SerializeField] Material _srcMaterial = null;
        [SerializeField] int _numSegments = 128;
        [SerializeField] float _frequency = 16;
        [SerializeField] bool _everyFrame = false;

        Entity _segments;

        void OnEnable () => Segments.Core.Create( out _segments , _srcMaterial );
        void OnDisable () => Segments.Core.Destroy( _segments );

        void Update ()
        {
            var segments = Segments.Core.GetBuffer( _segments );
            if( segments.Length!=_numSegments || _everyFrame )
            {
                segments.Length = _numSegments;

                // schedule new job:
                var jobHandle = new StressTestJob
                {
                    Transform    = transform.localToWorldMatrix ,
                    NumSegments    = _numSegments ,
                    Segments    = segments.AsNativeArray() ,
                    Offset        = Time.time ,
                    Frequency    = _frequency ,
                }.Schedule( arrayLength: _numSegments , indicesPerJobCount: 64 );

                Segments.Core.AddDependency( jobHandle );
            }
        }

        #if UNITY_EDITOR
        void OnDrawGizmos () => Gizmos.DrawIcon(transform.position, "");// draws a white square icon to help with object selection in Scene view
        #endif

        [Unity.Burst.BurstCompile]
        public struct StressTestJob : IJobParallelForBatch
        {
            public float4x4 Transform;
            public int NumSegments;
            public float Offset;
            public float Frequency;
            [WriteOnly] public NativeArray<float3x2> Segments;
            void IJobParallelForBatch.Execute ( int startIndex , int count )
            {
                float3 pos = new float3(Transform.c3.x , Transform.c3.y , Transform.c3.z);
                float3 right = Transform.Right();
                float3 up = Transform.Up();
                float3 forward = Transform.Forward();
                for( int i = 0 ; i<count ; i++ )
                {
                    int index = startIndex + i;
                    float2 t = new float2(index, index+1) / new float2(NumSegments, NumSegments);
                    float2 a = math.sin(new float2(Offset, Offset) + Frequency * t * new float2(math.PI*2f, math.PI*2f));
                    float2 a2 = math.sin(new float2(Offset, Offset) + Frequency/math.PI * t  * new float2(math.PI*2f, math.PI*2f));
                    float3 vec0 = pos + forward*t[0] + up*a[0] + right*a2[0];
                    float3 vec1 = pos + forward*t[1] + up*a[1] + right*a2[1];
                    Segments[index] = new float3x2(vec0, vec1);
                }
            }
        }

    }
}
