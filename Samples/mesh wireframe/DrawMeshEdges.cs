﻿using UnityEngine;

using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Samples
{
	[ExecuteAlways]
	[AddComponentMenu("")]
	[RequireComponent( typeof(MeshFilter) )]
	class DrawMeshEdges : MonoBehaviour
	{
		
		[SerializeField] Material _materialOverride = null;

		NativeArray<float3> _vertices;
		NativeArray<int2> _edges;

		Entity _segments;
		EntityManager _entityManager;

		void OnEnable ()
		{
			// create list of edges:
			var mf = GetComponent<MeshFilter>();
			{
				var mesh = mf.sharedMesh;
				_vertices = new NativeArray<Vector3>( mesh.vertices , Allocator.Persistent ).Reinterpret<float3>();
				var triangles = new NativeArray<int>( mesh.triangles , Allocator.TempJob );
				var job = new ToEdgesJob{
					Triangles = triangles.AsReadOnly() ,
					Results = new NativeList<int2>( initialCapacity:triangles.Length*3 , Allocator.Persistent )
				};
				job.Run();
				_edges = job.Results.ToArray( Allocator.Persistent );
				job.Results.Dispose();
				triangles.Dispose();
			}

			// create segment buffer:
			Segments.Core.CreateBatch( out _segments , out _entityManager , _materialOverride );
			
			// initialize buffer size:
			var buffer = Segments.Utilities.GetSegmentBuffer( _segments , _entityManager );
			buffer.Length = _edges.Length;
		}

		void OnDisable ()
		{
			Segments.Core.DestroyBatch( _segments );
			if( _vertices.IsCreated ) _vertices.Dispose();
			if( _edges.IsCreated ) _edges.Dispose();
		}

		void Update ()
		{
			Segments.Core.CompleteDependency();

			var buffer = Segments.Utilities.GetSegmentBuffer( _segments , _entityManager );
			var jobHandle = new UpdateSegmentsJob{
				Edges		= _edges.AsReadOnly() ,
				Vertices	= _vertices.AsReadOnly() ,
				Transform	= transform.localToWorldMatrix ,
				Segments	= buffer.AsNativeArray() ,
			}.Schedule( arrayLength:_edges.Length , innerloopBatchCount:128 );
			
			Segments.Core.AddDependency( jobHandle );
		}

		[BurstCompile]
		public struct UpdateSegmentsJob : IJobParallelFor
		{
			[ReadOnly] public NativeArray<int2>.ReadOnly Edges;
			[ReadOnly] public NativeArray<float3>.ReadOnly Vertices;
			[ReadOnly] public float4x4 Transform;
			[WriteOnly] public NativeArray<float3x2> Segments;
			//public BufferLookup<Segments.Segment> SegmentBufferLookup;
			void IJobParallelFor.Execute ( int index )
			{
				//var buffer = SegmentBufferLookup[ default ];

				int i0 = Edges[index].x;
				int i1 = Edges[index].y;
				float4 p0 = math.mul( Transform , new float4( Vertices[i0] , 1 ) );
				float4 p1 = math.mul( Transform , new float4( Vertices[i1] , 1 ) );
				Segments[index] = new float3x2{
					c0	= new float3{ x=p0.x , y=p0.y , z=p0.z } ,
					c1	= new float3{ x=p1.x , y=p1.y , z=p1.z }
				};
			}
		}

		[BurstCompile]
		public struct ToEdgesJob : IJob
		{
			[ReadOnly] public NativeArray<int>.ReadOnly Triangles;
			[WriteOnly] public NativeList<int2> Results;
			void IJob.Execute ()
			{
				var edges = new NativeHashMap<ulong,int2>( Triangles.Length*3 , Allocator.Temp );
				for( int i=0 ; i<Triangles.Length ; i+=3 )
				{
					int a = Triangles[i];
					int b = Triangles[i+1];
					int c = Triangles[i+2];
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
				Results.AddRange( edges.GetValueArray(Allocator.Temp) );
				edges.Dispose();
			}
		}
		
	}
}
