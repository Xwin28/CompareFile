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
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using V_AnimationSystem;
using System.Threading;
using ECS_AnimationSystem;
using CodeMonkey.Utils;
using CodeMonkey.MonoBehaviours;

public class TestECS : MonoBehaviour {

    public static TestECS instance;

    public static Mesh QuadMesh { get { return instance.quadMesh; } }
    public static Mesh ShadowMesh { get { return instance.shadowMesh; } }
    public static Material ShadowMaterial { get { return instance.shadowMaterial; } }

    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private Mesh quadMesh;
    [SerializeField] public Material zombieMaterial;
    [SerializeField] public Material marineMaterial;
    [SerializeField] public Material shadowMaterial;
    private Mesh shadowMesh;

    private EntityManager entityManager;

    private Vector3 cameraFollowPosition;
    private float cameraFollowZoom;

    public static NativeQueue<MarineShotZombieAction> queuedActions;


    private void Awake() {
        //Sound_Manager.Init((Sound_Manager.AudioType audioType) => .015f);
        instance = this;
        queuedActions = new NativeQueue<MarineShotZombieAction>(Allocator.Persistent);
    }

    private void Start() {
        cameraFollowZoom = 80f;
        cameraFollow.Setup(() => cameraFollowPosition, () => cameraFollowZoom, true, true);
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        ECS_Animation.Init();
        ECS_QuadrantSystem.Init();

        shadowMesh = ECS_Animation.CreateMesh(9f, 6f);

        for (int i = 0; i < 30; i++) {
            SpawnMarine();
        }

        for (int i = 0; i < 1; i++) {
            SpawnZombie(true);
        }
    }

    private void SpawnMarine() {
        SpawnMarine(new float3(UnityEngine.Random.Range(-70f, 70f), UnityEngine.Random.Range(-60f, 60f), 0f));
    }

    private void SpawnMarine(float3 spawnPosition) {
        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(Marine),
            typeof(Skeleton_Data),
            typeof(Skeleton_Material),
            typeof(Skeleton_PlayAnim),
            typeof(QuadrantEntity),
            typeof(FindTargetData),
            typeof(Health),
            typeof(MarineShoot),
            typeof(MoveTo),
            typeof(EntityAnims),
            typeof(Translation)
        );

        Entity entity = entityManager.CreateEntity(entityArchetype);

        entityManager.SetComponentData(entity, new Translation { Value = spawnPosition });
        entityManager.SetComponentData(entity, new Skeleton_Data { frameRate = 1f });
        entityManager.SetComponentData(entity, new Skeleton_Material { materialTypeEnum = Skeleton_Material.TypeEnum.Marine });
        entityManager.SetComponentData(entity, new Skeleton_PlayAnim { ecsUnitAnimTypeEnum = ECS_UnitAnimType.TypeEnum.dBareHands_Idle, animDir = UnitAnim.AnimDir.Down });
        entityManager.SetComponentData(entity, new MarineShoot { nextShootTimerMax = .1f });
        entityManager.SetComponentData(entity, new Health { health = 50 });
        entityManager.SetComponentData(entity, new MoveTo { move = false, position = spawnPosition, moveSpeed = 40f });
        entityManager.SetComponentData(entity, new EntityAnims { idleAnimType = ECS_UnitAnimType.TypeEnum.dMarine_Idle, walkAnimType = ECS_UnitAnimType.TypeEnum.dMarine_Walk });
        entityManager.SetComponentData(entity, new QuadrantEntity { typeEnum = QuadrantEntity.TypeEnum.Marine });
        entityManager.SetComponentData(entity, new FindTargetData { targetRange = 100f });
            
