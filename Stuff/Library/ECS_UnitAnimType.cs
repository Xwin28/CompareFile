using ECS_AnimationSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using V_AnimationSystem;

public class ECS_UnitAnimType : MonoBehaviour
{

    public enum TypeEnum
    {
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

    public static void Init()
    {
        unitAnimTypeDic = new Dictionary<TypeEnum, ECS_UnitAnimType>();
        unitAnimTypeList = new List<ECS_UnitAnimType>();

        foreach (TypeEnum typeEnum in System.Enum.GetValues(typeof(TypeEnum)))
        {
            ECS_UnitAnimType ecsUnitAnimType = ECS_Animation.ConvertVAnimToAnim(typeEnum);
            unitAnimTypeDic[typeEnum] = ecsUnitAnimType;
            unitAnimTypeList.Add(ecsUnitAnimType);
        }

        ECS_UnitAnim.Init();
    }

    public static List<ECS_UnitAnimType> GetUnitAnimTypeList()
    {
        return unitAnimTypeList;
    }

    public static ECS_UnitAnimType Get(TypeEnum typeEnum)
    {
        return unitAnimTypeDic[typeEnum];
    }

    private Dictionary<UnitAnim.AnimDir, ECS_UnitAnim> singleAnimDic;
    private TypeEnum ecsUnitAnimTypeEnum;

    public ECS_UnitAnimType(TypeEnum ecsUnitAnimTypeEnum, ECS_UnitAnim animDown, ECS_UnitAnim animUp, ECS_UnitAnim animLeft, ECS_UnitAnim animRight, ECS_UnitAnim animDownLeft, ECS_UnitAnim animDownRight, ECS_UnitAnim animUpLeft, ECS_UnitAnim animUpRight)
    {
        this.ecsUnitAnimTypeEnum = ecsUnitAnimTypeEnum;
        singleAnimDic = new Dictionary<UnitAnim.AnimDir, ECS_UnitAnim>();
        SetAnims(animDown, animUp, animLeft, animRight, animDownLeft, animDownRight, animUpLeft, animUpRight);
    }

    private void SetAnims(ECS_UnitAnim animDown, ECS_UnitAnim animUp, ECS_UnitAnim animLeft, ECS_UnitAnim animRight, ECS_UnitAnim animDownLeft, ECS_UnitAnim animDownRight, ECS_UnitAnim animUpLeft, ECS_UnitAnim animUpRight)
    {
        singleAnimDic[UnitAnim.AnimDir.Down] = animDown;
        singleAnimDic[UnitAnim.AnimDir.Up] = animUp;
        singleAnimDic[UnitAnim.AnimDir.Left] = animLeft;
        singleAnimDic[UnitAnim.AnimDir.Right] = animRight;
        singleAnimDic[UnitAnim.AnimDir.DownLeft] = animDownLeft;
        singleAnimDic[UnitAnim.AnimDir.DownRight] = animDownRight;
        singleAnimDic[UnitAnim.AnimDir.UpLeft] = animUpLeft;
        singleAnimDic[UnitAnim.AnimDir.UpRight] = animUpRight;
    }

    public TypeEnum GetTypeEnum()
    {
        return ecsUnitAnimTypeEnum;
    }

    public ECS_UnitAnim GetUnitAnim(Vector3 dir)
    {
        return GetUnitAnim(V_UnitAnimation.GetAngleFromVector(dir));
    }

    public ECS_UnitAnim GetUnitAnim(int angle)
    {
        return GetUnitAnim(UnitAnim.GetAnimDirFromAngle(angle));
    }

    public ECS_UnitAnim GetUnitAnim(UnitAnim.AnimDir animDir)
    {
        return singleAnimDic[animDir];
    }

    public static UnitAnim.AnimDir GetAnimDir(Vector3 dir)
    {
        return GetAnimDir(V_UnitAnimation.GetAngleFromVector(dir));
    }

    public static UnitAnim.AnimDir GetAnimDir(int angle)
    {
        return UnitAnim.GetAnimDirFromAngle(angle);
    }
}
