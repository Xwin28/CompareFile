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
using Unity.Mathematics;
using Unity.Entities;
using V_AnimationSystem;

namespace ECS_AnimationSystem {

    public static class ECS_Animation {


        public static void Init() {
            V_Animation.Init();
            ECS_UnitAnimType.Init();

            
            /*
            string[] unitAnimTypeNameArray = new string[] { 
                "dBareHands_Idle",
                "dBareHands_Walk",
                "dZombie_Idle",
                "dZombie_Walk",
                "dMarine_Idle",
                "dMarine_Walk",
                "dMarine_Aim",
                "dMarine_Attack",
            };

            foreach (string unitAnimTypeName in unitAnimTypeNameArray) {
                TestUnitAnimTypeConstantFrameCount(UnitAnimType.GetUnitAnimType(unitAnimTypeName));
            }*/
        }


        public static void PlayAnimJobs(Entity entity, int index, EntityCommandBuffer.Concurrent entityCommandBuffer, Skeleton_Data skeletonData, ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, Vector3 dir, Skeleton_Anim_OnComplete onComplete) {
            PlayAnimJobs(entity, index, entityCommandBuffer, skeletonData, ecsUnitAnimTypeEnum, GetAnimDir(dir), onComplete);
        }

        public static void PlayAnimJobs(Entity entity, int index, EntityCommandBuffer.Concurrent entityCommandBuffer, Skeleton_Data skeletonData, ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir, Skeleton_Anim_OnComplete onComplete) {
            if (IsAnimDifferentFromActive(skeletonData, ecsUnitAnimTypeEnum, animDir)) {
                // Different from current, play anim
                PlayAnimForcedJobs(entity, index, entityCommandBuffer, ecsUnitAnimTypeEnum, animDir, onComplete);
            }
        }

        public static void PlayAnim(Entity entity, ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, Vector3 dir, Skeleton_Anim_OnComplete onComplete) {
            Skeleton_Data skeletonData = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Skeleton_Data>(entity);
            PlayAnim(entity, skeletonData, ecsUnitAnimTypeEnum, GetAnimDir(dir), onComplete);
        }

        public static void PlayAnim(Entity entity, Skeleton_Data skeletonData, ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir, Skeleton_Anim_OnComplete onComplete) {
            if (IsAnimDifferentFromActive(skeletonData, ecsUnitAnimTypeEnum, animDir)) {
                PlayAnimForced(entity, ecsUnitAnimTypeEnum, animDir, onComplete);
            }
        }

        public static bool IsAnimDifferentFromActive(Skeleton_Data skeletonData, ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir) {
            // Ignores if same animType, same angle and same frameRateMod
            
            // 8 angles
            if (ecsUnitAnimTypeEnum == skeletonData.activeUnitAnimTypeEnum && animDir == skeletonData.activeAnimDir) {
                // Same anim, same angle
                return false;
            } else {
                if (ecsUnitAnimTypeEnum != skeletonData.activeUnitAnimTypeEnum) {
                    // Different anim, same angle
                    return true;
                } else {
                    // Different angle, same anim
                    return true;
                }
            }
        }

        public static void PlayAnimForced(Entity entity, ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, Vector3 dir, Skeleton_Anim_OnComplete onComplete) {
            UnitAnim.AnimDir animDir = GetAnimDir(dir);
            PlayAnimForced(entity, ecsUnitAnimTypeEnum, animDir, onComplete);
        }

        public static void PlayAnimForced(Entity entity, ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir, Skeleton_Anim_OnComplete onComplete) {
            Skeleton_Data skeletonData = GetSkeletonData(ecsUnitAnimTypeEnum, animDir, onComplete);
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(entity, skeletonData);
            
            if (skeletonData.frameRate <= 0) { Debug.LogError("#################### FRAMERATE ZERO!"); }
        }

        public static void PlayAnimForcedJobs(Entity entity, int index, EntityCommandBuffer.Concurrent entityCommandBuffer, ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir, Skeleton_Anim_OnComplete onComplete) {
            Skeleton_Data skeletonData = GetSkeletonData(ecsUnitAnimTypeEnum, animDir, onComplete);
            entityCommandBuffer.SetComponent(index, entity, skeletonData);

            if (skeletonData.frameRate <= 0) { Debug.LogError("#################### FRAMERATE ZERO!"); }
        }
        
