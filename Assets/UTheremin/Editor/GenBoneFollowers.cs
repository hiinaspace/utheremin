using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

public class GenBoneFollowers : MonoBehaviour
{
    [MenuItem("GenBoneFollowers/Generate")]
    static void Gen()
    {
        var p = AssetDatabase.LoadAssetAtPath("Assets/UTheremin/BoneFollower.prefab", typeof(GameObject));
        foreach (HumanBodyBones t in Enum.GetValues(typeof(HumanBodyBones)))
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p);
            go.name = $"BoneFollower-{t}";
            var u = go.GetComponent<UdonBehaviour>();
            u.publicVariables.TrySetVariableValue("trackedBone", t);
            u.SetProgramVariable("trackedBone", t);
        }
    }
}
