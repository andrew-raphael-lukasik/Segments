using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;

namespace EcsLineRenderer
{
	public struct SegmentMaterialOverride : IComponentData
	{
		
		public int id;

		
		static Dictionary<int,Material> _lookup = new Dictionary<int,Material>(10);
		public static SegmentMaterialOverride Factory ( Material mat ) => (SegmentMaterialOverride) mat;
		
		public static implicit operator Material ( SegmentMaterialOverride val ) => _lookup[val.id];
		public static implicit operator SegmentMaterialOverride ( Material mat )
		{
			int hash = mat.GetHashCode();
			if( !_lookup.ContainsKey(hash) ) _lookup.Add( hash , mat );
			return new SegmentMaterialOverride{ id = hash };
		}

	}
}
