
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

class VIUHandRenderer : MonoBehaviour, IViveRoleComponent
{
    [SerializeField]
    private ViveRoleProperty m_viveRole = ViveRoleProperty.New(TrackedHandRole.RightHand);
    public ViveRoleProperty viveRole { get { return m_viveRole; } }

    [SerializeField]
    bool renderAxis;
    [SerializeField]
    bool renderTrackedBone = true;
    bool oldRenderAxis;

    public bool RenderTrackedBone { set { renderTrackedBone = value; } }
    public bool RenderAxis { set { renderAxis = value; } }

    [Tooltip("Default color of hand points")]
    public Color pointColor = Color.blue;
    [Tooltip("Default color of links between keypoints in skeleton mode")]
    public Color linkColor = Color.white;

    [Tooltip("Root object of skinned mesh")]
    public SkinnedMeshRenderer skinRenderer;
    //[SerializeField]
    //bool rotationFix = true;
    [SerializeField]
    bool positionFix = true;
    [SerializeField]
    bool scaleFix = false;
    [Tooltip("Nodes of skinned mesh, must be size of 21 in same order as skeleton definition")]
    public Transform[] Nodes = new Transform[21];

    private static HandJointName[] considerJoint = new HandJointName[]
    {
            HandJointName.Wrist,
            HandJointName.ThumbMetacarpal, HandJointName.ThumbProximal, HandJointName.ThumbDistal, HandJointName.ThumbTip, // thumb        
            HandJointName.IndexProximal, HandJointName.IndexIntermediate,HandJointName.IndexDistal, HandJointName.IndexTip, // index
            HandJointName.MiddleProximal, HandJointName.MiddleIntermediate, HandJointName.MiddleDistal, HandJointName.MiddleTip, // middle
            HandJointName.RingProximal, HandJointName.RingIntermediate, HandJointName.RingDistal, HandJointName.RingTip, // ring
            HandJointName.PinkyProximal, HandJointName.PinkyIntermediate, HandJointName.PinkyDistal, HandJointName.PinkyTip, // pinky
    };

    HandJointName[] BoneRenderConnections
    {
        get
        {
            if (_boneRenderConnections == null)
            {
                List<HandJointName> connection = new List<HandJointName>();
                //connection.AddRange(new HandJointName[]
                //{
                //    HandJointName.Wrist, HandJointName.ThumbMetacarpal, HandJointName.Wrist, HandJointName.IndexProximal, HandJointName.Wrist, HandJointName.MiddleProximal, HandJointName.Wrist, HandJointName.RingProximal, HandJointName.Wrist, HandJointName.PinkyProximal, // palm and finger starts
                //    HandJointName.ThumbProximal, HandJointName.IndexProximal, HandJointName.IndexProximal, HandJointName.MiddleProximal, HandJointName.MiddleProximal, HandJointName.RingProximal, HandJointName.RingProximal, HandJointName.PinkyProximal, // finger starts          
                //});
                connection.AddRange(GetFingerConnection(FingerName.Thumb));
                connection.AddRange(GetFingerConnection(FingerName.Index));
                connection.AddRange(GetFingerConnection(FingerName.Middle));
                connection.AddRange(GetFingerConnection(FingerName.Ring));
                connection.AddRange(GetFingerConnection(FingerName.Pinky));
                _boneRenderConnections = connection.ToArray();
            }
            return _boneRenderConnections;
        }
    }
    private static HandJointName[] _boneRenderConnections = null;

    public static HandJointName[] GetFingerConnection(FingerName finger)
    {
        switch (finger)
        {
            case FingerName.Thumb:
                return new HandJointName[] { HandJointName.ThumbMetacarpal, HandJointName.ThumbProximal, HandJointName.ThumbProximal, HandJointName.ThumbDistal, HandJointName.ThumbDistal, HandJointName.ThumbTip };
            case FingerName.Index:
                return new HandJointName[] { HandJointName.IndexProximal, HandJointName.IndexIntermediate, HandJointName.IndexIntermediate, HandJointName.IndexDistal, HandJointName.IndexDistal, HandJointName.IndexTip };
            case FingerName.Middle:
                return new HandJointName[] { HandJointName.MiddleProximal, HandJointName.MiddleIntermediate, HandJointName.MiddleIntermediate, HandJointName.MiddleDistal, HandJointName.MiddleDistal, HandJointName.MiddleTip };
            case FingerName.Ring:
                return new HandJointName[] { HandJointName.RingProximal, HandJointName.RingIntermediate, HandJointName.RingIntermediate, HandJointName.RingDistal, HandJointName.RingDistal, HandJointName.RingTip };
            case FingerName.Pinky:
                return new HandJointName[] { HandJointName.PinkyProximal, HandJointName.PinkyIntermediate, HandJointName.PinkyIntermediate, HandJointName.PinkyDistal, HandJointName.PinkyDistal, HandJointName.PinkyTip };
            default: return null;
        }
    }

