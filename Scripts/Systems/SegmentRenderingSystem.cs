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
	[UpdateInGroup( typeof(UpdatePresentationSystemGroup) )]
	public class SegmentRenderingSystem : SystemBase
	{


		List<Batch> _batches = new List<Batch>();
		bool _isSystemDestroyed = false;


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

			_isSystemDestroyed = true;
		}


		protected override void OnUpdate ()
		{
			for( int i=_batches.Count-1 ; i!=-1 ; i-- )
				if( _batches[i].isDisposed )
					_batches.RemoveAt(i);

			int numBatches = _batches.Count;
			{
				var deps = new NativeArray<JobHandle>( numBatches+1 , Allocator.Temp );
				for( int i=0 ; i<numBatches ; i++ )
					deps[i] = _batches[i].Dependency;
				deps[numBatches] = Dependency;
				Dependency = JobHandle.CombineDependencies( deps );
			}
			if( numBatches==0 ) return;

			var entityManager = EntityManager;
			var segmentData = GetComponentDataFromEntity<Segment>( isReadOnly:false );

			for( int batchIndex=numBatches-1 ; batchIndex!=-1 ; batchIndex-- )
			{
				var batch = _batches[ batchIndex ];
				NativeList<float3x2> buffer = batch.buffer;
				Mesh mesh = batch.mesh;

				// int bufferSize = buffer.Length;// throws dependency errors
				int bufferSize = buffer.AsParallelReader().Length;
				
				batch.Dependency.Complete();

				int numVertices = buffer.Length * 2;
				mesh.SetVertexBufferParams( numVertices , Batch.layout );
				mesh.SetVertexBufferData( buffer.AsArray() , 0 , 0 , buffer.Length );
				// var tmp = new NativeArray<float3>( numVertices , Allocator.Temp );
				// for( int i=0 ; i<numVertices ; i++ ) tmp[i] = (float3) UnityEngine.Random.onUnitSphere;
				// mesh.SetVertexBufferData( tmp , 0 , 0 , tmp.Length );

				int numIndices = numVertices;
				var indices = new NativeArray<uint>( numIndices , Allocator.TempJob );
				new IndicesJob{ indices = indices }
					.Schedule( indices.Length , 1024 )
					.Complete();
				mesh.SetIndexBufferParams( numIndices , IndexFormat.UInt32 );
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


		public void CreateBatch ( out Batch batch , Material materialOverride = null )
		{
			if( materialOverride==null )
				materialOverride = Internal.ResourceProvider.default_material;
			
			var buffer = new NativeList<float3x2>( Allocator.Persistent );
			batch = new Batch(
				mat:		materialOverride ,
				buffer:		buffer
			);
			_batches.Add( batch );
		}


		internal static unsafe void ConvertArray ( NativeArray<float3x2> src , ref float[] dst )
		{
			void* unsafeInput = NativeArrayUnsafeUtility.GetUnsafePtr( src );
			int len = src.Length * 3 * 2;
			if( dst.Length!=len ) dst = new float[ len ];
			void* outputPtr = UnsafeUtility.PinGCArrayAndGetDataAddress( dst , out ulong handle );
			UnsafeUtility.MemCpy( destination: outputPtr , source: unsafeInput , size: src.Length * UnsafeUtility.SizeOf<float3x2>() );
			UnsafeUtility.ReleaseGCObject( handle );
		}

		internal static unsafe void CopyData ( NativeArray<float3x2> src , ref NativeList<float3> dst )
		{
			void* inputPtr = NativeArrayUnsafeUtility.GetUnsafePtr( src );
			void* outputPtr = NativeListUnsafeUtility.GetUnsafePtr( dst );

			int numVectors = src.Length * 2;
			if( dst.Length!=numVectors ) dst.Length = numVectors;

			UnsafeUtility.MemCpy( destination: outputPtr , source: inputPtr , size: src.Length * UnsafeUtility.SizeOf<float3x2>() );
		}


		[Unity.Burst.BurstCompile]
		public struct IndicesJob : IJobParallelFor
		{
			[WriteOnly] public NativeArray<uint> indices;
			void IJobParallelFor.Execute ( int index ) => indices[index] = (uint) index;
		}


	}
}
