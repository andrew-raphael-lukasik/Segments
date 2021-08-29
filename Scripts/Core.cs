using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;

namespace Segments
{
	public static class Core
	{
		

		internal static List<Batch> Batches = new List<Batch>();
		static Material default_material { get; set; }

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

				// load default material asset:
				if( default_material==null )
				{
					const string path = "packages/Segments/default";
					default_material = UnityEngine.Resources.Load<Material>( path );
					if( default_material!=null )
						default_material.hideFlags = HideFlags.DontUnloadUnusedAsset;
					else
						Debug.LogWarning($"loading Material asset failed, path: \'{path}\'");
				}
				
				return world;
			}
		}


		public static void CreateBatch ( out Batch batch , Material materialOverride = null )
		{
			GetWorld();// makes sure initialized world exists

			if( materialOverride==null )
				materialOverride = default_material;
			Assert.IsNotNull( materialOverride , $"{nameof(materialOverride)} is null" );
			
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
