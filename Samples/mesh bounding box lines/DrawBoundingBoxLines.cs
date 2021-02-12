using UnityEngine;

using Unity.Mathematics;
using Unity.Entities;

namespace EcsLineRenderer.Samples
{
	[AddComponentMenu("")]
	[RequireComponent( typeof(MeshRenderer) )]
	class DrawBoundingBoxLines : MonoBehaviour
	{

		[SerializeField] Material _materialOverride = null;
		[SerializeField] float _widthOverride = 0.003f;

		MeshRenderer _meshRenderer = null;
		Entity[] _entities;
		EntityManager _commandLR;
		const int k_cube_vertices = 12;


		void OnEnable ()
		{
			_meshRenderer = GetComponent<MeshRenderer>();

			// make sure LR world exists:
			var worldLR = LineRendererWorld.GetOrCreateWorld();
			_commandLR = worldLR.EntityManager;

			// initialize segment pool:
			if( _entities==null || _entities.Length==0 )
			{
				if( _materialOverride!=null )
				{
					if( _widthOverride>0f )
						LineRendererWorld.InstantiatePool( k_cube_vertices , out _entities , _widthOverride , _materialOverride );
					else
						LineRendererWorld.InstantiatePool( k_cube_vertices , out _entities , _materialOverride );
				}
				else
				{
					if( _widthOverride>0f )
						LineRendererWorld.InstantiatePool( k_cube_vertices , out _entities , _widthOverride );
					else
						LineRendererWorld.InstantiatePool( k_cube_vertices , out _entities );
				}
			}
		}

		void OnDisable ()
		{
			if( LineRendererWorld.IsCreated )
				LineRendererWorld.Downsize( ref _entities , 0 );
		}

		void Update ()
		{
			int index = 0;
			var bounds = _meshRenderer.bounds;
			LineRendererWorld.Upsize( ref _entities , index+k_cube_vertices );
			Plot.Box(
				command:	_commandLR ,
				entities:	 _entities ,
				index:		ref index ,
				size:		bounds.size ,
				pos:		bounds.center ,
				rot:		quaternion.identity
			);
		}

	}
}
