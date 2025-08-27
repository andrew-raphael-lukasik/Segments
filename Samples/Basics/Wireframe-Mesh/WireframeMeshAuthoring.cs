using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Transforms;

namespace Samples
{
    [ExecuteAlways]
    [RequireComponent( typeof(MeshFilter) )]
    class WireframeMeshAuthoring : MonoBehaviour
    {
        [SerializeField] Material _materialOverride;
        Entity _segments;

        void OnEnable()
        {
            // create list of edges:
            NativeArray<float3> vertices;
            NativeArray<int2> edges;
            {
                var mf = GetComponent<MeshFilter>();
                var mesh = mf.sharedMesh;
                vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.TempJob).Reinterpret<float3>();
                var triangles = new NativeArray<int>(mesh.triangles, Allocator.TempJob);
                var job = new ToEdgesJob{
                    Triangles = triangles.AsReadOnly(),
                    Results = new NativeList<int2>(initialCapacity:triangles.Length*3, Allocator.TempJob)
                };
                job.Run();
                edges = job.Results.ToArray(Allocator.TempJob);
                job.Results.Dispose();
                triangles.Dispose();
            }

            // create segment buffer:
            Segments.Core.Create(out _segments, _materialOverride);

            // create local-space line segments:
            {
                var buffer = Segments.Core.GetBuffer(_segments);
                buffer.Length = edges.Length;

                var jobHandle = new UpdateSegmentsJob{
                    Edges       = edges.AsReadOnly(),
                    Vertices    = vertices.AsReadOnly(),
                    Segments    = buffer.AsArray(),
                }.Schedule(arrayLength:edges.Length, innerloopBatchCount:128);

                // pass the job handle so dependency system knows aobut this job (needed when scheduling from Monobehaviours)
                Segments.Core.AddDependency(jobHandle);
            }
        }

        void OnDisable() => Segments.Core.Destroy(_segments);

        void Update()
        {
            // note: we don't change the lines in Update here because mesh is not changing so no reason to recreate the wireframe more than once

            // update transform (because lines are in local-space):
            var entityManager = Segments.Core.GetWorld().EntityManager;
            entityManager.SetComponentData(_segments, new LocalToWorld{
                Value = transform.localToWorldMatrix
            });
        }

        #if UNITY_EDITOR
        void OnDrawGizmos() => Gizmos.DrawIcon(transform.position, "");// draws a white square icon to help with object selection in Scene view
        #endif

        [Unity.Burst.BurstCompile]
        public struct UpdateSegmentsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int2>.ReadOnly Edges;
            [ReadOnly] public NativeArray<float3>.ReadOnly Vertices;
            [WriteOnly] public NativeArray<float3x2> Segments;
            void IJobParallelFor.Execute ( int index )
            {
                int i0 = Edges[index].x;
                int i1 = Edges[index].y;
                Segments[index] = new float3x2(Vertices[i0], Vertices[i1]);
            }
        }

        [Unity.Burst.BurstCompile]
        public struct ToEdgesJob : IJob
        {
            [ReadOnly] public NativeArray<int>.ReadOnly Triangles;
            [WriteOnly] public NativeList<int2> Results;
            void IJob.Execute()
            {
                var edges = new NativeHashMap<ulong,int2>(Triangles.Length*3, Allocator.Temp);
                for( int i=0 ; i<Triangles.Length ; i+=3 )
                {
                    int a = Triangles[i];
                    int b = Triangles[i+1];
                    int c = Triangles[i+2];
                    ulong hash;
                    
                    hash = (ulong)math.max(a,b)*(ulong)1e6 + (ulong)math.min(a,b);
                    if( !edges.ContainsKey(hash) )
                        edges.Add( hash , new int2(a, b) );
                    
                    hash = (ulong)math.max(b,c)*(ulong)1e6 + (ulong)math.min(b,c);
                    if( !edges.ContainsKey(hash) )
                        edges.Add( hash , new int2(b, c) );

                    hash = (ulong)math.max(c,a)*(ulong)1e6 + (ulong)math.min(c,a);
                    if( !edges.ContainsKey(hash) )
                        edges.Add(hash, new int2(c, a));
                }
                Results.AddRange(edges.GetValueArray(Allocator.Temp));
                edges.Dispose();
            }
        }

    }
}
