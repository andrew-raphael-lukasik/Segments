using UnityEngine;

using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace Segments.Samples
{
	[AddComponentMenu("")]
	[RequireComponent( typeof(MeshFilter) )]
	class DrawMeshEdges : MonoBehaviour
	{
		
		[SerializeField] Material _materialOverride = null;
		[SerializeField] float _widthOverride = 0.003f;

		NativeList<float3x2> _segments;
		NativeArray<Vector3> _vertices;
		NativeArray<int2> _edges;
		Segments.NativeListToSegmentsSystem _segmentsSystem;
		public JobHandle Dependency;


		void OnEnable ()
		{
			// create list of edges:
			var mf = GetComponent<MeshFilter>();
			{
				var mesh = mf.sharedMesh;
				_vertices = new NativeArray<Vector3>( mesh.vertices , Allocator.Persistent );
				var triangles = new NativeArray<int>( mesh.triangles , Allocator.TempJob );
				var job = new ToEdgesJob{
					triangles = triangles.AsReadOnly() ,
					results = new NativeList<int2>( initialCapacity:triangles.Length*3 , Allocator.Persistent )
				};
				job.Run();
				_edges = job.results.ToArray( Allocator.Persistent );
				job.results.Dispose();
				triangles.Dispose();
			}

			var world = Segments.Core.GetWorld();
			_segmentsSystem = world.GetExistingSystem<Segments.NativeListToSegmentsSystem>();

			// initialize segment list:
			Entity prefab;
			if( _materialOverride!=null )
			{
				if( _widthOverride>0f ) prefab = Segments.Core.GetSegmentPrefabCopy( _materialOverride , _widthOverride );
				else prefab = Segments.Core.GetSegmentPrefabCopy( _materialOverride );
			}
			else
			{
				if( _widthOverride>0f ) prefab = Segments.Core.GetSegmentPrefabCopy( _widthOverride );
				else prefab = Segments.Core.GetSegmentPrefabCopy();
			}
			_segmentsSystem.CreateBatch( prefab , out _segments );
			_segments.Length = _edges.Length;
		}

		void OnDisable ()
		{
			Dependency.Complete();
			if( _segments.IsCreated ) _segments.Dispose();
			if( _vertices.IsCreated ) _vertices.Dispose();
			if( _edges.IsCreated ) _edges.Dispose();
		}

		void Update ()
		{
			Dependency.Complete();

			var job = new UpdateSegmentsJob{
				edges		= _edges.AsReadOnly() ,
				vertices	= _vertices.AsReadOnly() ,
				matrix		= transform.localToWorldMatrix ,
				segments	= _segments
			};
			Dependency = job.Schedule( arrayLength:_edges.Length , innerloopBatchCount:128 );
			
			_segmentsSystem.Dependencies.Add( Dependency );
		}

		public struct UpdateSegmentsJob : IJobParallelFor
		{
			[ReadOnly] public NativeArray<int2>.ReadOnly edges;
			[ReadOnly] public NativeArray<Vector3>.ReadOnly vertices;
			[ReadOnly] public float4x4 matrix;
			[WriteOnly] public NativeArray<float3x2> segments;
			void IJobParallelFor.Execute ( int index )
			{
				int i0 = edges[index].x;
				int i1 = edges[index].y;
				float4 p0 = math.mul( matrix , new float4( vertices[i0] , 0 ) );
				float4 p1 = math.mul( matrix , new float4( vertices[i1] , 0 ) );
				segments[index] = new float3x2{
					c0	= new float3{ x=p0.x , y=p0.y , z=p0.z } ,
					c1	= new float3{ x=p1.x , y=p1.y , z=p1.z }
				};
			}
		}

		[Unity.Burst.BurstCompile]
		public struct ToEdgesJob : IJob
		{
			[ReadOnly] public NativeArray<int>.ReadOnly triangles;
			[WriteOnly] public NativeList<int2> results;
			void IJob.Execute ()
			{
				var edges = new NativeHashMap<ulong,int2>( capacity:triangles.Length*3 , Allocator.Temp );
				for( int i=0 ; i<triangles.Length ; i+=3 )
				{
					int a = triangles[i];
					int b = triangles[i+1];
					int c = triangles[i+2];
					ulong hash;
					
					hash = (ulong)math.max(a,b)*(ulong)1e6 + (ulong)math.min(a,b);
					if( !edges.ContainsKey(hash) )
						edges.Add( hash , new int2{ x=a , y=b } );
					
					hash = (ulong)math.max(b,c)*(ulong)1e6 + (ulong)math.min(b,c);
					if( !edges.ContainsKey(hash) )
						edges.Add( hash , new int2{ x=b , y=c } );

					hash = (ulong)math.max(c,a)*(ulong)1e6 + (ulong)math.min(c,a);
					if( !edges.ContainsKey(hash) )
						edges.Add( hash , new int2{ x=c , y=a } );
				}
				results.AddRange( edges.GetValueArray(Allocator.Temp) );
				edges.Dispose();
			}
		}
		
		
	}
}
