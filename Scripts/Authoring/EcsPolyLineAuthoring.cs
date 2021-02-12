using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace EcsLineRenderer.Authoring
{
    [DisallowMultipleComponent]
    public class EcsPolyLineAuthoring : MonoBehaviour
    {
        
        [UnityEngine.Serialization.FormerlySerializedAs("material")]
        public Material materialOverride = null;
		public float width = 0.1f;

		public Color color = Color.white;


        #if UNITY_EDITOR
        void OnValidate ()
        {
            ForEach( (t0,t1) =>
            {
                var go = t0.gameObject;
                var comp = go.GetComponent<EcsLineAuthoring>();
                if( comp==null ) comp = go.AddComponent<EcsLineAuthoring>();
                comp.materialOverride = this.materialOverride;
                comp.width = this.width;
                comp.start = t0.position;
                comp.end = t1.position;
                comp.color = this.color;
            });
            if( transform.childCount!=0 )
            {
                var comp = transform.GetChild(transform.childCount-1).gameObject.GetComponent<EcsLineAuthoring>();
                if( comp!=null )
                    UnityEditor.EditorApplication.delayCall += ()=> DestroyImmediate( comp );
            }
        }
        void OnDrawGizmos () => ForEach( (t0,t1) => Gizmos.DrawLine( t0.position , t1.position ) );
        void ForEach ( System.Action<Transform,Transform> action )
        {
            int numChildren = transform.childCount;
            for( int i=0 ; i<numChildren-1 ; i++ )
                action( transform.GetChild(i) , transform.GetChild(i+1) );
        }
        #endif

    }
}
