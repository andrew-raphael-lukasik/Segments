using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
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

			// shedule indices job:
			Profiler.BeginSample("shedule_indices_job");
			int numAllIndices = 0;
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
				numAllIndices = math.max( numAllIndices , batches[i].buffer.Length*2 );
			var allIndices = new NativeArray<uint>( numAllIndices , Allocator.TempJob );
			var allIndicesJobHandle = new IndicesJob{ Ptr=(uint*)allIndices.GetUnsafePtr() }.Schedule( allIndices.Length , 1024 );
			Profiler.EndSample();

			// shedule indices job:
			Profiler.BeginSample("shedule_bounds_job");
			NativeArray<Bounds> allBounds = new NativeArray<Bounds>( batches.Count , Allocator.TempJob );
			NativeArray<JobHandle> allBoundsJobHandles = new NativeArray<JobHandle>( batches.Count , Allocator.Temp );
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer;
				allBoundsJobHandles[i] = new BoundsJob( buffer , allBounds , i ).Schedule();
			}
			Profiler.EndSample();

			// remove disposed batches from the list:
			Profiler.BeginSample("remove_disposed");
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
				if( batches[i].isDisposed )
					batches.RemoveAt(i);
			Profiler.EndSample();
			
			// complete all dependencies:
			Profiler.BeginSample("complete_dependencies");
			NativeArray<JobHandle> dependencies = new NativeArray<JobHandle>( batches.Count , Allocator.Temp );
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
				dependencies[i] = batches[i].Dependency;
			JobHandle.CompleteAll( dependencies );
			Profiler.EndSample();

			// push vertices:
			Profiler.BeginSample("push_vertices");
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer;
				Mesh mesh = batch.mesh;
				int numVertices = buffer.Length * 2;
				
				mesh.SetVertexBufferParams( numVertices , _layout );
				mesh.SetVertexBufferData( buffer , 0 , 0 , buffer.Length );
			}
			Profiler.EndSample();

			allIndicesJobHandle.Complete();

			// push indices:
			Profiler.BeginSample("push_indices");
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer;
				Mesh mesh = batch.mesh;
				int numVertices = buffer.Length * 2;
				int numIndices = numVertices;
				var indices = AsArray<int>( allIndices.GetUnsafePtr() , numIndices );
				
				mesh.SetIndexBufferParams( numIndices , IndexFormat.UInt32 );
				mesh.SetIndexBufferData( indices , 0 , 0 , numIndices );
				mesh.SetSubMesh( 0 , new SubMeshDescriptor( indexStart:0 , indexCount:numIndices , topology:MeshTopology.Lines ) );
			}
			Profiler.EndSample();

			JobHandle.CompleteAll( allBoundsJobHandles );

			// push bounds:
			Profiler.BeginSample("push_bounds");
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
				batches[i].mesh.bounds = allBounds[i];
			Profiler.EndSample();

			allIndices.Dispose();
			allBounds.Dispose();
		}

		public unsafe NativeArray<T> AsArray <T> ( void* dataPtr , int dataLength ) where T : unmanaged
		{
			var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>( dataPtr , dataLength , Allocator.None );
			#if ENABLE_UNITY_COLLECTIONS_CHECKS
			NativeArrayUnsafeUtility.SetAtomicSafetyHandle( ref nativeArray , AtomicSafetyHandle.Create() );
			#endif
			return nativeArray;
		}


	}


	[BurstCompile]
	unsafe struct IndicesJob : IJobParallelFor
	{
		[NativeDisableUnsafePtrRestriction][WriteOnly] public uint* Ptr;
		void IJobParallelFor.Execute ( int index ) => Ptr[index] = (uint) index;
	}


	[BurstCompile]
	unsafe struct BoundsJob : IJob
	{
		[NativeDisableUnsafePtrRestriction][ReadOnly] float3x2* InputPtr;
		[ReadOnly] int InputLength;
		[NativeDisableUnsafePtrRestriction][WriteOnly] Bounds* OutputPtr;
		public BoundsJob ( NativeArray<float3x2> input ,  NativeArray<Bounds> output , int outputIndex )
		{
			this.InputPtr = (float3x2*) input.GetUnsafePtr();
			this.InputLength = input.Length;
			this.OutputPtr = ((Bounds*) output.GetUnsafePtr())+outputIndex;
		}
		void IJob.Execute ()
		{
			MinMaxAABB combined = MinMaxAABB.Empty;
			for( int i=InputLength-1 ; i!=-1 ; i-- )
			{
				combined.Encapsulate( InputPtr[i].c0 );
				combined.Encapsulate( InputPtr[i].c1 );
			}
			*OutputPtr = new Bounds{ min=combined.Min , max=combined.Max };
		}
	}

	
}