    List<GameObject> axisList;
    // list of points created (1 for 3D/2D point, 21 for skeleton)
    List<GameObject> points;
    // list of links created (only for skeleton)
    List<GameObject> links;
    // shared material for all point objects
    Material pointMat = null, linkMat = null;


    void Start()
    {
        if (skinRenderer != null)
            skinRenderer.enabled = false;
    }

    private void OnDisable()
    {
        _disableRenderBone();
    }

    Dictionary<HandJointName, RigidPose> _convertWorlPoses = new Dictionary<HandJointName, RigidPose>();
    void LateUpdate()
    {
        var deviceState = VRModule.GetCurrentDeviceState(viveRole.GetDeviceIndex());
        if (!deviceState.isPoseValid)
        {
            _disableRenderBone();
            if (skinRenderer != null)
                skinRenderer.enabled = false;
            return;
        }

        Transform wristNode = transform;

        //Transform joint consider the cameraRig's teleport position.
        RigidPose wristPose;
        if (!VivePose.TryGetHandJointPose(viveRole, HandJointName.Wrist, out wristPose))
            Debug.LogError("[VIUHandRenderer] Cannot find wrist in hand engine");

        Matrix4x4 cameraRigMat = Matrix4x4.identity;
        Matrix4x4 worldWristMat = Matrix4x4.identity;

        //Update points and links position
        int count = 0;
        foreach (HandJointName jointName in considerJoint)
        {
            RigidPose jointPose;
            if (VivePose.TryGetHandJointPose(viveRole, jointName, out jointPose))
            {
                if (count == 0)
                {
                    worldWristMat = transform.localToWorldMatrix;
                    Matrix4x4 worldWristMatNoScale = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

                    cameraRigMat = worldWristMat * Matrix4x4.TRS(wristPose.pos, wristPose.rot, Vector3.one).inverse;

                    Matrix4x4 localSpaceMat = cameraRigMat * Matrix4x4.TRS(wristPose.pos, wristPose.rot, Vector3.one);
                    if (transform.lossyScale.x < 0)
                        localSpaceMat.SetColumn(0, -localSpaceMat.GetColumn(0));
                    if (transform.lossyScale.y < 0)
                        localSpaceMat.SetColumn(1, -localSpaceMat.GetColumn(1));
                    if (transform.lossyScale.z < 0)
                        localSpaceMat.SetColumn(2, -localSpaceMat.GetColumn(2));

                    RigidPose pose = new RigidPose(worldWristMat.GetColumn(3), localSpaceMat.rotation);
                    _convertWorlPoses[jointName] = pose;
                }
                else
                {
                    Vector3 localSpaceVec = jointPose.pos - wristPose.pos;
                    Vector3 worldSpaceVec = cameraRigMat.MultiplyVector(localSpaceVec);

                    Matrix4x4 localSpaceMat = cameraRigMat * Matrix4x4.TRS(jointPose.pos, jointPose.rot, Vector3.one);
                    if (transform.lossyScale.x < 0)
                        localSpaceMat.SetColumn(0, -localSpaceMat.GetColumn(0));
                    if (transform.lossyScale.y < 0)
                        localSpaceMat.SetColumn(1, -localSpaceMat.GetColumn(1));
                    if (transform.lossyScale.z < 0)
                        localSpaceMat.SetColumn(2, -localSpaceMat.GetColumn(2));

                    Vector3 wristPos = (Vector3)worldWristMat.GetColumn(3);
                    RigidPose pose = new RigidPose(wristPos + worldSpaceVec, localSpaceMat.rotation);
                    _convertWorlPoses[jointName] = pose;
                }

                /*
                if (count == 0)
                {
                    Quaternion worldRigSpaceRot = transform.rotation;
                    Quaternion localRigSpaceRot = wristPose.rot;
                    cameraRigRot = worldRigSpaceRot * Quaternion.Inverse(localRigSpaceRot);
                    //Wrist must use Tracker hand's position, that also consider the CameraRig's transform
                    wristNode.position = transform.position;
                    wristNode.rotation = worldRigSpaceRot;

                    RigidPose pose = new RigidPose(wristNode.position, wristNode.rotation);
                    _convertWorlPoses[jointName] = pose;
                }
                else
                {
                    Vector3 localSpaceVec = jointPose.pos - wristPose.pos;
                    Vector3 worldSpaceVec = cameraRigRot * localSpaceVec;
                    Quaternion localSpaceRot = cameraRigRot * jointPose.rot;
                    RigidPose pose = new RigidPose(wristNode.position + worldSpaceVec, localSpaceRot);
                    _convertWorlPoses[jointName] = pose;
                }*/
            }
            else
                Debug.LogError("[VIUHandRenderer] Cannot find joint in hand engine : " + jointName);
            count++;
        }

        //Update skin
        if (skinRenderer != null)
        {
            skinRenderer.enabled = true;

            Nodes[0].position = _convertWorlPoses[HandJointName.Wrist].pos;
            Nodes[0].rotation = _convertWorlPoses[HandJointName.Wrist].rot;

            int nodeIndex = 1;
            for (int b = 0; b < 5; b++)
            {
                FingerName finger = (FingerName)b;
                HandJointName[] fingerConnection;
                fingerConnection = GetFingerConnection(finger);
                int countNode = 0;
                for (int a = 0; a < fingerConnection.Length; a += 2)
                {
                    Transform curJoint = Nodes[nodeIndex + countNode];
                    Transform nextJoint = Nodes[nodeIndex + countNode + 1];
                    RigidPose pose = _convertWorlPoses[fingerConnection[a]];
                    RigidPose nextPose = _convertWorlPoses[fingerConnection[a + 1]];

                    if (positionFix)
                    {
                        // if (a == 0)
                        curJoint.position = pose.pos;
                    }

                    //1. Set rotation and get new dir with next joint.
                    curJoint.rotation = pose.rot;
                    //if (rotationFix)
                    //{
                    //    //Calculate the joint rotation offset between joint axis dir and actual bone dir.

                    //    //2. Calculate offset
                    //    //*.Global rotation way
                    //    //Vector3 dir = -(nextJoint.position - curJoint.position).normalized;
                    //    //dir = cameraRigMat.inverse.MultiplyVector(dir);
                    //    //Quaternion rot = Quaternion.FromToRotation(dir, pose.rot * Vector3.forward);
                    //    //curJoint.rotation = pose.rot * rot;
                    //    //curJoint.rotation = cameraRigMat.rotation * curJoint.rotation;

                    //    //*.Local rotation can record at initialize step, no need compute at update().
                    //    //Vector3 localSkinCur = curJoint.InverseTransformPoint(curJoint.position);                    
                    //Vector3 localSkinNext = curJoint.InverseTransformPoint(nextPose.pos);//nextJoint's positin at curJoint
                    //Vector3 localSkinDir = -(localSkinNext /*- localSkinCur*/).normalized;
                    //curJoint.localRotation = Quaternion.LookRotation(localSkinDir);
                    //}

                    countNode++;
                }
                nodeIndex += 4;//skip finger tip
            }

        }

        //Update debug bone
        if (renderTrackedBone)
        {
            if (oldRenderAxis != renderAxis)
            {
                oldRenderAxis = renderAxis;
                _destroyRenderBone();
            }
            _initRenderBone(wristNode);
            _updateRenderBone();
        }
        else
            _destroyRenderBone();
    }