        public static Skeleton_Data GetSkeletonData(ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir, Skeleton_Anim_OnComplete onComplete) {
            ECS_UnitAnimType ecsUnitAnimType = ECS_UnitAnimType.Get(ecsUnitAnimTypeEnum);
            ECS_UnitAnim ecsUnitAnim = ecsUnitAnimType.GetUnitAnim(animDir);
            return
                new Skeleton_Data {
                    frameCount = ecsUnitAnim.GetFrameCount(),
                    currentFrame = 0,
                    loopCount = 0,
                    frameTimer = 0f,
                    frameRate = ecsUnitAnim.GetFrameRate(),
                    activeUnitAnimTypeEnum = ecsUnitAnimTypeEnum,
                    activeAnimDir = animDir,
                    onComplete = onComplete,
                };
        }



        
        public static UnitAnim.AnimDir GetAnimDir(float3 dir) {
            return ECS_UnitAnimType.GetAnimDir(dir);
        }

        public static UnitAnim.AnimDir GetAnimDir(Vector3 dir) {
            return ECS_UnitAnimType.GetAnimDir(dir);
        }


        private static void TestUnitAnimTypeConstantFrameCount(UnitAnimType unitAnimType) {
            foreach (UnitAnim.AnimDir animDir in System.Enum.GetValues(typeof(UnitAnim.AnimDir))) {
                if (!DoesUnitAnimHaveConstantFrameCount(unitAnimType.GetUnitAnim(animDir))) {
                    Debug.Log("######## FALSE " + unitAnimType.GetUnitAnim(animDir));
                } else {
                    //Debug.Log("CORRECT " + unitAnimType.GetUnitAnim(animDir));
                }
            }
        }

        private static bool DoesUnitAnimHaveConstantFrameCount(UnitAnim unitAnim) {
            V_Skeleton_Anim[] anims = unitAnim.GetAnims();
            int firstFrameCount = anims[0].frames.Length;

            for (int i = 1; i < anims.Length; i++) {
                if (anims[i].frames.Length != firstFrameCount) {
                    return false;
                }
            }

            return true;
        }

        public static Mesh CreateMesh(ECS_UnitAnim unitAnim, int frame) {
            ECS_Skeleton_Anim[] anims = unitAnim.anims;
            int animsLength = anims.Length;
            Skeleton_AnimMeshData testAnimMeshData = new Skeleton_AnimMeshData(animsLength);

            for (int i = 0; i < animsLength; i++) {
                //int frameIndex = (frame < anims[i].frameArray.Length) ? frame : anims[i].frameArray.Length - 1;
                int frameIndex = frame % anims[i].frameArray.Length;
                testAnimMeshData.SetQuad(i, anims[i].frameArray[frameIndex].quad);
            }

            testAnimMeshData.SetIgnoreQuad(animsLength);
            testAnimMeshData.RefreshMesh();

            return testAnimMeshData.mesh;
        }

        public static Mesh CreateMesh(float meshWidth, float meshHeight) {
            Vector3[] vertices = new Vector3[4];
            Vector2[] uv = new Vector2[4];
            int[] triangles = new int[6];
            
            float meshWidthHalf  = meshWidth  / 2f;
            float meshHeightHalf = meshHeight / 2f;

            vertices[0] = new Vector3(-meshWidthHalf,  meshHeightHalf);
            vertices[1] = new Vector3( meshWidthHalf,  meshHeightHalf);
            vertices[2] = new Vector3(-meshWidthHalf, -meshHeightHalf);
            vertices[3] = new Vector3( meshWidthHalf, -meshHeightHalf);

            uv[0] = new Vector2(0, 1);
            uv[1] = new Vector2(1, 1);
            uv[2] = new Vector2(0, 0);
            uv[3] = new Vector2(1, 0);

            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 2;
            triangles[4] = 1;
            triangles[5] = 3;

            Mesh mesh = new Mesh();

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;

            return mesh;
        }

