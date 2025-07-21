using UnityEngine;

namespace Samples
{
	[AddComponentMenu("")]
	class TransformRotate : MonoBehaviour
	{

		[SerializeField] bool _onDrawGizmos = true;
		[SerializeField] Vector3 _degreesPerSecond = new Vector3{ y=33f };

		#if UNITY_EDITOR
		void OnDrawGizmos ()
		{
			if( _onDrawGizmos && !Application.isPlaying )
				FixedUpdate();
		}
		#endif
		
		void FixedUpdate ()
			=> transform.Rotate( _degreesPerSecond*Time.fixedDeltaTime );
		
	}
}
