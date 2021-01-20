/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using CodeMonkey.Utils;


public struct QuadrantEntity : IComponentData {
    public TypeEnum typeEnum;

    public enum TypeEnum {
        Marine,
        Zombie,
    }
}

public struct QuadrantData : IComponentData {
    public Entity entity;
    public float3 position;
    public QuadrantEntity quadrantEntity;
}


public class ECS_QuadrantSystem : ComponentSystem {

    public static NativeMultiHashMap<int, QuadrantData> quadrantDataHashMap;

    public static void Init() {
        quadrantDataHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
    }


    public struct EntityWithPosition {
        public Entity entity;
        public float3 position;
    }

    private const int quadrantYMultiplier = 1000;
    private const int quadrantCellSize = 70;

    private static int GetPositionHashMapKey(float3 position) {
        return (int) (math.floor(position.x / quadrantCellSize) + (quadrantYMultiplier * math.floor(position.y / quadrantCellSize)));
    }

    public static void DebugDrawQuadrant(float3 position) {
        Vector3 lowerLeft = new Vector3(math.floor(position.x / quadrantCellSize) * quadrantCellSize, math.floor(position.y / quadrantCellSize) * quadrantCellSize);
        //Debug.DrawLine(lowerLeft, (Vector3)lowerLeft + new Vector3(quadrantCellSize, quadrantCellSize));
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+1, +0) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+0, +1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(+1, +0) * quadrantCellSize, lowerLeft + new Vector3(+1, +1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(+0, +1) * quadrantCellSize, lowerLeft + new Vector3(+1, +1) * quadrantCellSize);
        //Debug.Log(GetPositionHashMapKey(position) + "    " + position);
    }


    [BurstCompile]
    [RequireComponentTag(typeof(QuadrantEntity))]
    private struct SetEntityHashMapJob : IJobForEachWithEntity<Translation> {
        
        public NativeMultiHashMap<int, Entity>.ParallelWriter nativeMultiHashMap;

        public void Execute(Entity entity, int index, ref Translation translation) {
            int hashMapKey = GetPositionHashMapKey(translation.Value);
            nativeMultiHashMap.Add(hashMapKey, entity);
        }

    }

    [BurstCompile]
    [RequireComponentTag(typeof(QuadrantEntity))]
    private struct SetEntityWithPositionHashMapJob : IJobForEachWithEntity<Translation> {
        
        public NativeMultiHashMap<int, EntityWithPosition>.ParallelWriter nativeMultiHashMap;

        public void Execute(Entity entity, int index, ref Translation translation) {
            int hashMapKey = GetPositionHashMapKey(translation.Value);
            nativeMultiHashMap.Add(hashMapKey, new EntityWithPosition { entity = entity, position = translation.Value });
        }

    }
    
    [BurstCompile]
    [RequireComponentTag(typeof(QuadrantEntity))]
    private struct SetQuadrantDataHashMapJob : IJobForEachWithEntity<Translation, QuadrantEntity> {
        
        public NativeMultiHashMap<int, QuadrantData>.ParallelWriter nativeMultiHashMap;

        public void Execute(Entity entity, int index, ref Translation translation, ref QuadrantEntity quadrantEntity) {
            int hashMapKey = GetPositionHashMapKey(translation.Value);
            nativeMultiHashMap.Add(hashMapKey, new QuadrantData { 
                entity = entity, 
                position = translation.Value ,
                quadrantEntity = quadrantEntity
            });
        }

    }

    [BurstCompile]
    [RequireComponentTag(typeof(QuadrantData))]
    private struct FindClosestTargetJob : IJobForEachWithEntity<Translation, QuadrantEntity, FindTargetData> {
        
        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> targetHashMap;
        public NativeHashMap<Entity, QuadrantData>.ParallelWriter unitTargetHashMap;

        public void Execute(Entity entity, int index, ref Translation translation, ref QuadrantEntity quadrantEntity, ref FindTargetData findTargetData) {
            int unitHashMapKey = GetPositionHashMapKey(translation.Value);
            float3 unitPosition = translation.Value;
            Entity targetEntity = Entity.Null;
            float3 targetPosition = new float3(0, 0, 0);
            TrySetClosestTarget(unitHashMapKey    , unitPosition, quadrantEntity.typeEnum, ref targetEntity, ref targetPosition);
            TrySetClosestTarget(unitHashMapKey - 1, unitPosition, quadrantEntity.typeEnum, ref targetEntity, ref targetPosition); // Left
            TrySetClosestTarget(unitHashMapKey + 1, unitPosition, quadrantEntity.typeEnum, ref targetEntity, ref targetPosition); // Right
            TrySetClosestTarget(unitHashMapKey + quadrantYMultiplier - 1, unitPosition, quadrantEntity.typeEnum, ref targetEntity, ref targetPosition); // Up Left
            TrySetClosestTarget(unitHashMapKey + quadrantYMultiplier    , unitPosition, quadrantEntity.typeEnum, ref targetEntity, ref targetPosition); // Up Center
            TrySetClosestTarget(unitHashMapKey + quadrantYMultiplier + 1, unitPosition, quadrantEntity.typeEnum, ref targetEntity, ref targetPosition); // Up Right
            TrySetClosestTarget(unitHashMapKey - quadrantYMultiplier - 1, unitPosition, quadrantEntity.typeEnum, ref targetEntity, ref targetPosition); // Down Left
            TrySetClosestTarget(unitHashMapKey - quadrantYMultiplier    , unitPosition, quadrantEntity.typeEnum, ref targetEntity, ref targetPosition); // Down Center
            TrySetClosestTarget(unitHashMapKey - quadrantYMultiplier + 1, unitPosition, quadrantEntity.typeEnum, ref targetEntity, ref targetPosition); // Down Right

            if (math.distance(unitPosition, targetPosition) > findTargetData.targetRange) {
                targetEntity = Entity.Null;
            }

            if (targetEntity != Entity.Null) {
                unitTargetHashMap.TryAdd(entity, new QuadrantData { entity = targetEntity, position = targetPosition });
            }
        }

        private void TrySetClosestTarget(int quadrantHashMapKey, float3 unitPosition, QuadrantEntity.TypeEnum unitTypeEnum, ref Entity targetEntity, ref float3 targetPosition) {
            QuadrantData targetQuadrantData;
            NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
            if (targetHashMap.TryGetFirstValue(quadrantHashMapKey, out targetQuadrantData, out nativeMultiHashMapIterator)) {
                do {
                    if (targetQuadrantData.quadrantEntity.typeEnum != unitTypeEnum) {
                        // Different type enum, valid target
                        if (targetEntity == Entity.Null) {
                            // Has no target
                            targetEntity = targetQuadrantData.entity;
                            targetPosition = targetQuadrantData.position;
                        } else {
                            // Has target, closest?
                            // ######## TODO: REPLACE WITH math.select();
                            if (math.distance(unitPosition, targetQuadrantData.position) < math.distance(unitPosition, targetPosition)) {
                                // New Target closer
                                targetEntity = targetQuadrantData.entity;
                                targetPosition = targetQuadrantData.position;
                            }
                        }
                    }
                } while (targetHashMap.TryGetNextValue(out targetQuadrantData, ref nativeMultiHashMapIterator));
            }
        }

    }


    private struct SetTargetJob : IJobForEachWithEntity<Translation> {
        
        [ReadOnly] public NativeHashMap<Entity, QuadrantData> entityTargetHashMap;
        public EntityCommandBuffer.Concurrent entityCommandBuffer;

        public void Execute(Entity entity, int index, ref Translation translation) {
            QuadrantData quadrantData;
            if (entityTargetHashMap.TryGetValue(entity, out quadrantData)) {
                entityCommandBuffer.RemoveComponent(index, entity, typeof(HasTarget));
                entityCommandBuffer.AddComponent(index, entity, new HasTarget { targetEntity = quadrantData.entity, targetPosition = quadrantData.position });
            }
        }

    }

    private static int GetEntityCountInHashMap(NativeMultiHashMap<int, QuadrantData> quadrantDataHashMap, int quadrantHashMapKey) {
        QuadrantData quadrantData;
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        int count = 0;
        if (quadrantDataHashMap.TryGetFirstValue(quadrantHashMapKey, out quadrantData, out nativeMultiHashMapIterator)) {
            do {
                count++;
            } while (quadrantDataHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
        }
        return count;
    }


    protected override void OnDestroy() {
        quadrantDataHashMap.Dispose();
    }

    protected override void OnUpdate() {
        EntityQuery entityQuery = Entities.WithAll<QuadrantEntity, Translation, FindTargetData>().ToEntityQuery();

        quadrantDataHashMap.Clear();
        if (entityQuery.CalculateEntityCount() > quadrantDataHashMap.Capacity) {
            quadrantDataHashMap.Capacity = entityQuery.CalculateEntityCount();
        }

        NativeHashMap<Entity, QuadrantData> entityTargetHashMap = new NativeHashMap<Entity, QuadrantData>(entityQuery.CalculateEntityCount(), Allocator.TempJob);
        
        // Position Units in HashMap
        SetQuadrantDataHashMapJob setQuadrantDataHashMapJob = new SetQuadrantDataHashMapJob {
            nativeMultiHashMap = quadrantDataHashMap.AsParallelWriter(),
        };
        JobHandle setEntityHashMapJobHandle = JobForEachExtensions.Schedule(setQuadrantDataHashMapJob, entityQuery);
        setEntityHashMapJobHandle.Complete();
        

        // Cycle through all Units and FindTarget
        FindClosestTargetJob findClosestTargetJob = new FindClosestTargetJob {
            targetHashMap = quadrantDataHashMap,
            unitTargetHashMap = entityTargetHashMap.AsParallelWriter(),
        };
        JobHandle findClosestTargetJobHandle = JobForEachExtensions.Schedule(findClosestTargetJob, entityQuery);
        findClosestTargetJobHandle.Complete();


        SetTargetJob setTargetJob = new SetTargetJob {
            entityTargetHashMap = entityTargetHashMap,
            entityCommandBuffer = PostUpdateCommands.ToConcurrent(),
        };
        JobHandle setTargetJobHandle = JobForEachExtensions.Schedule(setTargetJob, entityQuery);
        setTargetJobHandle.Complete();

        entityTargetHashMap.Dispose();
        

        //DebugDrawQuadrant(UtilsClass.GetMouseWorldPosition());

        //int hashMapKey = GetPositionHashMapKey(UtilsClass.GetMouseWorldPosition());
        //Debug.Log(GetEntityCountInHashMap(quadrantDataHashMap, hashMapKey));
    }

}

