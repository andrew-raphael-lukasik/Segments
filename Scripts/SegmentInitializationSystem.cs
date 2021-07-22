using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;

namespace Segments
{
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(InitializationSystemGroup) )]
	internal class SegmentInitializationSystem : SystemBase
	{

		protected override void OnDestroy ()
		{
			Dependency.Complete();
			Core.DestroyAllBatches();
		}


		protected override void OnUpdate ()
		{
			var batches = Core.Batches;

			// remove disposed batches from the list:
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
				if( batches[i].isDisposed )
					batches.RemoveAt(i);
			int numBatches = batches.Count;

			// combine dependencies
			// {
			// 	var deps = new NativeArray<JobHandle>( numBatches+1 , Allocator.Temp );
			// 	for( int i=0 ; i<numBatches ; i++ )
			// 		deps[i] = _batches[i].Dependency;
			// 	deps[numBatches] = Dependency;
			// 	Dependency = JobHandle.CombineDependencies( deps );
			// }
			// if( numBatches==0 ) return;

			// update meshes for rendering:
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[ i ];
				batch.Dependency.Complete();

				NativeArray<float3x2> buffer = batch.buffer;
				Mesh mesh = batch.mesh;
				int bufferSize = buffer.Length;
				int numVertices = buffer.Length * 2;
				int numIndices = numVertices;

				var indices = new NativeArray<uint>( numIndices , Allocator.TempJob );
				var indicesJob = new IndicesJob{ Indices=indices }.Schedule( indices.Length , 1024 );
				mesh.SetVertexBufferParams( numVertices , Batch.layout );
				mesh.SetVertexBufferData( buffer , 0 , 0 , buffer.Length );
				mesh.SetIndexBufferParams( numIndices , IndexFormat.UInt32 );
				indicesJob.Complete();
				mesh.SetIndexBufferData( indices , 0 , 0 , numIndices );
				indices.Dispose();
				if( mesh.GetSubMesh(0).indexCount!=numIndices )
				{
					mesh.SetSubMesh(
						index:	0 ,
						desc:	new SubMeshDescriptor( indexStart:0 , indexCount:numIndices , topology:MeshTopology.Lines ) ,
						flags:	MeshUpdateFlags.DontValidateIndices
					);
				}
				mesh.RecalculateBounds();
				mesh.UploadMeshData( false );
			}
		}


		[Unity.Burst.BurstCompile]
		public struct IndicesJob : IJobParallelFor
		{
			[WriteOnly] public NativeArray<uint> Indices;
			void IJobParallelFor.Execute ( int index ) => Indices[index] = (uint) index;
		}


	}
}