        public static ECS_UnitAnimType ConvertVAnimToAnim(ECS_UnitAnimType.TypeEnum typeEnum) {
            UnitAnimType unitAnimType = UnitAnimType.GetUnitAnimType(typeEnum.ToString());
            ECS_UnitAnimType ecsUnitAnimType = new ECS_UnitAnimType(
                typeEnum,
                ConvertVAnimToAnim(unitAnimType.GetUnitAnim(UnitAnim.AnimDir.Down)),
                ConvertVAnimToAnim(unitAnimType.GetUnitAnim(UnitAnim.AnimDir.Up)),
                ConvertVAnimToAnim(unitAnimType.GetUnitAnim(UnitAnim.AnimDir.Left)),
                ConvertVAnimToAnim(unitAnimType.GetUnitAnim(UnitAnim.AnimDir.Right)),
                ConvertVAnimToAnim(unitAnimType.GetUnitAnim(UnitAnim.AnimDir.DownLeft)),
                ConvertVAnimToAnim(unitAnimType.GetUnitAnim(UnitAnim.AnimDir.DownRight)),
                ConvertVAnimToAnim(unitAnimType.GetUnitAnim(UnitAnim.AnimDir.UpLeft)),
                ConvertVAnimToAnim(unitAnimType.GetUnitAnim(UnitAnim.AnimDir.UpRight))
            );
            return ecsUnitAnimType;
        }

        private static ECS_UnitAnim ConvertVAnimToAnim(UnitAnim unitAnim) {
            List<ECS_Skeleton_Anim> skeletonAnimList = new List<ECS_Skeleton_Anim>();
            foreach (V_Skeleton_Anim vSkeletonAnim in unitAnim.GetAnims()) {
                skeletonAnimList.Add(ConvertVAnimToAnim(vSkeletonAnim));
            }
            return new ECS_UnitAnim {
                anims = skeletonAnimList.ToArray()
            };
        }

        private static ECS_Skeleton_Anim ConvertVAnimToAnim(V_Skeleton_Anim vSkeletonAnim) {
            List<ECS_Skeleton_Frame> skeletonFrameList = new List<ECS_Skeleton_Frame>();
            foreach (V_Skeleton_Frame vSkeletonFrame in vSkeletonAnim.GetFrames()) {
                ECS_Skeleton_Frame skeletonFrame = ConvertVAnimToAnim(vSkeletonFrame);
                //Debug.Log(vSkeletonFrame.pos + " " + vSkeletonFrame.v00 + " " + vSkeletonFrame.v00offset);
                skeletonFrameList.Add(skeletonFrame);
            }
            return new ECS_Skeleton_Anim {
                frameArray = skeletonFrameList.ToArray(),
                frameRate = vSkeletonAnim.GetFrameRateOriginal()
            };
        }

        private static ECS_Skeleton_Frame ConvertVAnimToAnim(V_Skeleton_Frame vSkeletonFrame) {
            UVType uvType = vSkeletonFrame.GetUVType();
            Vector2 uv00, uv11;
            uvType.GetUVCoords(out uv00, out uv11);
            float2 skeletonPos = new float2(vSkeletonFrame.pos.x, vSkeletonFrame.pos.y);
            return new ECS_Skeleton_Frame {
                quad = new Quad {
                    quadVertices = new QuadVertices {
                        v00 = skeletonPos + new float2(vSkeletonFrame.v00.x + vSkeletonFrame.v00offset.x, vSkeletonFrame.v00.y + vSkeletonFrame.v00offset.y),
                        v01 = skeletonPos + new float2(vSkeletonFrame.v01.x + vSkeletonFrame.v01offset.x, vSkeletonFrame.v01.y + vSkeletonFrame.v01offset.y),
                        v10 = skeletonPos + new float2(vSkeletonFrame.v10.x + vSkeletonFrame.v10offset.x, vSkeletonFrame.v10.y + vSkeletonFrame.v10offset.y),
                        v11 = skeletonPos + new float2(vSkeletonFrame.v11.x + vSkeletonFrame.v11offset.x, vSkeletonFrame.v11.y + vSkeletonFrame.v11offset.y),
                    },
                    quadUV = new QuadUV {
                        uv00 = new float2(uv00.x, uv00.y),
                        //uv01 = new float2(uv00.x, uv11.y),
                        //uv10 = new float2(uv11.x, uv00.y),
                        uv11 = new float2(uv11.x, uv11.y),
                    }
                }
            };
        }


    }
    

    public struct QuadVertices {
        public float2 v00;
        public float2 v01;
        public float2 v10;
        public float2 v11;
    }

    public struct QuadUV {
        public float2 uv00;
        //public float2 uv01;
        //public float2 uv10;
        public float2 uv11;
    }

