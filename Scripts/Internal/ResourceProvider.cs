using UnityEngine;

namespace EcsLineRenderer.Internal
{
	public static class ResourceProvider
	{

		public static Mesh default_mesh { get; private set; }
		public static Material default_material { get; private set; }
		public static SegmentMaterialOverride default_segment_material { get; private set; }

		static ResourceProvider ()
		{
			// load default mesh asset:
			if( default_mesh==null )
			{
				const string path = "ECSLineRenderer/default-mesh";
				default_mesh = UnityEngine.Resources.Load<Mesh>( path );
				if( default_mesh!=null )
					default_mesh.hideFlags |= HideFlags.DontUnloadUnusedAsset;
				else
					Debug.LogWarning($"loading Mesh asset failed, path: \'{path}\'");
			}

			// load default material asset:
			if( default_material==null )
			{
				const string path = "ECSLineRenderer/default-line";
				default_material = UnityEngine.Resources.Load<Material>( path );
				if( default_material!=null )
					default_material.hideFlags |= HideFlags.DontUnloadUnusedAsset;
				else
					Debug.LogWarning($"loading Material asset failed, path: \'{path}\'");

				// create default segement material:
				default_segment_material = default_material;
			}
		}

		/*------------------------------------------------------------------------------------------------------
			Uncomment if quad mesh asset needs to be recreated
		------------------------------------------------------------------------------------------------------*/
		// #if UNITY_EDITOR
		// [UnityEditor.MenuItem("ECS LineRenderer/Create Mesh")]
		// static void SaveMesh ()
		// {
		// 	var mesh = new Mesh();
		// 	mesh.name = "quad 1x1, pivot at bottom center";
		// 	mesh.vertices = new Vector3[4]{ new Vector3{ x=-0.5f } , new Vector3{ x=0.5f } , new Vector3{ x=-0.5f , z=1 } , new Vector3{ x=0.5f , z=1 } };
		// 	mesh.triangles = new int[6]{ 0 , 2 , 1 , 2 , 3 , 1 };
		// 	mesh.normals = new Vector3[4]{ -Vector3.forward , -Vector3.forward , -Vector3.forward , -Vector3.forward };
		// 	mesh.uv = new Vector2[4]{ new Vector2{ x=0 , y=0 } , new Vector2{ x=1 , y=0 } , new Vector2{ x=0 , y=1 } , new Vector2{ x=1 , y=1 } };
		// 	string path = UnityEditor.EditorUtility.SaveFilePanelInProject( "Save Procedural Mesh" , "Procedural Mesh" , "asset", "message" );
		// 	if( path!=null ) UnityEditor.AssetDatabase.CreateAsset( mesh , path );
		// 	UnityEngine.Object.DestroyImmediate(mesh);
		// }
		// #endif

	}
}