        ECS_Animation.PlayAnimForced(entity, ECS_UnitAnimType.TypeEnum.dBareHands_Idle, new Vector3(0, -1), default);
    }

    private void SpawnZombie(bool spawnZombieTop) {
        EntityArchetype zombieArchetype = entityManager.CreateArchetype(
            typeof(Zombie),
            typeof(Skeleton_Data),
            typeof(Skeleton_Material),
            typeof(Skeleton_PlayAnim),
            typeof(QuadrantEntity),
            typeof(FindTargetData),
            typeof(Health),
            typeof(ZombieAttack),
            typeof(MoveTo),
            typeof(EntityAnims),
            typeof(Translation)
        );

        Entity entity = entityManager.CreateEntity(zombieArchetype);

        float3 spawnPosition;
        if (spawnZombieTop) {
            spawnPosition = new float3(UnityEngine.Random.Range(-100f, 100f), 400f, 0f);
        } else {
            spawnPosition = UtilsClass.GetRandomDir() * 400f;
        }

        entityManager.SetComponentData(entity, new Translation { Value = spawnPosition });
        entityManager.SetComponentData(entity, new Skeleton_Data { frameRate = 1f });
        entityManager.SetComponentData(entity, new Skeleton_Material { materialTypeEnum = Skeleton_Material.TypeEnum.Zombie });
        entityManager.SetComponentData(entity, new Skeleton_PlayAnim { ecsUnitAnimTypeEnum = ECS_UnitAnimType.TypeEnum.dBareHands_Idle, animDir = UnitAnim.AnimDir.Down });
        entityManager.SetComponentData(entity, new ZombieAttack { nextAttackTimerMax = .1f });
        entityManager.SetComponentData(entity, new Health { health = 30 });

        entityManager.SetComponentData(entity, new MoveTo { 
            move = true, 
            position = spawnZombieTop ? spawnPosition + new float3(0, -700f, 0) : (float3)Vector3.zero, 
            moveSpeed = 15f 
        });

        bool useZombieAims = UnityEngine.Random.Range(0, 100) < 50;
        entityManager.SetComponentData(entity, new EntityAnims { 
            idleAnimType = useZombieAims ? ECS_UnitAnimType.TypeEnum.dZombie_Idle : ECS_UnitAnimType.TypeEnum.dBareHands_Idle, 
            walkAnimType = useZombieAims ? ECS_UnitAnimType.TypeEnum.dZombie_Walk : ECS_UnitAnimType.TypeEnum.dBareHands_Walk 
        });

        entityManager.SetComponentData(entity, new QuadrantEntity { typeEnum = QuadrantEntity.TypeEnum.Zombie });
        entityManager.SetComponentData(entity, new FindTargetData { targetRange = 100f });
            
        ECS_Animation.PlayAnimForced(entity, ECS_UnitAnimType.TypeEnum.dBareHands_Idle, new Vector3(0, -1), default);
    }

    private void Update() {
        HandleCamera();
        HandleZombieSpawning();

        if (Input.GetMouseButtonDown(0)) {
            SpawnMarine(UtilsClass.GetMouseWorldPosition());
        }
    }

    private void HandleCamera() {
        Vector3 moveDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) { moveDir.y = +1f; }
        if (Input.GetKey(KeyCode.S)) { moveDir.y = -1f; }
        if (Input.GetKey(KeyCode.A)) { moveDir.x = -1f; }
        if (Input.GetKey(KeyCode.D)) { moveDir.x = +1f; }

        moveDir = moveDir.normalized;
        float cameraMoveSpeed = 300f;
        cameraFollowPosition += moveDir * cameraMoveSpeed * Time.deltaTime;

        float zoomSpeed = 1500f;
        if (Input.mouseScrollDelta.y > 0) cameraFollowZoom -= 1 * zoomSpeed * Time.deltaTime;
        if (Input.mouseScrollDelta.y < 0) cameraFollowZoom += 1 * zoomSpeed * Time.deltaTime;

        cameraFollowZoom = Mathf.Clamp(cameraFollowZoom, 20f, 200f);
    }

    private float zombieSpawnTimer;
    private float zombieSpawnTimerMax = .2f;
    private void HandleZombieSpawning() {
        zombieSpawnTimer -= Time.deltaTime;
        if (zombieSpawnTimer < 0) {
            zombieSpawnTimer = zombieSpawnTimerMax;
            zombieSpawnTimerMax = .2f - Time.time * .002f;
            zombieSpawnTimerMax = Mathf.Clamp(zombieSpawnTimerMax, .07f, 1f);

            int spawnZombieCount = Mathf.RoundToInt(1 + Time.time * .05f);

            if (Time.time < 20f) {
                for (int i = 0; i < spawnZombieCount; i++) {
                    SpawnZombie(true);
                }
            } else {
                SpawnZombie(true);
                for (int i = 0; i < spawnZombieCount; i++) {
                    SpawnZombie(false);
                }
            }
            //Debug.Log(spawnZombieCount + " " + zombieSpawnTimerMax + " " + Time.time);
        }
    }

    public static float GetCameraShakeIntensity() {
        float intensity = Mathf.Clamp(.7f - instance.cameraFollowZoom / 170f, .0f, 2f);
        return intensity;
    }

}


