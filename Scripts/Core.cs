using System.Runtime.CompilerServices;
using Unity.Entities;
using Segments.Internal;

namespace Segments
{
	public static class Core
	{


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

				// DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups( world , typeof(SegmentRenderingSystem) );
				
				return world;
			}
		}


		public static SegmentRenderingSystem GetRenderingSystem ()
			=> GetWorld().GetOrCreateSystem<Segments.SegmentRenderingSystem>();


	}
}