    public struct QuadTriangles {
        public int t0;
        public int t1;
        public int t2;
        public int t3;
        public int t4;
        public int t5;
    }

    public struct Quad {
        public QuadVertices quadVertices;
        public QuadUV quadUV;
        //public QuadTriangles quadTriangles;
    }

    public struct ECS_Skeleton_Frame {
        public Quad quad;
    }

    public class ECS_Skeleton_Anim {
        public ECS_Skeleton_Frame[] frameArray;
        public float frameRate;
    }



    public class ECS_UnitAnim {

        public struct DictionaryKey {
            public ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum;
            public UnitAnim.AnimDir animDir;
        }

        public static Dictionary<DictionaryKey, ECS_UnitAnim> unitAnimDictionaryKeyDic;
        public static Dictionary<ECS_UnitAnim, List<Mesh>> unitAnimMeshListDic;

        public static void Init() {
            unitAnimMeshListDic = new Dictionary<ECS_UnitAnim, List<Mesh>>();
            unitAnimDictionaryKeyDic = new Dictionary<DictionaryKey, ECS_UnitAnim>();

            foreach (ECS_UnitAnimType ecsUnitAnimType in ECS_UnitAnimType.GetUnitAnimTypeList()) {
                foreach (UnitAnim.AnimDir animDir in System.Enum.GetValues(typeof(UnitAnim.AnimDir))) {
                    ECS_UnitAnim ecsUnitAnim = ecsUnitAnimType.GetUnitAnim(animDir);
                    unitAnimDictionaryKeyDic[new DictionaryKey {
                        ecsUnitAnimTypeEnum = ecsUnitAnimType.GetTypeEnum(),
                        animDir = animDir
                    }] = ecsUnitAnim;

                    unitAnimMeshListDic[ecsUnitAnim] = new List<Mesh>();

                    int frameCount = ecsUnitAnim.GetFrameCount();

                    for (int i = 0; i < frameCount; i++) {
                        Mesh mesh = ECS_Animation.CreateMesh(ecsUnitAnim, i);
                        unitAnimMeshListDic[ecsUnitAnim].Add(mesh);
                    }
                }
            }
        }

        public static ECS_UnitAnim Get(ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir) {
            return unitAnimDictionaryKeyDic[new DictionaryKey {
                ecsUnitAnimTypeEnum = ecsUnitAnimTypeEnum,
                animDir = animDir
            }];
        }

        public static List<Mesh> GetMeshList(ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir) {
            return unitAnimMeshListDic[Get(ecsUnitAnimTypeEnum, animDir)];
        }



        public ECS_Skeleton_Anim[] anims;

        public int GetFrameCount() {
            int frameCount = anims[0].frameArray.Length;

            foreach (ECS_Skeleton_Anim anim in anims) {
                if (anim.frameArray.Length > frameCount) frameCount = anim.frameArray.Length;
            }

            return frameCount;
        }

        public float GetFrameRate() {
            return anims[0].frameRate;
        }

    }

    public class ECS_UnitAnimType {
    
        public enum TypeEnum {
            dBareHands_Idle,
            dBareHands_Walk,
            dMarine_Idle,
            dMarine_Walk,
            dMarine_Aim,
            dMarine_Attack,
            dZombie_Idle,
            dZombie_Walk,
        }

        public static List<ECS_UnitAnimType> unitAnimTypeList;
        public static Dictionary<TypeEnum, ECS_UnitAnimType> unitAnimTypeDic;

        public static void Init() {
            unitAnimTypeDic = new Dictionary<TypeEnum, ECS_UnitAnimType>();
            unitAnimTypeList = new List<ECS_UnitAnimType>();

            foreach (TypeEnum typeEnum in System.Enum.GetValues(typeof(TypeEnum))) {
                ECS_UnitAnimType ecsUnitAnimType = ECS_Animation.ConvertVAnimToAnim(typeEnum);
                unitAnimTypeDic[typeEnum] = ecsUnitAnimType;
                unitAnimTypeList.Add(ecsUnitAnimType);
            }

            ECS_UnitAnim.Init();
        }

        public static List<ECS_UnitAnimType> GetUnitAnimTypeList() {
            return unitAnimTypeList;
        }

        public static ECS_UnitAnimType Get(TypeEnum typeEnum) {
            return unitAnimTypeDic[typeEnum];
        }