public struct Marine : IComponentData { }
public struct Zombie : IComponentData { }


public struct FindTargetData : IComponentData {
    public float targetRange;
}

public struct MarineShotZombieAction {
    public Entity marineEntity;
    public Entity zombieEntity;
    public int damageAmount;
}

public struct ZombieAttack : IComponentData {
    public float nextAttackTimer;
    public float nextAttackTimerMax;
}

public struct MarineShoot : IComponentData {
    public float nextShootTimer;
    public float nextShootTimerMax;
}

public struct MoveTo : IComponentData {
    public bool move;
    public float3 position;
    public float moveSpeed;
}

public struct HasTarget : IComponentData {
    public Entity targetEntity;
    public float3 targetPosition;
}

public struct Health : IComponentData {
    public int health;
}

public struct EntityAnims : IComponentData {
    public ECS_UnitAnimType.TypeEnum idleAnimType;
    public ECS_UnitAnimType.TypeEnum walkAnimType;
}

public struct Skeleton_Material : IComponentData {

    public TypeEnum materialTypeEnum;

    public enum TypeEnum {
        Marine,
        Zombie
    }
}




/*public class UnitActionMoveSystem : JobComponentSystem {

    [ExcludeComponent(typeof(HasTarget))]
    private struct Job : IJobForEachWithEntity<UnitAction, Translation, Skeleton_PlayAnim> {

        public float deltaTime;

        public void Execute(Entity entity, int index, [ReadOnly] ref UnitAction unitAction, ref Translation translation, ref Skeleton_PlayAnim skeletonPlayAnim) {
            if (math.distance(translation.Value, unitAction.moveToPosition) > 1f) {
                // Move to position
                float3 moveDir = math.normalize(unitAction.moveToPosition - translation.Value);
                float moveSpeed = 40f;
                translation.Value += moveDir * moveSpeed * deltaTime;
                skeletonPlayAnim.PlayAnim(ECS_UnitAnimType.TypeEnum.dMarine_Walk, moveDir, default);
            } else {
                // Already there
                skeletonPlayAnim.PlayAnim(ECS_UnitAnimType.TypeEnum.dMarine_Idle, float3.zero, default);
            }
        }

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        Job job = new Job {
            deltaTime = Time.deltaTime,
        };
        return job.Schedule(this, inputDeps);
    }

}*/

// Unit go to Move Position
public class UnitMoveSystem : JobComponentSystem {

    private struct Job : IJobForEachWithEntity<MoveTo, Translation, Skeleton_PlayAnim, EntityAnims> {

        public float deltaTime;

        public void Execute(Entity entity, int index, [ReadOnly] ref MoveTo moveTo, ref Translation translation, ref Skeleton_PlayAnim skeletonPlayAnim, ref EntityAnims entityAnims) {
            if (moveTo.move) {
                if (math.distance(translation.Value, moveTo.position) > 1f) {
                    // Move to position
                    float3 moveDir = math.normalize(moveTo.position - translation.Value);
                    translation.Value += moveDir * moveTo.moveSpeed * deltaTime;
                    skeletonPlayAnim.PlayAnim(entityAnims.walkAnimType, moveDir, default);
                } else {
                    // Already there
                    skeletonPlayAnim.PlayAnim(entityAnims.idleAnimType, float3.zero, default);
                }
            }
        }

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        Job job = new Job {
            deltaTime = UnityEngine.Time.deltaTime,
        };
        JobHandle _jobhandle = job.Schedule(this, inputDeps);
        _jobhandle.Complete();
        return _jobhandle;
    }

}

