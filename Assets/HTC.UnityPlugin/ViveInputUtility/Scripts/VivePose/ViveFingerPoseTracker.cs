using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViveFingerPoseTracker : VivePoseTracker
{
    public const float HitOffset = 0.05f;
    [SerializeField]
    HandJointName pokeJoint = HandJointName.IndexTip;

    public override void OnNewPoses()
    {
        uint deviceIndex = viveRole.GetDeviceIndex();

        var isValid = VivePose.IsValid(deviceIndex);

        if (isValid)
        {
            //RigidPose fingerPose = VivePose.GetPose(deviceIndex);
            RigidPose fingerPose = _getFingerPose(deviceIndex);
            TrackPose(fingerPose, origin);
        }

        SetIsValid(isValid);
    }

    RigidPose _getFingerPose(uint deviceIndex)
    {
        var devicesState = VRModule.GetDeviceState(deviceIndex);
        RigidPose fingerPose = default(RigidPose);
        RigidPose pokeJoint;
        if (VivePose.TryGetHandJointPose(viveRole, this.pokeJoint, out pokeJoint))
        {
            string deviceName = devicesState.deviceModel.ToString().ToLower();
            RigidPose hmdPose = VivePose.GetPose(DeviceRole.Hmd);
            //Vector3 bodyPos = hmdPose.pos + hmdPose.rot * Vector3.up * -0.3f +
            //    hmdPose.rot * Vector3.right * ((deviceName.Contains("right")) ? 0.3f : -0.3f);
            //Vector3 hitDir = (pokeJoint.pos - bodyPos).normalized;
            Vector3 hitDir = hmdPose.rot * Vector3.forward;
            fingerPose.pos = pokeJoint.pos + hitDir * -HitOffset;
            fingerPose.rot = Quaternion.LookRotation(hitDir);
        }

        return fingerPose;
    }

}
