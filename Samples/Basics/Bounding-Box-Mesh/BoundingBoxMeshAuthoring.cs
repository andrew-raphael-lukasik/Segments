using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Transforms;

namespace Samples
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    class BoundingBoxMeshAuthoring : MonoBehaviour
    {
        [SerializeField] Material _materialOverride = null;
        Entity _segments;

        void OnEnable()
        {
            // create segment buffer entity:
            Segments.Core.Create(out _segments, _materialOverride);

            // create local-space line segments:
            {
                var meshRenderer = GetComponent<MeshRenderer>();
                var segmentBuffer = Segments.Core.GetBuffer(_segments);

                // schedules a job that plots a bounding box
                segmentBuffer.Length = 12;// box needs 12 edges
                var bounds = meshRenderer.bounds;
                int index = 0;
                var jobHandle = new Segments.Plot.BoxJob(
                    segments:   segmentBuffer,
                    index:      ref index,
                    size:       bounds.size,
                    pos:        Vector3.zero,
                    rot:        quaternion.identity
                ).Schedule();

                // pass the job handle so dependency system knows aobut this job (needed when scheduling from Monobehaviours)
                Segments.Core.AddDependency(jobHandle);
            }
        }

        void OnDisable() => Segments.Core.Destroy(_segments);

        void Update()
        {
            // update transform (because lines are in local-space):
            var entityManager = Segments.Core.GetWorld().EntityManager;
            entityManager.SetComponentData(_segments, new LocalToWorld{
                Value = Matrix4x4.Translate(transform.position)// translation only because bounds size is world-space already
            });
        }

        #if UNITY_EDITOR
        void OnDrawGizmos() => Gizmos.DrawIcon(transform.position, "");// draws a white square icon to help with object selection in Scene view
        #endif

    }
}
