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
        MeshRenderer _meshRenderer;
        Entity _segments;

        void OnEnable()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            
            // create segment buffer entity:
            Segments.Core.Create(out _segments, _materialOverride);
        }

        void OnDisable() => Segments.Core.Destroy(_segments);

        void Update()
        {
            // schedules a job that plots a bounding box
            var segment = Segments.Core.GetSegment(_segments);
            var bounds = _meshRenderer.bounds;
            int index = 0;
            segment.Buffer.Length = 12;// box needs 12 edges
            segment.Dependency.Value = new Segments.Plot.BoxJob(
                segments:   segment.Buffer,
                index:      ref index,
                size:       bounds.size,
                pos:        Vector3.zero,
                rot:        quaternion.identity
            ).Schedule(segment.Dependency.Value);
            // note: line segments here are local-space

            // notifies the segment update systems that line buffer changed and needs updating
            Segments.Core.SetSegmentChanged(_segments);

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
