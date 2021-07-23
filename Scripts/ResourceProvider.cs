using UnityEngine;

namespace Segments.Internal
{
	public static class ResourceProvider
	{

		public static Material default_material { get; private set; }

		static ResourceProvider ()
		{
			// load default material asset:
			if( default_material==null )
			{
				const string path = "packages/Segments/default-line";
				default_material = UnityEngine.Resources.Load<Material>( path );
				if( default_material!=null )
					default_material.hideFlags = HideFlags.DontUnloadUnusedAsset;
				else
					Debug.LogWarning($"loading Material asset failed, path: \'{path}\'");
			}
		}
		
	}
}