// Marine with no Target AI
public class MarineNoTargetSystem : JobComponentSystem {
    
    [RequireComponentTag(typeof(Marine))]
    [ExcludeComponent(typeof(HasTarget))]
    private struct Job : IJobForEachWithEntity<Translation, Skeleton_PlayAnim, EntityAnims> {


        public void Execute(Entity entity, int index, ref Translation translation, ref Skeleton_PlayAnim skeletonPlayAnim, ref EntityAnims entityAnims) {
            skeletonPlayAnim.PlayAnim(entityAnims.idleAnimType, float3.zero, default);
        }

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        Job job = new Job {
        };
        JobHandle _jobhandle = job.Schedule(this, inputDeps);
        _jobhandle.Complete();
        return _jobhandle;
    }

}

// Marines Shoot at Target
public class MarineTargetZombieSystem : JobComponentSystem {

    private struct Jobs : IJobForEachWithEntity<Translation, Skeleton_PlayAnim, HasTarget, MarineShoot> {

        public float time;
        public float deltaTime;
        public NativeQueue<MarineShotZombieAction>.ParallelWriter queuedActions;

        public void Execute(Entity entity, int index, ref Translation translation, ref Skeleton_PlayAnim skeletonPlayAnim, ref HasTarget hasTarget, ref MarineShoot marineShoot) {
            float3 targetDir = math.normalize(hasTarget.targetPosition - translation.Value);
            if (time >= marineShoot.nextShootTimer) {
                // Shoot
                marineShoot.nextShootTimer = time + marineShoot.nextShootTimerMax;
                skeletonPlayAnim.PlayAnimForced(ECS_UnitAnimType.TypeEnum.dMarine_Attack, targetDir, Skeleton_Anim_OnComplete.Create(ECS_UnitAnimType.TypeEnum.dMarine_Aim, targetDir));
                queuedActions.Enqueue(new MarineShotZombieAction {
                    marineEntity = entity,
                    zombieEntity = hasTarget.targetEntity,
                    damageAmount = 10,
                });
            } else {
                //skeletonPlayAnim.PlayAnim(ECS_UnitAnimType.TypeEnum.dMarine_Aim, targetDir, default);
            }
        }

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        Jobs job = new Jobs {
            time = UnityEngine.Time.time,
            deltaTime = UnityEngine.Time.deltaTime,
            queuedActions = TestECS.queuedActions.AsParallelWriter()
        };
        JobHandle _jobhandle = job.Schedule(this, inputDeps);
        _jobhandle.Complete();
        return _jobhandle;
    }

}


// Execute Queued AI Actions
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class QueuedActionSystem : ComponentSystem {

    protected override void OnUpdate() {
        EntityQuery entityQuery = GetEntityQuery(typeof(Zombie));
        int entityCount = entityQuery.CalculateEntityCount();

        MarineShotZombieAction marineShotZombieAction;
        while (TestECS.queuedActions.TryDequeue(out marineShotZombieAction)) {
            if (EntityManager.Exists(marineShotZombieAction.marineEntity) && EntityManager.Exists(marineShotZombieAction.zombieEntity)) {
                float3 marinePosition = EntityManager.GetComponentData<Translation>(marineShotZombieAction.marineEntity).Value;
                float3 zombiePosition = EntityManager.GetComponentData<Translation>(marineShotZombieAction.zombieEntity).Value;
                float3 marineToZombieDir = math.normalize(zombiePosition - marinePosition);

                bool bonusEffects = (entityCount < 400 || UnityEngine.Random.Range(0, 100) < 60);
                if (bonusEffects) {
                    WeaponTracer.Create(marinePosition + marineToZombieDir * 10f, (Vector3)zombiePosition + UtilsClass.GetRandomDir() * UnityEngine.Random.Range(0, 20f));
                    Shoot_Flash.AddFlash(marinePosition + marineToZombieDir * 14f);
                    Blood_Handler.SpawnBlood(2, zombiePosition, marineToZombieDir);
                    UtilsClass.ShakeCamera(TestECS.GetCameraShakeIntensity(), .05f);
                    //Sound_Manager.PlaySound(Sound_Manager.Sound.Rifle_Fire, marinePosition);
                }

                Health zombieHealth = EntityManager.GetComponentData<Health>(marineShotZombieAction.zombieEntity);
                zombieHealth.health -= marineShotZombieAction.damageAmount;
                if (zombieHealth.health < 0) {
                    // Zombie dead!
                    FlyingBody.TryCreate(GameAssets.i.pfEnemyFlyingBody, zombiePosition, marineToZombieDir);
                    EntityManager.DestroyEntity(marineShotZombieAction.zombieEntity);
                    EntityManager.RemoveComponent<HasTarget>(marineShotZombieAction.marineEntity);
                } else {
                    // Zombie still has health
                    EntityManager.SetComponentData(marineShotZombieAction.zombieEntity, zombieHealth);
                }
            } else {
                if (EntityManager.Exists(marineShotZombieAction.marineEntity) && !EntityManager.Exists(marineShotZombieAction.zombieEntity)) {
                    // Marine exists but zombie is dead
                    EntityManager.RemoveComponent<HasTarget>(marineShotZombieAction.marineEntity);
                }
            }
        }
    }

    protected override void OnDestroy() {
        TestECS.queuedActions.Dispose();
    }

}


