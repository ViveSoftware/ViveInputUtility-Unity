using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System;
using UnityEngine;
using UnityEngine.Events;

public class DeviceScanner : MonoBehaviour, INewPoseListener
{
    [Serializable]
    public class UnityEventUint : UnityEvent<uint> { }
    [Serializable]
    public class UnityEventFloat : UnityEvent<float> { }

    private static readonly Collider[] hits = new Collider[1];

    [Range(0.01f, 1f)]
    public float radius = 0.05f;
    public Transform VROrigin;
    public float scanDuration = 1f;
    public Transform scannedReticle;

    public UnityEventUint OnDeviceScanned = new UnityEventUint();
    public UnityEvent OnConnectedDeviceChanged = new UnityEvent();
    public UnityEventFloat OnScanning = new UnityEventFloat();

    private bool[] deviceConnected = new bool[ViveRole.MAX_DEVICE_COUNT];
    private uint sentDevice = ViveRole.INVALID_DEVICE_INDEX;

    private uint previousScannedDevice = ViveRole.INVALID_DEVICE_INDEX;
    private uint currentScannedDevice = ViveRole.INVALID_DEVICE_INDEX;

    private float lastScannedChangedTime;
    private bool connectedDeviceChanged;

    protected virtual void Start()
    {
        if (scannedReticle != null)
        {
            scannedReticle.gameObject.SetActive(false);
        }
    }

    protected virtual void OnEnable()
    {
        VivePose.AddNewPosesListener(this);
    }

    protected virtual void OnDisable()
    {
        VivePose.RemoveNewPosesListener(this);
    }

    public virtual void BeforeNewPoses() { }

    public virtual void OnNewPoses()
    {
        previousScannedDevice = currentScannedDevice;
        currentScannedDevice = ViveRole.INVALID_DEVICE_INDEX;

        for (uint i = 0; i < ViveRole.MAX_DEVICE_COUNT; ++i)
        {
            if (ChangeProp.Set(ref deviceConnected[i], VivePose.IsConnected(i)))
            {
                connectedDeviceChanged = true;

                if (!deviceConnected[i] && sentDevice == i)
                {
                    if (sentDevice == i)
                    {
                        sentDevice = ViveRole.INVALID_DEVICE_INDEX;
                    }

                    if (scannedReticle != null)
                    {
                        scannedReticle.gameObject.SetActive(false);
                    }
                }
            }

            if (!deviceConnected[i]) { continue; }

            var pose = VivePose.GetPose(i, VROrigin);

            if (sentDevice == i && scannedReticle != null)
            {
                scannedReticle.position = pose.pos;
            }

            hits[0] = null;
            var hitCount = Physics.OverlapSphereNonAlloc(pose.pos, radius, hits);
            if (hitCount > 0 && hits[0].transform.IsChildOf(transform))
            {
                if (!ViveRole.IsValidIndex(currentScannedDevice))
                {
                    // not scanned any device yet this frame
                    currentScannedDevice = i;
                }
                else
                {
                    // multiple device scanned this frame
                    currentScannedDevice = ViveRole.INVALID_DEVICE_INDEX;
                    break;
                }

                hits[0] = null;
            }
        }

        if (previousScannedDevice != currentScannedDevice)
        {
            lastScannedChangedTime = Time.time;
        }
    }

    public virtual void AfterNewPoses() { }

    public void ClearScanned()
    {
        OnDeviceScanned.Invoke(sentDevice = currentScannedDevice = ViveRole.INVALID_DEVICE_INDEX);
        scannedReticle.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (connectedDeviceChanged)
        {
            connectedDeviceChanged = false;

            OnConnectedDeviceChanged.Invoke();
        }

        if (previousScannedDevice != currentScannedDevice)
        {
            OnScanning.Invoke(0f);
        }
        else if (ViveRole.IsValidIndex(currentScannedDevice) && sentDevice != currentScannedDevice)
        {
            var scannedDuration = Time.time - lastScannedChangedTime;
            if (scannedDuration > scanDuration)
            {
                if (!ViveRole.IsValidIndex(sentDevice) && scannedReticle != null)
                {
                    scannedReticle.gameObject.SetActive(true);
                    scannedReticle.position = VivePose.GetPose(currentScannedDevice, VROrigin).pos;
                }

                OnDeviceScanned.Invoke(sentDevice = currentScannedDevice);
                OnScanning.Invoke(0f);
            }
            else
            {
                OnScanning.Invoke(scannedDuration / scanDuration);
            }
        }
    }
}
