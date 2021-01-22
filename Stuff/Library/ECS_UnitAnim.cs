using ECS_AnimationSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using V_AnimationSystem;

public class ECS_UnitAnim : MonoBehaviour
{

    public struct DictionaryKey
    {
        public ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum;
        public UnitAnim.AnimDir animDir;
    }

    public static Dictionary<DictionaryKey, ECS_UnitAnim> unitAnimDictionaryKeyDic;
    public static Dictionary<ECS_UnitAnim, List<Mesh>> unitAnimMeshListDic;

    public static void Init()
    {
        unitAnimMeshListDic = new Dictionary<ECS_UnitAnim, List<Mesh>>();
        unitAnimDictionaryKeyDic = new Dictionary<DictionaryKey, ECS_UnitAnim>();

        foreach (ECS_UnitAnimType ecsUnitAnimType in ECS_UnitAnimType.GetUnitAnimTypeList())
        {
            foreach (UnitAnim.AnimDir animDir in System.Enum.GetValues(typeof(UnitAnim.AnimDir)))
            {
                ECS_UnitAnim ecsUnitAnim = ecsUnitAnimType.GetUnitAnim(animDir);
                unitAnimDictionaryKeyDic[new DictionaryKey
                {
                    ecsUnitAnimTypeEnum = ecsUnitAnimType.GetTypeEnum(),
                    animDir = animDir
                }] = ecsUnitAnim;

                unitAnimMeshListDic[ecsUnitAnim] = new List<Mesh>();

                int frameCount = ecsUnitAnim.GetFrameCount();

                for (int i = 0; i < frameCount; i++)
                {
                    Mesh mesh = ECS_Animation.CreateMesh(ecsUnitAnim, i);
                    unitAnimMeshListDic[ecsUnitAnim].Add(mesh);
                }
            }
        }
    }

    public static ECS_UnitAnim Get(ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir)
    {
        return unitAnimDictionaryKeyDic[new DictionaryKey
        {
            ecsUnitAnimTypeEnum = ecsUnitAnimTypeEnum,
            animDir = animDir
        }];
    }

    public static List<Mesh> GetMeshList(ECS_UnitAnimType.TypeEnum ecsUnitAnimTypeEnum, UnitAnim.AnimDir animDir)
    {
        return unitAnimMeshListDic[Get(ecsUnitAnimTypeEnum, animDir)];
    }



    public ECS_Skeleton_Anim[] anims;

    public int GetFrameCount()
    {
        int frameCount = anims[0].frameArray.Length;

        foreach (ECS_Skeleton_Anim anim in anims)
        {
            if (anim.frameArray.Length > frameCount) frameCount = anim.frameArray.Length;
        }

        return frameCount;
    }

    public float GetFrameRate()
    {
        return anims[0].frameRate;
    }
}
