using Unity.Entities;

namespace EcsLineRenderer
{
	[System.Serializable]
	public struct SegmentSharedMaterialOverride : ISharedComponentData, System.IEquatable<SegmentSharedMaterialOverride>
	{
		
		public UnityEngine.Material Value;
		

		#region System.IEquatable
		bool System.IEquatable<SegmentSharedMaterialOverride>.Equals ( SegmentSharedMaterialOverride other ) => ReferenceEquals(Value,other.Value);
		public override int GetHashCode () => !ReferenceEquals(Value,null) ? Value.GetHashCode() : 0;
		#endregion

	}
}