    void _disableRenderBone()
    {
        if (points != null)
            foreach (var p in points)
                p.SetActive(false);
        if (links != null)
            foreach (var l in links)
                l.SetActive(false);
        if (axisList != null)
            foreach (GameObject o in axisList)
                o.SetActive(false);
    }

    void _destroyRenderBone()
    {
        if (points != null)
            foreach (GameObject o in points)
                Destroy(o);
        if (links != null)
            foreach (GameObject o in links)
                Destroy(o);
        if (axisList != null)
            foreach (GameObject o in axisList)
                Destroy(o);
        points = links = axisList = null;
    }

    void _initRenderBone(Transform wristNode)
    {
        if (points != null)
            return;

        axisList = new List<GameObject>();
        points = new List<GameObject>();
        links = new List<GameObject>();

        // create game objects for points, number of points is determined by mode
        //int count = GestureProvider.HaveSkeleton ? 21 : 1;
        //ThumbProximal -> Tip = 4*4 +3 + 1 = 20
        int count = considerJoint.Length;
        Debug.Log("[VIUHandRenderer] consider joint : " + count);
        for (int i = 0; i < count; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "point" + i;
            //go.transform.parent = wristNode;
            go.transform.localScale = Vector3.one * 0.006f;
            go.SetActive(false);
            Destroy(go.GetComponent<Collider>());
            points.Add(go);

            // handle layer
            go.layer = wristNode.gameObject.layer;

            if (pointMat == null)
            {
                pointMat = new Material(go.GetComponent<Renderer>().sharedMaterial);
                pointMat.color = pointColor;
                linkMat = new Material(go.GetComponent<Renderer>().sharedMaterial);
                linkMat.color = linkColor;
            }

            // handle material
            go.GetComponent<Renderer>().sharedMaterial = pointMat;

            if (renderAxis)
            {
                GameObject axisObj = _createAxisPrefab();
                axisObj.name = "axis" + i;
                //axisObj.transform.parent = wristNode;
                axisObj.transform.localScale = Vector3.one * 0.006f;
                axisObj.SetActive(false);
                axisList.Add(axisObj);
            }
        }
        Debug.Log("[VIUHandRenderer] consider joint points : " + points.Count);

        // create game objects for links between keypoints, only used in skeleton mode         
        for (int i = 0; i < BoneRenderConnections.Length; i += 2)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = "link" + i;
            //go.transform.parent = wristNode;
            go.transform.localScale = Vector3.one * 0.008f;
            go.SetActive(false);
            Destroy(go.GetComponent<Collider>());
            links.Add(go);

            // handle layer
            go.layer = wristNode.gameObject.layer;

            // handle material
            go.GetComponent<Renderer>().sharedMaterial = linkMat;
        }
    }

    void _updateRenderBone()
    {
        // update points and links position
        int count = 0;
        foreach (HandJointName jointName in considerJoint)
        {
            var go = points[count];
            go.transform.position = _convertWorlPoses[jointName].pos;
            go.transform.rotation = _convertWorlPoses[jointName].rot;
            go.SetActive(true);

            if (renderAxis && axisList.Count > 0)
            {
                GameObject axis = axisList[count];
                axis.transform.position = go.transform.position;
                axis.transform.rotation = go.transform.rotation;
                axis.SetActive(true);
            }
            count++;
        }

        for (int i = 0; i < links.Count; i++)
        {
            var link = links[i];
            link.SetActive(false);

            HandJointName startIndex = BoneRenderConnections[i * 2];
            HandJointName endIndex = BoneRenderConnections[i * 2 + 1];

            Vector3 pose1 = _convertWorlPoses[startIndex].pos;
            Vector3 pose2 = _convertWorlPoses[endIndex].pos;
            // calculate link position and rotation based on points on both end
            link.SetActive(true);

            link.transform.position = (pose1 + pose2) / 2;
            var direction = pose2 - pose1;
            float len = direction.magnitude;
            direction /= len;
            link.transform.rotation = Quaternion.FromToRotation(Vector3.forward,
                //_convertWorlPoses[startIndex].rot * Vector3.forward
                direction
                );
            link.transform.localScale = new Vector3(0.0005f, 0.0005f, len / 2f - 0.001f);
        }
    }

    GameObject _createAxisPrefab()
    {
        GameObject axisNode = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(axisNode.GetComponent<Collider>());
        axisNode.GetComponent<Renderer>().material.color = Color.white;

        GameObject zNode = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(zNode.GetComponent<Collider>());
        zNode.GetComponent<Renderer>().material.color = Color.blue;
        zNode.transform.parent = axisNode.transform;
        zNode.transform.localPosition = Vector3.forward * 1f;
        zNode.transform.localRotation = Quaternion.Euler(90, 0, 0);
        zNode.transform.localScale = new Vector3(0.3f, 1f, 0.3f);


        GameObject yNode = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(yNode.GetComponent<Collider>());
        yNode.GetComponent<Renderer>().material.color = Color.green;
        yNode.transform.parent = axisNode.transform;
        yNode.transform.localPosition = Vector3.up * 1f;
        yNode.transform.localRotation = Quaternion.Euler(0, 0, 0);
        yNode.transform.localScale = new Vector3(0.3f, 1f, 0.3f);

        GameObject xNode = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(xNode.GetComponent<Collider>());
        xNode.GetComponent<Renderer>().material.color = Color.red;
        xNode.transform.parent = axisNode.transform;
        xNode.transform.localPosition = Vector3.right * 1f;
        xNode.transform.localRotation = Quaternion.Euler(0, 0, 90);
        xNode.transform.localScale = new Vector3(0.3f, 1f, 0.3f);

        return axisNode;
    }
}
