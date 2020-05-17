
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Try to roughly follow hand "center of magnetic field" so you can control the pitch
/// using index finger tracking to a degree.
/// </summary>
public class HandFollower : UdonSharpBehaviour
{
    public HumanBodyBones hand;
    // wrist weighs 1
    float[] fingerWeight = new float[15] {
        0.3f, 0.3f, 0.3f,
        0.5f, 2.6f, 2.6f,
        0.5f, 1.6f, 1.6f,
        0.2f, 1.0f, 1.0f,
        0.2f, 1.0f, 1.0f,
    };
    float totalFingerWeight;

    VRCPlayerApi player;
    bool isInEditor;

    float startTime;

    void Start()
    {
        startTime = Time.time;
        player = Networking.LocalPlayer;
        isInEditor = player == null;
        totalFingerWeight = 0;
        for (int i = 0; i < 15; ++i)
        {
            totalFingerWeight += fingerWeight[i];
        }
    }

    void Update()
    {
        // XXX sometimes this script fails to track the owner's hand, I think it throws an exception
        // try waiting a few seconds until after the map loads, in case there's some initialization that
        // this races against to actually succeed, either GetBonePosition or Networking.IsOwner
        if ((Time.time - startTime) < 3f) return;

        if (isInEditor)
            return;

        if (Networking.IsOwner(gameObject))
        {
            Vector3 center;
            if (hand == HumanBodyBones.LeftHand)
            {
                center = player.GetBonePosition(HumanBodyBones.LeftHand);
                center += fingerWeight[0] * player.GetBonePosition(HumanBodyBones.LeftThumbProximal);
                center += fingerWeight[1] * player.GetBonePosition(HumanBodyBones.LeftThumbIntermediate);
                center += fingerWeight[2] * player.GetBonePosition(HumanBodyBones.LeftThumbDistal);
                center += fingerWeight[3] * player.GetBonePosition(HumanBodyBones.LeftIndexProximal);
                center += fingerWeight[4] * player.GetBonePosition(HumanBodyBones.LeftIndexIntermediate);
                center += fingerWeight[5] * player.GetBonePosition(HumanBodyBones.LeftIndexDistal);
                center += fingerWeight[6] * player.GetBonePosition(HumanBodyBones.LeftMiddleProximal);
                center += fingerWeight[7] * player.GetBonePosition(HumanBodyBones.LeftMiddleIntermediate);
                center += fingerWeight[8] * player.GetBonePosition(HumanBodyBones.LeftMiddleDistal);
                center += fingerWeight[9] * player.GetBonePosition(HumanBodyBones.LeftRingProximal);
                center += fingerWeight[10] * player.GetBonePosition(HumanBodyBones.LeftRingIntermediate);
                center += fingerWeight[11] * player.GetBonePosition(HumanBodyBones.LeftRingDistal);
                center += fingerWeight[12] * player.GetBonePosition(HumanBodyBones.LeftLittleProximal);
                center += fingerWeight[13] * player.GetBonePosition(HumanBodyBones.LeftLittleIntermediate);
                center += fingerWeight[14] * player.GetBonePosition(HumanBodyBones.LeftLittleDistal);
                center = center / (1 + totalFingerWeight);
            } else
            {
                center = player.GetBonePosition(HumanBodyBones.RightHand);
                center += fingerWeight[0]  * player.GetBonePosition(HumanBodyBones.RightThumbProximal);
                center += fingerWeight[1]  * player.GetBonePosition(HumanBodyBones.RightThumbIntermediate);
                center += fingerWeight[2]  * player.GetBonePosition(HumanBodyBones.RightThumbDistal);
                center += fingerWeight[3]  * player.GetBonePosition(HumanBodyBones.RightIndexProximal);
                center += fingerWeight[4]  * player.GetBonePosition(HumanBodyBones.RightIndexIntermediate);
                center += fingerWeight[5]  * player.GetBonePosition(HumanBodyBones.RightIndexDistal);
                center += fingerWeight[6]  * player.GetBonePosition(HumanBodyBones.RightMiddleProximal);
                center += fingerWeight[7]  * player.GetBonePosition(HumanBodyBones.RightMiddleIntermediate);
                center += fingerWeight[8]  * player.GetBonePosition(HumanBodyBones.RightMiddleDistal);
                center += fingerWeight[9]  * player.GetBonePosition(HumanBodyBones.RightRingProximal);
                center += fingerWeight[10] * player.GetBonePosition(HumanBodyBones.RightRingIntermediate);
                center += fingerWeight[11] * player.GetBonePosition(HumanBodyBones.RightRingDistal);
                center += fingerWeight[12] * player.GetBonePosition(HumanBodyBones.RightLittleProximal);
                center += fingerWeight[13] * player.GetBonePosition(HumanBodyBones.RightLittleIntermediate);
                center += fingerWeight[14] * player.GetBonePosition(HumanBodyBones.RightLittleDistal);
                center = center / (1 + totalFingerWeight);
            }
            transform.position = center;
        }
    }
   }