        private Dictionary<UnitAnim.AnimDir, ECS_UnitAnim> singleAnimDic;
        private TypeEnum ecsUnitAnimTypeEnum;

        public ECS_UnitAnimType(TypeEnum ecsUnitAnimTypeEnum, ECS_UnitAnim animDown, ECS_UnitAnim animUp, ECS_UnitAnim animLeft, ECS_UnitAnim animRight, ECS_UnitAnim animDownLeft, ECS_UnitAnim animDownRight, ECS_UnitAnim animUpLeft, ECS_UnitAnim animUpRight) {
            this.ecsUnitAnimTypeEnum = ecsUnitAnimTypeEnum;
            singleAnimDic = new Dictionary<UnitAnim.AnimDir, ECS_UnitAnim>();
            SetAnims(animDown, animUp, animLeft, animRight, animDownLeft, animDownRight, animUpLeft, animUpRight);
        }

        private void SetAnims(ECS_UnitAnim animDown, ECS_UnitAnim animUp, ECS_UnitAnim animLeft, ECS_UnitAnim animRight, ECS_UnitAnim animDownLeft, ECS_UnitAnim animDownRight, ECS_UnitAnim animUpLeft, ECS_UnitAnim animUpRight) {
            singleAnimDic[UnitAnim.AnimDir.Down] = animDown;
            singleAnimDic[UnitAnim.AnimDir.Up] = animUp;
            singleAnimDic[UnitAnim.AnimDir.Left] = animLeft;
            singleAnimDic[UnitAnim.AnimDir.Right] = animRight;
            singleAnimDic[UnitAnim.AnimDir.DownLeft] = animDownLeft;
            singleAnimDic[UnitAnim.AnimDir.DownRight] = animDownRight;
            singleAnimDic[UnitAnim.AnimDir.UpLeft] = animUpLeft;
            singleAnimDic[UnitAnim.AnimDir.UpRight] = animUpRight;
        }

        public TypeEnum GetTypeEnum() {
            return ecsUnitAnimTypeEnum;
        }

        public ECS_UnitAnim GetUnitAnim(Vector3 dir) {
            return GetUnitAnim(V_UnitAnimation.GetAngleFromVector(dir));
        }

        public ECS_UnitAnim GetUnitAnim(int angle) {
            return GetUnitAnim(UnitAnim.GetAnimDirFromAngle(angle));
        }

        public ECS_UnitAnim GetUnitAnim(UnitAnim.AnimDir animDir) {
            return singleAnimDic[animDir];
        }

        public static UnitAnim.AnimDir GetAnimDir(Vector3 dir) {
            return GetAnimDir(V_UnitAnimation.GetAngleFromVector(dir));
        }

        public static UnitAnim.AnimDir GetAnimDir(int angle) {
            return UnitAnim.GetAnimDirFromAngle(angle);
        }

    }

    public class Skeleton_AnimMeshData {

        public Mesh mesh;
        public Vector3[] vertices;
        private int verticesLength;
        public Vector2[] uv;
        public int[] triangles;
        public Quad[] quadArray;
        public int quadIgnoreIndex;

        public Skeleton_AnimMeshData(int quadArrayLength) {
            mesh = new Mesh();
            mesh.MarkDynamic();
            //quadArrayLength = 7;
            quadArray = new Quad[quadArrayLength];
            vertices = new Vector3[4 * quadArrayLength];
            verticesLength = vertices.Length;
            uv = new Vector2[4 * quadArrayLength];

            triangles = new int[6 * quadArrayLength];// { 0, 1, 2, 2, 1, 3,     4+0, 4+1, 4+2, 4+2, 4+1, 4+3 };
            for (int i = 0; i < quadArrayLength; i++) {
                triangles[(i * 6) + 0] = (i * 4) + 0;
                triangles[(i * 6) + 1] = (i * 4) + 1;
                triangles[(i * 6) + 2] = (i * 4) + 2;
                triangles[(i * 6) + 3] = (i * 4) + 2;
                triangles[(i * 6) + 4] = (i * 4) + 1;
                triangles[(i * 6) + 5] = (i * 4) + 3;
            }

            quadIgnoreIndex = 0;
        }

        public void SetIgnoreQuad(int index) {
            quadIgnoreIndex = index;
        }