/*public class UnitRotateTowardsMouseSystem : JobComponentSystem {

    private struct Job : IJobForEachWithEntity<Skeleton_PlayAnim, Translation> {

        public Vector3 mouseWorldPosition;

        public void Execute(Entity entity, int index, ref Skeleton_PlayAnim skeletonPlayAnim, [ReadOnly] ref Translation translation) {
            Vector3 dirToMouse = (mouseWorldPosition - (Vector3)translation.Value).normalized;
            skeletonPlayAnim.ecsUnitAnimTypeEnum = ECS_UnitAnimType.TypeEnum.dMarine_Idle;
            skeletonPlayAnim.animDir = ECS_Animation.GetAnimDir(dirToMouse);
        }

    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        Job job = new Job {
            mouseWorldPosition = UtilsClass.GetMouseWorldPosition(),
        };
        JobHandle jobHandle = job.Schedule(this, inputDeps);

        return jobHandle;
    }

}
*/



// Play the Animation currently stored in Skeleton_PlayAnim
[UpdateAfter(typeof(Skeleton_Callbacks))]
public class Skeleton_PlayAnimSystem : JobComponentSystem {

    private struct Job : IJobForEachWithEntity<Skeleton_Data, Skeleton_PlayAnim> {
        
        public EntityCommandBuffer.Concurrent entityCommandBuffer;

        public void Execute(Entity entity, int index, ref Skeleton_Data skeletonData, ref Skeleton_PlayAnim skeletonPlayAnim) {
            if (skeletonPlayAnim.forced) {
                skeletonPlayAnim.forced = false;
                ECS_Animation.PlayAnimForcedJobs(entity, index, entityCommandBuffer, skeletonPlayAnim.ecsUnitAnimTypeEnum, skeletonPlayAnim.animDir, skeletonPlayAnim.onComplete);
            } else {
                ECS_Animation.PlayAnimJobs(entity, index, entityCommandBuffer, skeletonData, skeletonPlayAnim.ecsUnitAnimTypeEnum, skeletonPlayAnim.animDir, skeletonPlayAnim.onComplete);
            }
        }

    }
    
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate() {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        Job job = new Job {
            entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
        };
        JobHandle jobHandle = job.Schedule(this, inputDeps);
        jobHandle.Complete();
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);
        
        return jobHandle;
    }

}

// Update the Current Skeleton Frame
public class Skeleton_UpdaterJob : JobComponentSystem {

    [BurstCompile]
    private struct Job : IJobForEach<Skeleton_Data> {

        public float deltaTime;

