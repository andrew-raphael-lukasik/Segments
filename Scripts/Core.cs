using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Segments.Internal;

namespace Segments
{
	public static class Core
	{
		

		internal static List<Batch> Batches = new List<Batch>();


		static World world;


		public static World GetWorld ()
		{
			if( world!=null && world.IsCreated )
				return world;
			else
			{
				world = World.DefaultGameObjectInjectionWorld;
				
				#if UNITY_EDITOR
				if( world==null )
				{
					// create editor world:
					world = DefaultWorldInitialization.Initialize( "Editor World" , true );
					// DefaultWorldInitialization.DefaultLazyEditModeInitialize();// not immediate
				}
				#endif

				DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups( world , typeof(SegmentInitializationSystem) , typeof(SegmentRenderingSystem) );
				
				return world;
			}
		}


		public static void CreateBatch ( out Batch batch , Material materialOverride = null )
		{
			GetWorld();// makes sure initialized world exists

			if( materialOverride==null )
				materialOverride = Internal.ResourceProvider.default_material;
			
			var buffer = new NativeList<float3x2>( Allocator.Persistent );
			batch = new Batch(
				mat:		materialOverride ,
				buffer:		buffer
			);
			Batches.Add( batch );
		}


		public static void DestroyAllBatches ()
		{
			for( int i=Batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = Batches[i];
				batch.Dependency.Complete();
				batch.DisposeImmediate();
				
				Batches.RemoveAt(i);
			}
			Assert.AreEqual( Batches.Count , 0 );
		}


		public static void Render ( Camera camera , MaterialPropertyBlock materialPropertyBlock = null )
		{
			for( int i=Batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = Batches[i];
				Graphics.DrawMesh( batch.mesh , Vector3.zero , quaternion.identity , batch.material , 0 , camera , 0 , materialPropertyBlock , false , true , true );
			}
		}


	}
}
