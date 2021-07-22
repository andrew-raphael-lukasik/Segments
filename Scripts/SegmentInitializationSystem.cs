using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using BurstCompile = Unity.Burst.BurstCompileAttribute;

namespace Segments
{
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(InitializationSystemGroup) )]
	internal class SegmentInitializationSystem : SystemBase
	{


		static readonly VertexAttributeDescriptor[] _layout = new[]{ new VertexAttributeDescriptor( VertexAttribute.Position , VertexAttributeFormat.Float32 , 3 ) };


		protected override void OnDestroy ()
		{
			Dependency.Complete();
			Core.DestroyAllBatches();
		}


		protected override unsafe void OnUpdate ()
		{
			var batches = Core.Batches;

			// remove disposed batches from the list:
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
				if( batches[i].isDisposed )
					batches.RemoveAt(i);
			
			// complete all dependencies:
			NativeArray<JobHandle> dependencies = new NativeArray<JobHandle>( batches.Count , Allocator.Temp );
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
				dependencies[i] = batches[i].Dependency;
			JobHandle.CompleteAll( dependencies );

			// update vertices:
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer;
				Mesh mesh = batch.mesh;
				int numVertices = buffer.Length * 2;
				
				mesh.SetVertexBufferParams( numVertices , _layout );
				mesh.SetVertexBufferData( buffer , 0 , 0 , buffer.Length );
			}

			// update indices:
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer;
				Mesh mesh = batch.mesh;
				int numVertices = buffer.Length * 2;
				int numIndices = numVertices;
				var indices = new NativeArray<uint>( numIndices , Allocator.TempJob );
				var indicesJob = new IndicesJob{ Ptr=(uint*)indices.GetUnsafePtr() }.Schedule( indices.Length , 1024 );
				
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
			}

			// final updates:
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
			{
				Mesh mesh = batches[i].mesh;
				mesh.RecalculateBounds();
				mesh.UploadMeshData( false );
			}
		}


		[BurstCompile]
		public unsafe struct IndicesJob : IJobParallelFor
		{
			[NativeDisableUnsafePtrRestriction][WriteOnly] public uint* Ptr;
			void IJobParallelFor.Execute ( int index ) => Ptr[index] = (uint) index;
		}


	}
}
