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


		protected override void OnDestroy ()
		{
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

			// var dependencies = new NativeList<JobHandle>( initialCapacity:numBatches+1 , Allocator.Temp );
			for( int batchIndex=numBatches-1 ; batchIndex!=-1 ; batchIndex-- )
			{
				var batch = _batches[ batchIndex ];
				NativeList<float3x2> buffer = batch.Segments;

				// int bufferSize = buffer.Length;// throws dependency errors
				int bufferSize = buffer.AsParallelReader().Length;

				var _materialProperties = new MaterialPropertyBlock();
				
				batch.Dependency.Complete();
				ConvertArray( buffer , ref batch.shaderData );
				_materialProperties.SetFloatArray( "_ArrayVertices" , batch.shaderData );
				Graphics.DrawProcedural(
					material:		batch.material ,
					bounds:			new Bounds( Vector3.zero , Vector3.one*100 ) ,
					topology:		MeshTopology.Lines ,
					vertexCount:	bufferSize , 
					instanceCount:	1 ,
					camera:			null ,
					properties:		_materialProperties ,
					castShadows:	ShadowCastingMode.Off ,
					receiveShadows:	false ,
					layer:			0
				);
			}
			// if( dependencies.Length!=0 )
			// {
			// 	dependencies.Add( Dependency );
			// 	Dependency = JobHandle.CombineDependencies( dependencies );
			// 	// dependencies.Clear();
			// }

			// Graphics.DrawProcedural()

			// CommandBuffer cmd = new CommandBuffer();
			// cmd.SetComputeBufferData(  );
			// Graphics.ExecuteCommandBuffer( cmd );
		}


		public void CreateBatch ( out Batch batch , Material materialOverride = null )
		{
			if( materialOverride==null )
				materialOverride = Internal.ResourceProvider.default_material;
			
			var buffer = new NativeList<float3x2>( Allocator.Persistent );
			batch = new Batch(
				material:	materialOverride ,
				buffer:		buffer
			);
			_batches.Add( batch );
		}


		internal static unsafe void ConvertArray ( NativeArray<float3x2> input , ref float[] output )
		{
			void* unsafeInput = NativeArrayUnsafeUtility.GetUnsafePtr( input );
			int len = input.Length * 3 * 2;
			if( output.Length!=len ) output = new float[ len ];
			void* outputPtr = UnsafeUtility.PinGCArrayAndGetDataAddress( output , out ulong handle );
			UnsafeUtility.MemCpy( destination: outputPtr , source: unsafeInput , size: input.Length * UnsafeUtility.SizeOf<float3x2>() );
			UnsafeUtility.ReleaseGCObject( handle );
		}


	}
}
