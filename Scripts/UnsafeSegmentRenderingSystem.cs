using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;

namespace Segments
{
	[WorldSystemFilter( 0 )]
	[AlwaysUpdateSystem]
	[UpdateInGroup( typeof(UpdatePresentationSystemGroup) )]
	public class UnsafeSegmentRenderingSystem : SystemBase
	{


		List<UnsafeBatch> _batches = new List<UnsafeBatch>();


		protected override void OnCreate ()
		{
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}


		protected override void OnDestroy ()
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

			Dependency.Complete();

			for( int i=_batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = _batches[i];
				batch.Dependency.Complete();
				batch.Dispose();
				
				_batches.RemoveAt(i);
			}
			Assert.AreEqual( _batches.Count , 0 );
		}


		protected override void OnUpdate ()
		{
			// remove disposed batches from the list:
			for( int i=_batches.Count-1 ; i!=-1 ; i-- )
				if( _batches[i].isDisposed )
					_batches.RemoveAt(i);
			int numBatches = _batches.Count;

			// update meshes for rendering:
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = _batches[ i ];
				batch.Dependency.Complete();

				var buffer = batch.buffer;
				Mesh mesh = batch.mesh;
				int numVertices = buffer.length * 2;
				int numIndices = numVertices;

				var indices = new NativeArray<uint>( numIndices , Allocator.TempJob );
				var indicesJob = new IndicesJob{ Indices=indices }.Schedule( indices.Length , 1024 );
				mesh.SetVertexBufferParams( numVertices , UnsafeBatch.layout );
				mesh.SetVertexBufferData( buffer.AsArray() , 0 , 0 , buffer.length );

				mesh.SetIndexBufferParams( numIndices , IndexFormat.UInt32 );
				indicesJob.Complete();
				mesh.SetIndexBufferData( indices , 0 , 0 , numIndices );
				indices.Dispose();
				mesh.SetSubMesh(
					index:	0 ,
					desc:	new SubMeshDescriptor( indexStart:0 , indexCount:numIndices , topology:MeshTopology.Lines ) ,
					flags:	MeshUpdateFlags.DontValidateIndices
				);
				mesh.RecalculateBounds();
				mesh.UploadMeshData( false );
			}
		}

		void OnBeginCameraRendering ( ScriptableRenderContext context , Camera camera )
		{
			#if UNITY_EDITOR
			if( camera.name=="Preview Scene Camera" ) return;
			#endif

			var propertyBlock = new MaterialPropertyBlock{};
			for( int i=_batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = _batches[i];
				Graphics.DrawMesh( batch.mesh , Vector3.zero , quaternion.identity , batch.material , 0 , camera , 0 , propertyBlock , false , true , true );
			}
		}


		public unsafe void CreateBatch ( out UnsafeBatch batch , Material materialOverride = null )
		{
			if( materialOverride==null )
				materialOverride = Internal.ResourceProvider.default_material;
			
			var buffer = new VeryUnsafeList<float3x2>( initialCapacity:32 , Allocator.Persistent );
			
			batch = new UnsafeBatch(
				mat:		materialOverride ,
				buffer:		buffer
			);
			_batches.Add( batch );
		}


		[Unity.Burst.BurstCompile]
		public struct IndicesJob : IJobParallelFor
		{
			[WriteOnly] public NativeArray<uint> Indices;
			void IJobParallelFor.Execute ( int index ) => Indices[index] = (uint) index;
		}


	}
}
