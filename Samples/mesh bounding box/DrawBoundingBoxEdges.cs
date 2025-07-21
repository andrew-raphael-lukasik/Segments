using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace Samples
{
    [ExecuteAlways]
    [RequireComponent( typeof(MeshRenderer) )]
    class DrawBoundingBoxEdges : MonoBehaviour
    {

        [SerializeField] Material _materialOverride = null;

        MeshRenderer _meshRenderer = null;
        Entity _segments;
        
        void OnEnable ()
        {
            _meshRenderer = GetComponent<MeshRenderer>();

            // create segment buffer entity:
            Segments.Core.Create( out _segments , _materialOverride );
        }

        void OnDisable ()
        {
            Segments.Core.Destroy( _segments );
        }

        void Update ()
        {
            var buffer = Segments.Core.GetBuffer( _segments );
            
            // schedules a job that plots a bounding box
            buffer.Length = 12;// box needs 12 edges
            var bounds = _meshRenderer.bounds;
            int index = 0;
            var jobHandle = new Segments.Plot.BoxJob(
                segments:    buffer ,
                index:        ref index ,
                size:        bounds.size ,
                pos:        bounds.center ,
                rot:        quaternion.identity
            ).Schedule();

            // pass along the job handle so Segments knows aobut this job (needed when scheduling from Monobehaviours)
            Segments.Core.AddDependency( jobHandle );
        }

        #if UNITY_EDITOR
        void OnDrawGizmos () => Gizmos.DrawIcon(transform.position, "");// draws a white square icon to help with object selection in Scene view
        #endif

    }
}
