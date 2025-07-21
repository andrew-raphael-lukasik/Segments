using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEditor;

namespace Segments
{
    [WorldSystemFilter( WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor )]
    [UpdateInGroup( typeof(InitializationSystemGroup) )]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct SegmentInitializationSystem : ISystem
    {
        EntityQuery _query;

        [Unity.Burst.BurstCompile]
        public void OnCreate ( ref SystemState state )
        {
            _query = new EntityQueryBuilder(Allocator.Temp).WithAll<SegmentCreationRequestData>().Build( ref state );
            state.RequireForUpdate( _query );
        }

        // [Unity.Burst.BurstCompile]
        public void OnUpdate ( ref SystemState state )
        {
            var entitiesGraphicsSystem = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            var entityManager = state.EntityManager;
            
            foreach( Entity entity in _query.ToEntityArray(Allocator.Temp) )
            {
                var mesh = new Mesh();
                string label = $"Segments mesh {mesh.GetHashCode()}";
                mesh.name = label;
                mesh.MarkDynamic();
                mesh.hideFlags = HideFlags.DontSave;
                BatchMeshID batchMeshID = entitiesGraphicsSystem.RegisterMesh( mesh );

                var data = entityManager.GetSharedComponentManaged<SegmentCreationRequestData>( entity );
                BatchMaterialID batchMaterialID = entitiesGraphicsSystem.RegisterMaterial( data.material );
                var renderMeshDescription = new RenderMeshDescription( shadowCastingMode:ShadowCastingMode.On , receiveShadows:true , renderingLayerMask:1 );
                var materialMeshInfo = new MaterialMeshInfo( batchMaterialID , batchMeshID );
                RenderMeshUtility.AddComponents( entity , entityManager , renderMeshDescription , materialMeshInfo );

                entityManager.RemoveComponent<SegmentCreationRequestData>( entity );

                #if UNITY_EDITOR
                entityManager.SetName( entity , label );
                #endif
            }
        }
    }

    struct SegmentCreationRequestData : ISharedComponentData, System.IEquatable<SegmentCreationRequestData>
    {
        public Material material;

        public bool Equals ( SegmentCreationRequestData other )
        {
            if( other.material==null ) return false;
            return this.material==other.material;
        }
        public override int GetHashCode ()
        {
            int hash = base.GetHashCode();
            if( this.material!=null ) hash += 97 * this.material.GetHashCode();
            return hash;
        }
    }

}