        public void Execute(ref Skeleton_Data skeletonData) {
            //skeletonRefreshTimer.refreshTimer -= deltaTime;
            skeletonData.frameTimer -= deltaTime;
            while (skeletonData.frameTimer < 0) {
                skeletonData.frameTimer += skeletonData.frameRate;
                skeletonData.currentFrame = skeletonData.currentFrame + 1;
                if (skeletonData.currentFrame >= skeletonData.frameCount) {
                    skeletonData.currentFrame = 0;
                    skeletonData.loopCount++;
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        Job job = new Job {
            deltaTime = UnityEngine.Time.deltaTime
        };
        JobHandle _jobhandle = job.Schedule(this, inputDeps);
        _jobhandle.Complete();
        return _jobhandle;
    }

}

[UpdateAfter(typeof(Skeleton_UpdaterJob))]
public class Skeleton_Callbacks : JobComponentSystem {
    
    private struct Job : IJobForEachWithEntity<Skeleton_Data, Skeleton_PlayAnim> {

        public void Execute(Entity entity, int index, ref Skeleton_Data skeletonData, ref Skeleton_PlayAnim skeletonPlayAnim) {
            if (skeletonData.loopCount > 0 && skeletonData.onComplete.hasOnComplete) {
                skeletonPlayAnim.PlayAnim(skeletonData.onComplete.unitAnimTypeEnum, skeletonData.onComplete.animDir, default);
            }
        }

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        Job job = new Job {
        };
        JobHandle _jobhandle = job.Schedule(this, inputDeps);
        _jobhandle.Complete();
        return _jobhandle;
    }

}


// Display the Mesh
[UpdateAfter(typeof(Skeleton_PlayAnimSystem))]
public class Skeleton_MeshDisplay : ComponentSystem {

    // Display Mesh
    protected override void OnUpdate() {
        Material marineMaterial = TestECS.instance.marineMaterial;
        Material zombieMaterial = TestECS.instance.zombieMaterial;
        Quaternion quaternionIdentity = Quaternion.identity;
        //Mesh mesh = ECS_UnitAnim.GetMeshList(ECS_UnitAnimType.TypeEnum.dBareHands_Idle, UnitAnim.AnimDir.Down)[0];
        //List<Mesh> meshList = ECS_UnitAnim.GetMeshList(ECS_UnitAnimType.TypeEnum.dBareHands_Idle, UnitAnim.AnimDir.Down);

        EntityQuery skeletonQuery = GetEntityQuery(ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<Skeleton_Data>(), ComponentType.ReadOnly<Skeleton_Material>());
        NativeArray<Translation> translationArray = skeletonQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<Skeleton_Data> skeletonDataArray = skeletonQuery.ToComponentDataArray<Skeleton_Data>(Allocator.TempJob);
        NativeArray<Skeleton_Material> skeletonMaterialArray = skeletonQuery.ToComponentDataArray<Skeleton_Material>(Allocator.TempJob);

        float3 skeletonOffset = new float3(0, 5f, 0);
        //*
        for (int i = 0; i < translationArray.Length; i++) {
            Vector3 position = translationArray[i].Value + skeletonOffset;
            position.z = position.y * .05f;
            Skeleton_Data skeletonData = skeletonDataArray[i];
            // #### TODO: Performance issue when grabbing mesh list
            List<Mesh> meshList = ECS_UnitAnim.GetMeshList(skeletonData.activeUnitAnimTypeEnum, skeletonData.activeAnimDir);
            Mesh mesh = meshList[skeletonData.currentFrame];
            Material material = skeletonMaterialArray[i].materialTypeEnum == Skeleton_Material.TypeEnum.Marine ? marineMaterial : zombieMaterial;
            Graphics.DrawMesh(mesh, position, quaternionIdentity, material, 0);

            Graphics.DrawMesh(TestECS.ShadowMesh, translationArray[i].Value + new float3(0, -3.5f, 0f), quaternionIdentity, TestECS.ShadowMaterial, 0);
        }
        //*/

        //UpdateMeshesMultithreaded(translationArray, skeletonDataArray, meshList[0]);

        translationArray.Dispose();
        skeletonDataArray.Dispose();
        skeletonMaterialArray.Dispose();
    }

    
    /*
    protected override void OnCreate() {
        Init();
        base.OnCreate();
    }

    protected override void OnDestroy() {
        if (threadPool != null) {
            foreach (Thread thread in threadPool) {
                thread.Abort();
            }
        }
        base.OnDestroy();
    }




    public struct ThreadData {
        public Translation[] translationArray;
        public Skeleton_Data[] skeletonDataArray;
        public Mesh mesh;
        public int indexStart;
        public int indexEnd;
	}


    public static void Init() {
        if (threadPool != null) {
            foreach (Thread thread in threadPool) {
                thread.Abort();
            }
        }
        threadPool = null;
        threadPoolQueue = null;
        mutex = null;
        
        // Create Mutex
		if (mutex == null) {
			mutex = new Mutex(false);
            locker = new object();
            lockerDone = new object();
		}

        cores = SystemInfo.processorCount * 2;
        if (threadPool == null) {
            // Create Threads
            threadPoolQueue = new Queue<ThreadData>();

            threadPool = new Thread[cores];
            Debug.Log("Start new Threads...");
            for (int j = 0; j < cores; j++) {
                threadPool[j] = new Thread(ThreadRun);// { IsBackground = true };
                threadPool[j].Start();
            }
        }

    }

    private static int finishCount;
    private static Mutex mutex;
    private static object locker;
    private static object lockerDone;
    private static bool allDone;

    private static int cores;
        
    private static Thread[] threadPool;
    private static Queue<ThreadData> threadPoolQueue;

    private void UpdateMeshesMultithreaded(NativeArray<Translation> translationArray, NativeArray<Skeleton_Data> skeletonDataArray, Mesh mesh) {
        //cores = SystemInfo.processorCount * 2;
        //cores = 1; // DISABLE MULTITHREADING
        
		int slice = translationArray.Length / cores;

		finishCount = 0;
        allDone = false;

        if (cores > 1) {
		    int i = 0;
            lock (locker) {
                ThreadData threadData;
                for (i = 0; i < cores - 1; i++) {
                    threadData = new ThreadData {
                        indexStart = slice * i,
                        indexEnd = slice * (i + 1),
                        translationArray = translationArray.ToArray(),
                        skeletonDataArray = skeletonDataArray.ToArray(),
                        mesh = mesh,
                    };
                    threadPoolQueue.Enqueue(threadData);
                }
                Monitor.PulseAll(locker);
            }
            ThreadData threadDataSingle = new ThreadData {
                indexStart = slice * i,
                indexEnd = translationArray.Length,
                translationArray = translationArray.ToArray(),
                skeletonDataArray = skeletonDataArray.ToArray(),
                mesh = mesh,
            };
		    SingleThread(threadDataSingle);

            lock (lockerDone) {
                if (!allDone) {
                    // Wait for all threads to be done
                    Monitor.Wait(lockerDone);
                }
            }
        } else {
            // Normal, just one core
			ThreadData threadData = new ThreadData {
                indexStart = 0,
                indexEnd = translationArray.Length,
                translationArray = translationArray.ToArray(),
                skeletonDataArray = skeletonDataArray.ToArray(),
                mesh = mesh,
            };
			DoWork(threadData);
        }

    }

    private static void ThreadRun() {
        while (true) {
            ThreadData threadData;

            lock (locker) {
                while (threadPoolQueue.Count == 0) Monitor.Wait(locker);
                threadData = threadPoolQueue.Dequeue();
            }

            SingleThread(threadData);
        }
    }

    private static void SingleThread(System.Object obj) {
        SingleThread((ThreadData)obj);
    }

	private static void SingleThread(ThreadData threadData) {
        DoWork(threadData);

		mutex.WaitOne();
		finishCount++;
        if (finishCount >= cores) {
            // All done
            lock (lockerDone) {
                allDone = true;
                Monitor.PulseAll(lockerDone);
            }
        }
		mutex.ReleaseMutex();
	}

    private static void DoWork(ThreadData threadData) {
        Material zombieMaterial = TestECS.instance.material;
        Quaternion quaternionIdentity = Quaternion.identity;

        for (int i = threadData.indexStart; i < threadData.indexEnd; i++) {
            Vector3 position = threadData.translationArray[i].Value;
            position.z = position.y * .1f;
            Mesh mesh = threadData.mesh;// meshList[skeletonDataArray[i].currentFrame];
            return;
            Graphics.DrawMesh(mesh, position, quaternionIdentity, zombieMaterial, 0);
        }
    }
    */

}