        public void SetQuad(int index, Quad quad) {
            quadArray[index] = quad;
        }

        private int verticeIndex;
        public void RefreshMesh() {
            verticeIndex = 0;

            Vector3 verticesZero = vertices[0];

            for (int i = 0; i < quadIgnoreIndex; i++) {
                Quad quad = quadArray[i];

                // Use Quad
                QuadVertices quadVertices = quad.quadVertices;
                vertices[verticeIndex    ] = new Vector3(quadVertices.v01.x, quadVertices.v01.y);
                vertices[verticeIndex + 1] = new Vector3(quadVertices.v11.x, quadVertices.v11.y);
                vertices[verticeIndex + 2] = new Vector3(quadVertices.v00.x, quadVertices.v00.y);
                vertices[verticeIndex + 3] = new Vector3(quadVertices.v10.x, quadVertices.v10.y);

                QuadUV quadUV = quad.quadUV;
                uv[verticeIndex    ] = new Vector2(quadUV.uv00.x, quadUV.uv11.y);
                uv[verticeIndex + 1] = new Vector2(quadUV.uv11.x, quadUV.uv11.y);
                uv[verticeIndex + 2] = new Vector2(quadUV.uv00.x, quadUV.uv00.y);
                uv[verticeIndex + 3] = new Vector2(quadUV.uv11.x, quadUV.uv00.y);

                verticeIndex = verticeIndex + 4;
            }
            while (verticeIndex < verticesLength) {
                vertices[verticeIndex] = verticesZero;
                verticeIndex++;
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
        }

    }

    /*public struct Skeleton_RefreshTimer : IComponentData {
        public const float SKELETON_REFRESHTIMER_MAX = .016f;
        public float refreshTimer;
    }*/

    public struct Skeleton_Data : IComponentData {

        public float frameTimer;
        public float frameRate;
        public int currentFrame;
        public int frameCount;
        public int loopCount;

        public ECS_UnitAnimType.TypeEnum activeUnitAnimTypeEnum;
        public UnitAnim.AnimDir activeAnimDir;

        public Skeleton_Anim_OnComplete onComplete;

    }

    public struct Skeleton_Anim_OnComplete {
        public bool hasOnComplete;
        public ECS_UnitAnimType.TypeEnum unitAnimTypeEnum;
        public UnitAnim.AnimDir animDir;

        public static Skeleton_Anim_OnComplete Create(ECS_UnitAnimType.TypeEnum unitAnimTypeEnum, float3 dir) {
            return Create(unitAnimTypeEnum, ECS_Animation.GetAnimDir(dir));
        }

        public static Skeleton_Anim_OnComplete Create(ECS_UnitAnimType.TypeEnum unitAnimTypeEnum, UnitAnim.AnimDir animDir) {
            return new Skeleton_Anim_OnComplete {
                hasOnComplete = true,
                unitAnimTypeEnum = unitAnimTypeEnum,
                animDir = animDir
            };
        }

    }
    

    public struct Skeleton_PlayAnim : IComponentData {
        public ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum;
        public UnitAnim.AnimDir animDir;
        public bool forced;
        public Skeleton_Anim_OnComplete onComplete;
    
        public void PlayAnim(ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, float3 dir, Skeleton_Anim_OnComplete onComplete) {
            PlayAnim(ecsUnitAnimTypeEnum, ECS_Animation.GetAnimDir(dir), false, onComplete);
        }

        public void PlayAnimForced(ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, float3 dir, Skeleton_Anim_OnComplete onComplete) {
            PlayAnim(ecsUnitAnimTypeEnum, ECS_Animation.GetAnimDir(dir), true, onComplete);
        }
    
        public void PlayAnim(ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir, Skeleton_Anim_OnComplete onComplete) {
            PlayAnim(ecsUnitAnimTypeEnum, animDir, false, onComplete);
        }
    
        public void PlayAnimForced(ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir, Skeleton_Anim_OnComplete onComplete) {
            PlayAnim(ecsUnitAnimTypeEnum, animDir, true, onComplete);
        }

        public void PlayAnim(ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir, bool forced, Skeleton_Anim_OnComplete onComplete) {
            this.ecsUnitAnimTypeEnum = ecsUnitAnimTypeEnum;
            this.animDir = animDir;
            this.forced = forced;
            this.onComplete = onComplete;
        }

    }

}