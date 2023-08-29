# Vive Input Utility for Unity - v1.18.3
Copyright (c) 2016-2023, HTC Corporation. All rights reserved.


## Changes for v1.18.3:

* Changes & Fixes
  - Fix unable to get controller button on OpenVR(SteamVR) platform
    - Seems latest Unity XR Plugin chages behaviour causes not actvating SteamVRv2Module correctly
  - Now recommanded settings button will show up in VIUSettings if there's ignored settings
  - Fix ViveColliderEventCaster button not working in some cases
  - Fix ControllerButton.BKeyTouch typo


## Changes for v1.18.2:

* Bug Fixes
  - Fix Meta Pro controller input not handled when using latest Oculus plugin
  - Fix unable to enable platform support in VIU Settings when using latest Unity XR Plugin Management


## Changes for v1.18.1:

* Changes
  - Update compatibility with com.unity.xr.openxr 1.4.2
  - Update compatibility with Oculus Integration v50
    - Handle removal of OVR Avatar
    - Add Quest Pro & Touch Pro support
  - Deprecate old Oculus graphics & quality recommended settings check on newer version

* Bug Fixes
  - Fix GetPadPressVector/GetPadTouchVector always return zero value
  - Fix missing controller models after suspend/resume
  - Fix Focus 3 controller not recoginzed in OpenXR mode


## Changes for v1.17.0:

* Changes
  - Now support Focus 3 Tracker through [VBS](https://business.vive.com/us/support/vbs/category_howto/vive-business-streaming.html)
  - Now button for Focus 3 Tracker is mapping to ControllerButton.ApplicationMenu instead of ControllerButton.A
  - Remove Graphic Jobs recommended settings for Wave
    - According to Unity document, Graphics Jobs only supported on certain environment(Vulkan) on Android.

* Bug Fixes
  - Fix Grabbable object with PoseFreezer calculating wrong pose when the object root is not at (0,0,0)
  - Fix device status for WaveHandTracking doesn't reset correctly


## Changes for v1.16.0:

* Changes
  - Add **Vive Wrist Tracker** support
    - Requires latest Wave XR plugin (v4.3+)
    - Able to work with ViveRole binding system
  - More Oculus platform support
    - Now able to recognize Quest 2 controllers (VRModuleDeviceModel.OculusQuest2ControllerLeft/Right)
    - Add new setting options
      - Enable/Disable Oculus Hand Tracking
      - Enable/Disable Tracked Hand Render Model
      - Enable/Disable Oculus Controller Render Model
      - Enable/Disable Hand Attached to Oculus Controller Render Model

* Bug fixes
  - Fix HandRole doesn't map other controller role correctly
  - Fix sometimes unable to enable/disable Wave HandTracking/WristTracker features in VIUSettings
  - Fix Oculus SDK Render Model broken when switching back from tracked hand
  - Fix compatibility with older Oculus SDK version


## Changes for v1.15.0:

* Changes
  - New OpenXR support options in VIU Settings (experimental)
    - OpenXR Desktop
    - OpenXR Android for WaveXR
    - OpenXR Android for Oculus
  - Add Oculus Quest 2 controller input support

* Known Issues
  - Oculus SDK Render Model brakes when switch back from tracked hand


## Changes for v1.14.2:

* Changes
  - Add compatibility with Vive Wave 4.3
  - Add compatibility with Open XR Plugin
    - Fix ambiguous PoseControl type
  - Remove unused dropdown button for WMR settings

* Bug Fixes
  - Fix compile error when SteamVR installed and platform is Android
  - Fix RenderModelHook unable to enable/disable custom/fallback render model in some cases


## Changes for v1.14.1:

* Changes
  - Add Wave Hand Tracking support (Wave XR Plugin v4.1 or newer required)
  - Add fallback model for ViveFlowPhoneController and ViveFocus3Controller


## Changes for v1.14.0:

* Changes
  - Add ability to identify ViveFlowPhoneController
  - Add plain fallback model for ViveFocus3Controller & ViveTracker3
  - Add Input System support
    - Required [Input System](https://docs.unity3d.com/Manual/com.unity.inputsystem.html) installed in project
	- For example, now able to bind v3 position action from HandRole.RightHand device by setting binding path to
	  - <VIUSyntheticDeviceLayoutHandRole>{RightHand}/position
  - Add new role type "PrimaryHandRole"
    - PrimaryHand maps first found controller/tracker/trackedhand accrodeing to which dominent hand
	- API to control PrimaryHandRole dominant hand:
      - ViveRole.DefaultPrimaryHandRoleHandler.DominantHand
      - ViveRole.DefaultPrimaryHandRoleHandler.SetRightDominantAndRefresh()
      - ViveRole.DefaultPrimaryHandRoleHandler.SetLeftDominantAndRefresh()
      - ViveRole.DefaultPrimaryHandRoleHandler.SwapDominantHandAndRefresh()

* Bug Fixes
  - Fix LiteCoroutine DelayUpdateCall not working in some cases
  - Fix RenderModelHook shaderOverride not working


## Changes for v1.13.4:

* Changes
  - Add static API to retrieve pinch ray from Wave SDK
    - WaveHandTrackingSubmodule.TryGetLeftPinchRay(out Vector3 origin, out Vector3 direction)
    - WaveHandTrackingSubmodule.TryGetLeftPinchRay(out Vector3 origin, out Vector3 direction)
    - Returns true if the pinch ray is currently valid, that is having valid tracking and the app got input focus
  - Add 4 fingers curl & grip button values for Wave tracked hand device
  - Change GestureIndexPinch button active threshold from 0.95 to 0.5 for Wave tracked hand device

* Package Changes
  - Now support Unity 2018.4 or newer due to the Asset Store publish restriction
    - https://assetstore.unity.com/publishing/release-updates#accepted-unity-versions-TjfK
  - Remove asmdef files from package archive


## Changes for v1.13.2:

* New Features
  - Add support for Wave 4.1
  - Add support for Vive Hand Tracking 0.10

* Changes
  - Now Grabbable able to be stretch around using 2 grabbers
    - Requires enabling "multiple grabbers" option
    - Able to scale if min/maxScaleOnStretch set to proper values (min < max)
  - Optimize process creating Wave Render Model by reducing redundent instances

* Bug Fixes
  - Fix Grip axis returns joystick value on Oculus controller (Unity XR only)
  - Fix ControllerRole doesn't map left hand device correctly
  - Fix unable to get Grip/A/B/X/Y button values on Wave controllers (Wave 3.2 only)


## Changes for v1.13.1:

* New Features
  - Add support for Wave 4.0
  - New tracked hand rigs API (Experimental)
  - New tooltip framework (Experimental)

* Known Issue
  - Latest Oculus SDK not compatible (controller not recognized)


## Changes for v1.12.2:

* New Features
  - Add ControllerButtonMask to able to mask out multiple buttin input simultaneously
```csharp
// return true if right controller trigger or pad button pressed
ViveInput.GetAnyPress(HandRole.RightHand, new ControllerButtonMask(ControllerButton.Trigger, ControllerButton.Pad))

// return true if both right controller trigger and pad button pressed
ViveInput.GetAllPress(HandRole.RightHand, new ControllerButtonMask(ControllerButton.Trigger, ControllerButton.Pad))
```

* Bug Fixes
  - Fix error when Wave Essence RenderModel module is installed
  - Fix teleport in wrong height for some device in example 6

## Changes for v1.12.0:

* New Features
  - Add support for Wave XR Plugin
  - Add support for OpenVR XR Plugin preview4 and above
  - Add support for Oculus Link Quest

* Changes
  - Move VIUSettings.asset default path to folder under Assets\VIUSettings\Resources

* Bug Fixes
  - Optimize performance updating define symbols #186
  - Fix recommended settings did not skip check for values that are already using recommended value
  - Fix compile error when using Oculus Integration Unity Plugin v19 and above
  - Fix losing materials in example scenes when Universal Render Pipeline is applied
  - Fix GetReferencedAssemblyNameSet() for Unity 2017.3 and 2017.4
  - Fix SymbolRequirement.reqAnyMethods validation throwing exception when any argument type not found
  - Fix UnityWebRequest.SendWebRequest() in early Unity version (5.4 - 2017.1)

* Known Issue
  - Importing Oculus Integration v19 before upgrading VIU to v1.12.0 will cause compile error and brake VIUSettings.
    - Workaround: manually clear the "Scripting Define Symbols" in Player Settings should solve it


## Changes for v1.11.0:

* New Features
  - Add compatibility with **Unity 2020.1**
  - Add support for **Unity XR Platform** (OpenVR, Oculus, and Windows MR)
    - VIU Settings automatically installs proper XR Loader from **PackageManager** for supported devices
      - **OpenVR XR Plugin**
      - **Oculus XR Plugin**
      - **Windows XR Plugin**
  - Add support for Oculus controller render model (Requires [Oculus SDK](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022))
  - Add new **ControllerButton.DPadCenter** & **ControllerButton.DPadCenterTouch**
    - Also change **VIUSettings.virtualDPadDeadZone** default value from 0.15 to **0.25**
    - This settings is not available in VIU Settings UI yet, but can be manually modified in HTC.UnityPlugin/ViveInputUtility/Resources/**VIUSettings.asset**

* Changes
  - Now use relative path when choosing Oculus Android AndroidManifest.xml file with picker (#175)
  - Update **ControllerManagerSample**
    - Fix support for StickyGrabbables
    - Clean up side cases where updateactivity was needed
    - Add more button options for laser pointer
    - Fix typo
  - Slightly change **ColliderEventCaster**'s behaviour
    - Now IColliderEventPressUpHandler will be treggered only if IColliderEventPressDownHandler also implemented
    - This is for aligning IPointerUpHandler and IPointerDownHandler behaviour
  - Improve **Grabbable**
    - Now **BasicGrabbable** & **StickyGrabbable** able to accept more then one grab button
      - Add new property **primaryGrabButton** so it can be specified with VIU ControllerButton
      - Obsolete property grabButton and add new property **secondaryGrabButton** as a substitute
      - Note that you must setup **ViveColliderEventCaster** properly to send the specified grab button event
  - Improve **Teleportable**
    - Now **Teleportable** able to accept more then one teleport button
      - Add new property **PrimeryTeleportButton** so it can specify with VIU ControllerButton
        - Obsolete property teleportButton and add new property **SecondaryTeleportButton** as a substitute
        - Note that you must setup **ViveRaycaster** properly to send the specified teleport button event
      - Add new property **TriggeredType**
        - **ButtonUp** : perform teleport on button press up (default)
        - **ButtonDown** : perform teleport on button press down
        - **ButtonClick** : perform teleport on button press up only if pointed object when press down/up are the same
      - Add new property **RotateToHitObjectFront**
        - When set to true, teleportation will rotate pivot front to hit object front
      - Add new property **TeleportToHitObjectPivot**
        - When set to true, teleportation will move pivot to hit object pivot instead of the hit point
      - Add new property **UseSteamVRFade**
        - Only works when [SteamVR Plugin](https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647) is installed
        - When set to false or SteamVR Plugin is not installed, the teleportation will delay for half of fadeDuration without fading effect
        - This provides an option for developer to implement their custom fading effect in the OnBeforeTeleport event
      - Add event **OnBeforeTeleport(Teleportable src, RaycastResult hitResult, float delay)**
        - Emit before fade duration start counting down
        - Usually delay argument is half of fade duration (0 if fadeDuration is ignored)
        - Possible usage is to start custom fading effect in this callback
      - Add event **OnAfterTeleport(Teleportable src, RaycastResult hitResult, float delay)**
        - Emit after teleportation is performed
      - Add static event **OnBeforeAnyTeleport(Teleportable src, RaycastResult hitResult, float delay)**
        - Static version of OnBeforeTeleport
        - Emit before OnBeforeTeleport
      - Add static event **OnAfterAnyTeleport(Teleportable src, RaycastResult hitResult, float delay)**
        - Static version of OnAfterTeleport
        - Emit before OnAfterTeleport
      - Add property **AdditionalTeleportRotation**
        - The rotation value will be multiplied on the target (around pivot) when teleporting
        - Possible usage is to set the value (according to other input like pad or joystick direction) in the OnBeforeTeleport callback
      - Add method **AbortTeleport()**
        - Cancel the teleportation during fading
        - Possible usage is to abort in the OnBeforeTeleport callback to perform a custom teleportation
        - Another usage is to interrupt the fade-in effect progress so that the Teleportable able to trigger next teleport event immediatly

* Bug Fixes
  - Fix left Cosmos controller did not bind button X and Y
  - Fix saving of Oculus Android XML path setting (#175)
  - Fix applying some recommended settings didn't trigger editor to compile
  - Fix "recommended settings" button in VIU Settings disappeared after exiting editor play mode


## Changes for v1.10.7:

* Bug Fix
  - Fix device AngularVelocity have zero value when UnityEngineVRModule activated
  - Fix showing wrong model when Oculus Quest Controller connected
  - Fix SteamVR_Action callback function isn't working when VIU input system is activated
  - Fix bumper key isn't working when Vive Cosmos Controller connected
  - Fix controller scroll isn't working in WaveVRModule

* Improvement
  - Now support WaveVR SDK new haptic API
  - Hide most "field never assigned" warnings for serialized field in MonoBehaviour
  - Add static controller model for Valve Index Controller
  - Avoid ListPool from ambiguous reference (when Core RP Library installed)


## Changes for v1.10.6:

* Bug Fix
  - Fix SteamVR Plugin v2.4.5 incompatibility due to action manifest path changes 5fb63198
  - Fix some Oculus(Android) recommend settings not working in Unity 2019.1 or newer 1c8ebf78


## Changes for v1.10.5:

* Improvement
  - Add platform supported define symbols
  - Add VIVE Cosmos support
  - Add Valve Index support
  - Update bindings for Index controller to allow trackpad and thumbstick to work individually 
  - Add Oculus Quest support
  - Add Oculus Rift S support
  - Add Unity XR input supports Oculus (Android)
  - Update support WaveVR SDK 3.0.2 requires VR Supported
  - Add support to WaveVR SDK 3.1
  - Add default and custom AndroidManifest path in VIU Settings
  - Add custom controller model in Simulator
  - Add system input for left Oculus controller (OculusVRModule/UnityNativeModule)
  - Reduce Component Menu path (HTC/VIU -> VIU)
  - [VIUSettings] Update VIVE to OpenVR
  - [VIUSettings] Update VIVE Focus to WaveVR
  - [VIUSettings] Update Oculus (Android) to Oculus Android
  - [VIUSettings] Update Oculus Rift & Touch to Oculus Desktop
  - [VIUSettings] Update Oculus VR SDK download link
  - [VIUSettings] Update Oculus Go to Oculus (Android)

* Bug Fix
  - [GoogleVRModule] Fix null exception and no input issue
  - [PackageManager] Fix assertion error and null exception
  - [VIUSettings] Fix no Oculus (Android) option
  - [WaveVRModule] Fix cannot teleport in ControllerManagerSample scene
  - [SteamVRInputBinding] Fix partial input bindings for VIVE Tracker
  - [SteamVRModule] Fix Override Model bug
  - [OculusVRModule] Fix no Axis2D(Touchpad) for Oculus Go controller
  - [SteamVRModule] Fix no Cosmos grip button input


## Changes for v1.10.4:

* Improvement
  - Replace WWW to UnityWebRequest
  - Pointer3DInputModule no longer set EventSystem to DontDestroyOnLoad
  - Add VRModuleInput2DType
  - Split VIUSettings into partials
  - Add VIVE Cosmos controller enum
  - Improve OculusVR support
  - Add install package button in VIUSettings
  - Add WaveVR SDK 3.0.2 support
  - Add autoScaleReticle setting

* Bug Fix
  - [OculusVRModule] Fix serial number conflict on Oculus device
  - [UnityEngineVRModule] Fix input manager index out of bound
  - [SteamVRModule] Fix swapping controllers will cause input events showing on wrong device
  - [WaveVRModule] Fix left hand mode cannot show controller model
  - [ExampleScene] Fix BodyRole center position transformation
  - [ExampleScene] Fix ResetButton teleports if moved after Start()


## Changes for v1.10.3:

* Improvement
  - [WaveVRModule] Support WaveVR simulator (Enable VIVE Focus support in Editor mode)

* Bug Fix
  - [WaveVRModule] Fix WaveVR tracking glitter issue
  - [WaveVRModule] Fix VIU simulator cannot launch (ONLY enable VIU simulator)


## Changes for v1.10.2:

* Improvement
  - [ViveInput] Add new procedure vibration API (only works with SteamVR v2)
```csharp
using HTC.UnityPlugin.Vive;
ViveInput.TriggerHapticVibrationEx<TRole>(
      TRole role,
      float durationSeconds = 0.01f,
      float frequency = 85f,
      float amplitude = 0.125f,
      float startSecondsFromNow = 0f)
```

* Bug Fix
  - [ViveInput] Fix issue that TriggerHapticPulse is not working
  - [Utility] Fix bug in IndexedSet indexer setter.
  - [WaveVRModule] Fix VIU example freezes after launched
  - [OculusVRModule] Fix OculusVRModule not recognizing GearVR or OculusGO correctly
  - [ExternalCamera] Fix not working with SteamVR v2
  - [ExternalCamera] Fix SteamVR_ExternalCamera setting sceneResolutionScale incorrectly in some cases
  - [ExternalCamera] Fix potential RenderTexture created by SteamVR_ExternalCamera having incorrect ColorSpace


## Changes for v1.10.1:

* New Features
  - SteamVR Plugin v2.0/v2.1 support
  - SteamVR New Input System support ([Guide](https://github.com/ViveSoftware/ViveInputUtility-Unity/wiki/SteamVR-Input-System-Support))

* Improvement
  - Now compatible with Google VR SDK v1.170.0
  - Add ControllerAxis.Joystick for Windows Mixed Reality Motion Controller
  - Extend ControllerButton (DPadXXX are virtual buttons simulated from trackpad/thumbstick values)
    - BKey (Menu)
    - BkeyTouch (MenuTouch)
    - Bumper (Axis3)
    - BumperTouch (Axis3Touch)
    - ProximitySensor
    - DPadLeft
    - DPadLeftTouch
    - DPadUp
    - DPadUpTouch
    - DPadRight
    - DPadRightTouch
    - DPadDown
    - DPadDownTouch
    - DPadUpperLeft
    - DPadUpperLeftTouch
    - DPadUpperRight
    - DPadUpperRightTouch
    - DPadLowerRight
    - DPadLowerRightTouch
    - DPadLowerLeft
    - DPadLowerLeftTouch

* Known Issue
  - When working with SteamVR Plugin v2, VIU can get poses or send vibration pulses for all connected devices, but only able to connect button inputs from up to 2 Vive Controllers or 10 Vive Trackers (due to the limitation of SteamVR input sources). If you know how to work around, please let us know.


## Changes for v1.9.0:

* New Features
  - Wave VR Plugin v2.1.0 support
  - Add Oculus Go support (requires Oculus VR plugin)

* Improvement
  - [WaveVRModule] Add 6 DoF Controller Simulator (Experimental)
    - [More...](https://github.com/ViveSoftware/ViveInputUtility-Unity/wiki/Wave-VR-6-DoF-Controller-Simulator)
  - [RenderModelHook] Now able to load Wave VR generic controller model
  - [Example2] Add drop area that using physics collider
  - [Example3] Make scroll delta tunable from the Editor
    - [issue#49](https://github.com/ViveSoftware/ViveInputUtility-Unity/issues/49)
  - [VIUSettings] Add WVR virtual arms properties
  - [BindingUI] Make un-recognized device visible in the device view (shown in VIVE device icon)
    - [issue#51](https://github.com/ViveSoftware/ViveInputUtility-Unity/issues/51)

* Bug Fix
  - [VRModule] Fix sometimes SteamVRModule activated but not polling any device pose/input


## Changes for v1.8.3:

* Improvement
  - Improve simulator module
    - Add instruction UI
    - Add group control (devices move alone with camera)
    - See [Simulator](https://github.com/ViveSoftware/ViveInputUtility-Unity/wiki/Simulator) for more detail
![VIUSettings in Preference window](https://github.com/ViveSoftware/ViveInputUtility-Unity/blob/gh-pages/assets/img/simulator_01.png)
  - Add ReticlePose.IReticleMaterialChanger interface, better way to indicate different type of pointing object
    - Now you can setup default reticle material in ReticlePose property
![VIUSettings in Preference window](https://github.com/ViveSoftware/ViveInputUtility-Unity/blob/gh-pages/assets/img/retical_mat_changer_default_setup.png)
    - Setup reticle material property in component that implement IReticleMaterialChanger
![VIUSettings in Preference window](https://github.com/ViveSoftware/ViveInputUtility-Unity/blob/gh-pages/assets/img/retical_mat_changer_walkable_setup.png)
  - Add support for WaveVR SDK v2.0.37

* Bug fix
  - Fix tracking device pose not applying scale from the origin transform
  - Fix SteamVRModule reporting wrong input-focus state
  - Fix ViveRigidPoseTracker broke after re-enabled
  - Fix PoseModifier not work correctly ordered by priority value
  

## Changes for v1.8.2

* Bug fix
  - Fix define symbols sometimes not handle correctly when switching build target
  - Disable vr support if device list is empty
  - Add code to avoid NullReferenceException in VRModule
  - Add SetValueByIndex method to IndexedTable
  - Fix StickyGrabbable not working in certain cases


## Changes for v1.8.1:

* New features
  - VIVE Focus Support
    - Required Unity 5.6 or later version
    - Download and install WaveVR SDK Unity plugin from https://hub.vive.com/profile/material-download
    - Enable VIVE Focus support under Edit > Preference > VIU Settings
	
* Bug fix
  - Fix no tracking devices dectected when SteamVR plugin v1.2.3 is installed
  - Fix compile error in Unity 2018.1
  - Fix VIUSettings changes not saved into asset data file


## Changes for v1.8.0:

* New features
  - Daydream Support
  - Simulator
  - New VIU Settings to easy setup project supporting device
    - Open from Editor -> Preferences -> VIU Settings
![VIUSettings in Preference window](https://github.com/ViveSoftware/ViveInputUtility-Unity/blob/gh-pages/assets/img/viusettings_preview_01.png)

* Improvement
  - Now compatible with SteamVR v1.1.1~v1.2.3
  - Move some properties into VIU Settings
    - Removed:
      - ViveRoleBindingHelper.OverrideConfigPath
      - ViveRoleBindingHelper.BindingConfig.apply_bindings_on_load
      - ViveRoleBindingHelper.BindingConfig.toggle_interface_key_code
      - ViveRoleBindingHelper.BindingConfig.toggle_interface_modifier
      - ViveRoleBindingHelper.BindingConfig.interface_prefab
    - Replaced with:
      - VIUSettings.bindingConfigFilePath
      - VIUSettings.autoLoadBindingConfigOnStart
      - VIUSettings.bindingInterfaceSwitchKey
      - VIUSettings.bindingInterfaceSwitchKeyModifier
      - VIUSettings.bindingInterfaceObject

* Bug fix
  - Fix compiler error caused by wrong define symbols.

* VIU Settings
  - Setting changes is saved in project resource folder named VIUSettings.asset.
  - VIU Settings will load default setting automatically if VIUSettings.asset resource not found.

* Daydream Support
  - Requires Android platform support, Unity 5.6 or later and GoogleVR plugin.
  - Requires checking the Daydream support toggle in Edit -> Preferences -> VIUSettings
  - Beware of Daydream controller have less buttons.
  - Remember to define and give the VR origin a headset height for Daydream device user.

* Simulator
  - Simulator is a fake VRModule that spawn/remove fake devices, create fake tracking and fake input events.
  - Requires checking the Simulator support toggle in Edit -> Preferences -> VIUSettings
  - Simulator only enabled when no VR device detected.
  - There are 2 ways to manipulate the fake devices
      - Handle events by script manually
        - HTC.UnityPlugin.VRModuleManagement.VRModule.Simulator.onUpdateDeviceState
          - Invoked each frame when VRModule performs a device state update
          - Write device state into currState argument to manipulate devices
          - Read-only argument prefState preserved device state in last frame
      - Use Keyboard-Mouse control (can be disabled in VIUSettings)
        - Add/Remove/Select devices
          - [0~9] Add and select device N if device N is not selected, otherwise, deselect it
          - [` + 0~5] Add and select device 10+N if device 10+N is not selected, otherwise, deselect it
          - [Shift + 0~9] Remove and deselect device N
          - [Shift + ` + 0~5] Remove and deselect device 10+N
        - Control selected device
          - [W] Move selected device forward
          - [S] Move selected device backward
          - [D] Move selected device right
          - [A] Move selected device left
          - [E] Move selected device up
          - [Q] Move selected device down
          - [C] Roll+ selected device
          - [Z] Roll- selected device
          - [X ] Reset selected device roll
          - [ArrowUp] Pitch+ selected device
          - [ArrowDown] Pitch- selected device
          - [ArrowRight] Yaw+ selected device
          - [ArrowLeft] Yaw- selected device
          - [MouseMove] Pitch/Yaw selected device
          - [MouseLeft] Press Trigger on selected device
          - [MouseRight] Press Trackpad on selected device
          - [MouseMiddle] Press Grip on selected device
          - [Hold Shift + MouseMove] Touch Trackpad on selected device
        - Control HMD
          - [T] Move hmd forward
          - [G] Move hmd backward
          - [H] Move hmd right
          - [F] Move hmd left
          - [Y] Move hmd up
          - [R] Move hmd down
          - [N] Roll+ hmd
          - [V] Roll- hmd
          - [B] Reset hmd roll
          - [I] Pitch+ hmd
          - [K] Pitch- hmd
          - [L] Yaw+ hmd
          - [J] Yaw- hmd
  - Notice that when the simulator is enabled, it is started with 3 fake device
    - [0] HMD (selected)
    - [1] Right Controller
    - [2] Left Controller


## Changes for v1.7.3:

* Bug fix
  - [VRModule] Fix compile error in OculusVRModule.cs #14 
  - [Pointer3D] Fix **IPointerExit** not handled correctly when disabling a raycaster #16 
  - [Pointer3D] Fix **IPointerPressExit**, **IPointerUp** doesn't execute in some cases
    - Add **Pointer3DEventData.pressProcessed** flag to ensure Down/Up processed correctly

* Improvement
  - Add new struct type **HTC.UnityPlugin.Utility.RigidPose** to replace **HTC.UnityPlugin.PoseTracker.Pose**
    - Utility.RigidPose inherit all members in PoseTracker.Pose
    - Add forward, up, right property getter
  - Obsolete struct **HTC.UnityPlugin.PoseTracker.Pose** (will be removed in future version) to avoid type ambiguous with UnityEngine.Pose (new type in Unity 2017.2)
    - If you somehow want to using HTC.UnityPlugin.PoseTracker and UnityEngine.Pose in your script at the same time, please use full type name or add type alias to avoid ambiguous compile error
  ``` csharp
  using HTC.UnityPlugin.PoseTracker;
  using UnityEngine;
  using Pose = UnityEngine.Pose;

  public class MyPoseTracker : BasePoseTracker
  {
      public Pose m_pose;
      ...
  }
  ```
  - Change some recommended setting value
    - Now binding interface switch is recommended as enable only if Steam VR plugin is imported
    - Now external camera interface switch is recommended as enable only if Steam VR plugin is imported
  - Now VIU system game object will be auto generated only if nessary


## Changes for v1.7.2:

* New features
  - Add recommended VR project settings notification window

* Bug fix
  - Fix compile error in Unity 5.5/5.6
  - [VRModule] Fix UnityEngineVRModule not removing disappeared device correctly
    - In Unity 2017.2, InputTracking.GetNodeStates sometimes returns ghost nodes at the beginning of play mode.
  - [Teleportable] Remove a potential null reference
    - This happens when SteamVR plugin is imported and no SteamVR_Camera exist.

* Improvement
  - [Pointer3D] Now Pointer3DInputModule shows status in EventSystem's inspector preview window
  - [Pointer3D] Let Pointer3DInputModule behave more consistent with StandaloneInputModule
    - Now IPointerEnterHandler / IPointerExitHandler only triggered once for each pointer, pressing buttons won't trigger Enter/Exit anymore.
  - [Pointer3D] Add IPointer3DPressEnterHandler / IPointer3DPressExitHandler
    - Their behaviours are like IPointerEnterHandler/IPointerExitHandler, but press enter happend when the button is pressed and moved in, and press exit on button released or pointer moved out.
  - [SteamVRCameraHook] Fix not expending head-eye-ear correctly
  - [VRModule] Fix update timing issue
    - This fix VivePoseTracker tracking is delayed in editor playing mode.


## Changes for v1.7.1:

* New features
  - [ExternalCameraHook] Add externalcamera.cfg config interface
    - The interface is built into project when VIU_EXTERNAL_CAMERA_SWITCH symbol is defined.
    - It is automatically activated with the external camera quad view.

![External Camera Config Interface](https://github.com/ViveSoftware/ViveInputUtility-Unity/blob/gh-pages/assets/img/external_camera_config_interface.png)

* Changes
  - Now you can fully disable the binding interface switch by removing the VIU_BINDING_INTERFACE_SWITCH symbol
    - That means nologer 

* Bug fix
  - [ViveRole] Fix HandRole.ExternalCamera not mapping to tracker in some cases
  - [ViveRole] Fix ViveRole.IMap.UnbindAll() to work correctly
  - [BindingInterface] Fix some info updating and animation issue
  - [VivePose] Now GetPose returns Pose.identity instead of default(Pose) for invalid device


## Changes for v1.7.0:

* New features
  - Add notification when new version released on [Github](https://github.com/ViveSoftware/ViveInputUtility-Unity/releases).
  - Add **VRModule** class to bridge various VR SDK. It currently supports SteamVR plugin, Oculus VR plugin and Unity native VR/XR interface.
    - **void VRModule.Initialize()**: Create and initilize VRModule manager instance.
    - **VRModuleActiveEnum VRModule.activeModule**: Returns the activated module.
    - **IVRModuleDeviceState VRModule.GetCurrentDeviceState(uint deviceIndex)**: Returns the virtual VR device status.
    - **event NewPosesListener VRModule.onNewPoses**: Invoked after virtual VR device status is updated.
    - **event DeviceConnectedListener VRModule.onDeviceConnected**: Invoked after virtual VR device is connected/disconnected.
    - **event ActiveModuleChangedListener VRModule.onActiveModuleChanged**: Invoked when a VR module is activated.
  - New binding interface using overlay UI. By default, the binding interface can be enabled by pressing RightShift + B in play mode.
    - ![Binding UI](https://github.com/ViveSoftware/ViveInputUtility-Unity/blob/gh-pages/assets/img/binding_ui_preview_01.png)
    - ![Binding UI](https://github.com/ViveSoftware/ViveInputUtility-Unity/blob/gh-pages/assets/img/binding_ui_preview_02.png)
    - ![Binding UI](https://github.com/ViveSoftware/ViveInputUtility-Unity/blob/gh-pages/assets/img/binding_ui_preview_03.png)
  - Add define symbols
    - **VIU_PLUGIN**: Defined when Vive Input Utility plugin is imported in the project.
    - **VIU_STEAMVR**: Defined when SteamVR plugin is imported in the project.
    - **VIU_OCULUSVR**: Defined when OculusVR plugin (OVRPlugin) is imported in the project.
    - **VIU_BINDING_INTERFACE_SWITCH**: Define it to let the project be able to switch binding interface by pressing RightShift + B in play mode.
    - **VIU_EXTERNAL_CAMERA_SWITCH**: Define it to let the project be able to switch external camera quad view by pressing RightShift + M in play mode.
  - Add new role HandRole.ExternalCamera (Alias for HandRole.Controller3).
    - By default, it is mapping to the 3rd controller, if 3rd controller not available, then mapping to the first valid generic tracker.
    - ExternalCameraHook uses mapping as the default tracking target.

* New componts
  - [ViveInputVirtualButton] Use this helper component to combine multiple Vive inputs into one virtual button.

* Improvement
  - [ViveInput] Add more controller buttons, use ViveInput.GetPress(role, buttonEnum) to get device button stat
    - **System** (Only visible when sendSystemButtonToAllApps option is on)
    - **Menu**
    - **MenuTouch**
    - **Trigger**
    - **TriggerTouch**
    - **Pad**
    - **PadTouch**
    - **Grip**
    - **GripTouch**
    - **CapSenseGrip**
    - **CapSenseGripTouch**
    - **AKey**
    - **AKeyTouch**
    - **OuterFaceButton** (Alias for Menu)
    - **OuterFaceButtonTouch** (Alias for MenuTouch)
    - **InnerFaceButton** (Alias for Grip)
    - **InnerFaceButtonTouch** (Alias for GripTouch)
  - [ViveInput] Add controller axis enum, use ViveInput.GetAxis(role, axisEnum) to get device axis value
    - **PadX**
    - **PadY**
    - **Trigger**
    - **CapSenseGrip**
    - **IndexCurl**
    - **MiddleCurl**
    - **RingCurl**
    - **PinkyCurl**
  - [ViveRole] Role mapping/binding mechanism is improved and become more flexible.
    - Now different devices can bind to same role at the same time.
    - If a unconnected device is bound to a role, that role can still map to other connected device.
  - [ViveRole] Obsolete functions that retrieve device status and property, use static API in VRModule instead.
    - **ViveRole.TryGetDeviceIndexBySerialNumber**: Use VRModule.TryGetDeviceIndex instead.
    - **ViveRole.GetModelNumber**: Use VRModule.GetCurrentDeviceState(deviceIndex).modelNumber instead
    - **ViveRole.GetSerialNumber**: Use VRModule.GetCurrentDeviceState(deviceIndex).serialNumber instead
    - **ViveRole.GetDeviceClass**: Use VRModule.GetCurrentDeviceState(deviceIndex).deviceClass instead
  - [ViveRoleBindingsHelper] Now will automatically load bindings from "vive_role_bindings.cfg", no need to be in the scene to work.
  - [RenderModelHook] Add override model and shader option.
  - [ExternalCameraHook] Now ExternalCameraHook will track the HandRole.ExternalCamera by default.
  - [ExternalCameraHook] Now will be added into scene automatically if "externalcamera.cfg" exist when start playing, no need to add to scene manually.
  - [ExternalCameraHook] You can now enable static External Camera quad view (without tracking to a device) if
    1. VIU_EXTERNAL_CAMERA_SWITCH symbol is defined.
	2. externalcamera.cfg exist.
	3. RightShift + M pressed in play mode.
  - [BasicGrabbable, StickyGrabbable, Draggable] Add unblockable grab/drag option.

* Bug fix
  - [ViveRoleProperty] Fix not handling serialized data right in inspector.
  - [PoseEaser] Now use unscaled time instead to avoid from being effected by time scale.


## Changes for v1.6.4:

* Resloves tracking pose not updating in Unity 5.6

* Add SnapOnEnable option to ViveRigidPoseTracker component

* Add mouse button mapping options to ViveRaycaster component

* Fix crashes when clicking dropdown UI in RoleBindingExample scene

* Fix auto-bake-lightmap errors in example scenes

* Correct rigidbody null check in Draggable and Grabbable scripts


## Changes for v1.6.3:

* Fix ViveRoleProperty returns wrong type & value [issue#9](https://github.com/ViveSoftware/ViveInputUtility-Unity/issues/9)

* Now Teleportable component will find target & pivot automatically [issue#8](https://github.com/ViveSoftware/ViveInputUtility-Unity/issues/8)

* Remove warning in LineRenderer


## Changes for v1.6.2:

* Fix remapping errors from HandRoleHandler [issue#1](https://github.com/ViveSoftware/ViveInputUtility-Unity/issues/1)

* Fix ViveRoleProperty.ToRole always returns Invalid [issue#6](https://github.com/ViveSoftware/ViveInputUtility-Unity/issues/6)

* Fix ViveRaycaster not working when app loses focus [issue#7](https://github.com/ViveSoftware/ViveInputUtility-Unity/issues/7)


## Changes for v1.6.0:

* New ViveRole System
    - ViveRole is a mapping system that relate logic roles to OpenVR device indices.
    - Each role has their own auto-mapping logic, and binding API allow user to customize the relation.
        - Both mapping (role, device index) binding (role, device serial number) are one-on-one relation
        - When a device serial number is binding to a role, it means that role is always mapping to the specific device
        - If the bound device is disconnected, the bound role will not mapping to any device index (invalid).
    - Currently there are 4 built-in roles:
        - DeviceRole: role that mapping to all 16 devices, ordered exactly same as device index.
        - HandRole: role related to standard Vive controllers, with basic RightHand/LeftHand recognition.
        - TrackerRole: role related to Vive trackers, first conntected tracker will be mapping to Tracker1.
        - BodyRole: role related to devices that tracking human limbs.
    - Creating custom role in an instant by adding ViveRoleEnumAttribute to your enum type
    - Customizing auto-mapping logic by implementing ViveRoleHandler\<EnumType\> and call ViveRole.AssignMapHandler()

* New query APIs that accept any ViveRoles, ex.
    - Use ViveRole.GetDeviceIndexEx(TrackerRole.Tracker1) to get tracker's device index.
    - Use VivePose.GetPoseEx(TrackerRole.Tracker1) to get tracker's tracking data
    - Use ViveInput.GetPressEx(TrackerRole.Tracker1, ControllerButton.Trigger) to get tracker's trigger event.
    - Use ViveInput.AddListenerEx(TrackerRole.Tracker1, ControllerButton.Trigger, ButtonEventType.Press) to listen tracker's trigger event.

* New sample scene "RoleBindingExample"
    - This sample scene demonstrate how to bind device a role and save/load those bindings

* New ViveRoleBindingsHelper helper component
    - Adding this component to scene to auto-load bindings.
    - Call function SaveRoleBindings(filePath) to save bindings manually.
    - Call function LoadRoleBindings(filePath) to load bindings manually.

* New RenderModelHook helper component
    - This script creates and handles SteamVR_RenderModel, so you can show render model specified by ViveRole insdead of device index.

* New ExternalCameraHook helper component
    - This script creates and handles SteamVR_ExternalCamera, and let camera tracking device specified by ViveRole insdead of device index.
    - Setup step-by-step
	1. Add a file called externalcamera.cfg in the root of your project. (the config file sample can be found [here](https://steamcommunity.com/app/358720/discussions/0/405694031549662100/))
    2. Add ExternalCameraHook component into your scene. (don't remove the auto-generated SteamVR_Render component)
	3. Select ExternalCameraHook gameobject and set the device role in inspector. (or set in script, ex. ExternalCameraHook.viveRole.SetEx(TrackerRole.Tracker1))
          - If you are using 3rd Vive standard controller as external camera, set to HandRole.Controller3 (recommended)
	    - If you are using ViveTracker as external camera, set to TrackerRole.Tracker1 (recommended)
    4. (Optional) Bind the external camera tracking device to the role
	    1. Open "RoleBindingExample" scene
	    2. Scan the specific device (for external camera)
	    3. Bind to the specific role (ex. HandRole.Controller3 or TrackerRole.Tracker1)
	    4. Save bindings
	    5. Back to your project scene
	    6. Add ViveRoleBindingsHelper component into your scene. (to load bindings automatically)
	    7. Now external camera should always tracking at the device you wanted.


## Changes for v1.5.3:

* Make compatible with SteamVR plugin 1.2.1
* Fix a bug in ColliderEventCaster that cause crash when disabling event caster and handling events at the same time.
* Change default teleportButton in Teleportable to TeleportButton.Pad instead of TeleportButton.Trigger
* Containers optimize
    - Re-write IndexedTable, should be more efficient
    - Add read-only interface


## Changes for v1.5.2:

* Make compatible with SteamVR plugin 1.2.0


## Changes for v1.5.1:

* Update guide document
    - Reveal used namespace in some example scripts.
    - Add ready-to-used component list.

* New controllers prefab that handles both hand EventRaycaster, ColliderEventCaster, guidelines and models
    - Hide controllers' models when grabbing or dragging
    - Enable EventRaycaster on pad touched, otherwise enable ColliderEventCaster

* Pointer3D
    - Expose Pointer3DRaycaster at Pointer3DEventData.raycaster, to get Raycaster from eventData easily.
    - Move dragThreshold and clickInterval settings from Pointer3DInputModule to Pointer3DRaycaster.
    - Re-design RaySegmentGenerator. Now RaycastMode setting is replaced by applying ProjectionGenerator & ProjectileGenerator component with Pointer3DRaycaster.
    Add or enable only one generator at a time, or which generator used by the raycaster is unexpected.
    Also customize your own generators by implementing BaseRaySegmentGenerator.

* ColliderEvent
    - Now OnColliderEventClick won't invoke if caster has leaved the pressed object.
    - Fix a bug in ColliderEventCaster that doesn't handle hovered colliders correctly.
    - Fix a bug that ColliderEventCaster doesn't handle event correctly when disable.
    - Add ColliderEventTrigger component, work just like built-in EventTrigger


* Add Pointer3DEventData extensions
```csharp
      Pointer3DRaycaster PointerEventData.GetRaycaster3D()
	                bool PointerEventData.TryGetRaycaster3D(out Pointer3DRaycaster raycaster)
	        TRaycaster3D PointerEventData.GetRaycaster3D<TRaycaster3D>()
	                bool PointerEventData.TryGetRaycaster3D<TRaycaster3D>(out TRaycaster3D raycaster)
```

* Add ColliderEventData extensions
```csharp
            TEventCaster ColliderEventData.GetEventCaster<TEventCaster>()
	                bool ColliderEventData.TryGetEventCaster<TEventCaster>(out TEventCaster eventCaster)
```

* Add VivePointerEventData extensions
```csharp
                    bool PointerEventData.IsViveButton(HandRole hand)
                    bool PointerEventData.IsViveButton(ControllerButton button)
                    bool PointerEventData.IsViveButton(HandRole hand, ControllerButton button)
	                bool PointerEventData.TryGetViveButtonEventData(out VivePointerEventData viveEventData)
```

* Add ViveColliderEventData extensions
```csharp
                    bool ColliderEventData.IsViveButton(HandRole hand)
                    bool ColliderEventData.IsViveButton(ControllerButton button)
                    bool ColliderEventData.IsViveButton(HandRole hand, ControllerButton button)
                    bool ColliderEventData.TryGetViveButtonEventData(out ViveColliderButtonEventData viveEventData)
                    bool ColliderAxisEventData.IsViveTriggerValue()
                    bool ColliderAxisEventData.IsViveTriggerValue(HandRole hand)
                    bool ColliderAxisEventData.TryGetViveTriggerValueEventData(out ViveColliderTriggerValueEventData viveEventData)
                    bool ColliderAxisEventData.IsVivePadAxis()
                    bool ColliderAxisEventData.IsVivePadAxis(HandRole hand)
                    bool ColliderAxisEventData.TryGetVivePadAxisEventData(out ViveColliderPadAxisEventData viveEventData)
```

* Improve BasicGrabbable component, and Draggable(in 3D Drag example) as well
    - Now grabbed object can collide properly into other colliders.
    - Now handles multiple grabbers.
    - Add speed factor parameter to adjast grabbed object following speed.
    - Add afterGrabbed & beforeRelease event handler.

* Add dragging state material in MaterialChanger.

* Fix a bug in Teleportable so that GuideLineDrawer won't draw in wrong position.

* New containers in Utiliy
```csharp
    IndexedSet<TKey>                  // container that combinds set and list, order is not preserved, removing complexity is O(1)
    OrderedIndexedSet<TKey>           // container that combinds set and list, order is preserved, removing complexity is O(N)
    IndexedTable<TKey, TValue>        // container that combinds dictionary and list, order is not preserved, removing complexity is O(1)
    OrderedIndexedTable<TKey, TValue> // container that combinds dictionary and list, order is preserved, removing complexity is O(N)
```


## Changes for v1.5.0:

* Add new raycast mode for Pointer3DRaycaster
    - Default : one simple raycast
    - Projection : raycast in a constant distance then raycast toward gravity
    - Projectile : raycast multiple times alone the projectile curve using initial velocity 

* Add ViveInput.GetCurrentRawControllerState and ViveInput.GetPreviousRawControllerState.

* BaseRaycastMethod now registered into Pointer3DRaycaster at Start instead of Awake.

* Remove RequireComponent(typeof(BaseMultiMethodRaycaster)) attribute from BaseRaycastMethod.

* Pointer3DRaycaster now registered into Pointer3DInputModule at Start instead of Awake.

* EventCamera for Pointer3DRaycaster now place at root, instead of child of Pointer3DRaycaster.

* New ColliderEventSyatem. Hover thins using collider (instead of raycast), send button events to them, handle events by EventSystem-like handlers.
    - IColliderEventHoverEnterHandler
    - IColliderEventHoverExitHandler
    - IColliderEventPressDownHandler
    - IColliderEventPressUpHandler
    - IColliderEventPressEnterHandler
    - IColliderEventPressExitHandler
    - IColliderEventClickHandler
    - IColliderEventDragStartHandler
    - IColliderEventDragUpdateHandler
    - IColliderEventDragEndHandler
    - IColliderEventDropHandler
    - IColliderEventAxisChangeHandler

* New example scene to demonstrate how ColliderEvent works.
    - Assets\HTC.UnityPlugin\ViveInputUtility\Examples\5.ColliderEvent\ColliderEvent.unity

* Update tutorial & guide document.


## Changes for v1.4.7:

* Now HandRole defines more then 2 controllers.

* Add some comment and description to public API.


## Changes for v1.4.6:

* Fix a bug in the examples, now reticle posed correctly when scaling VROrigin.


## Changes for v1.4.5:

* Fix a rare issue in Pointer3DInputModule when processing event raycast.


## Changes for v1.4.4:

* Remove example 5 & 6 from package for release(still available in full package), since they are not good standard practices in VR for avoiding motion sickness by moving the player.

* Reset pointer's tranform(to align default laser pointer direction) in examples.

* Adjust default threshold to proper value in PoseStablizer & Pointer3DInputModule.

* Fix a bug in Pointer3DRaycaster that causes other input module to drive Pointer3DRaycaster(witch should be only driven by Poinster3DInputModule).

* Now Pointer3DRaycaster can optionally show event raycast line in editor for debugging.

* Add step by step tutorial document and example scene.

* Replace about document with developer guide.


## Changes for v1.4.3:

* Update usage document(rewrite sample code).

* Add copyright terms.

* Define new controller button : FullTrigger(consider pressed only when trigger value is 1.0).

* Fix ViveInput.GetPadPressDelta and ViveInput.GetPadTouchDelta to work properly.

* Add scroll delta scale property for ViveRaycaster(to adjust scrolling sensitivity).

* Add PoseEaser effect settings and PoseEaserEditor to show properties.

* Add ViveInput.TriggerHapticPulse for triggering controller vibration.


## Changes for v1.4.2:

* Update usage document.

* Reorder parameters in Pose.SetPose.

* Now click interval can be configured by setting ViveInput.clickInterval.


## Changes for v1.4.1:

* Fix wrong initial status for ViveRole and ViveInput.

* Example: showLocalAvatar (property for LANGamePlayer) won't hide shadow (hide mesh only) if set to false.


## Changes for v1.4.0:

* Separate PoseTracker module from VivePose.

* New tracking effect PoseFreezer.

* Reorganize folders.


## Changes for v1.3.0:

* VivePose is now pure static class (Since Unity 5.3.5 fixed issue with double rendering of canvas on Vive VR, PoseUpdateMode is no longer needed).

* New components CanvasRaycastMethod and CanvasRaycastTarget.
    - CanvasRaycastMethod works like GraphicRaycastMethod, but use CanvasRaycastTarget component to target canvases, instead of asigning canvas property once at a time.


## Changes for v1.2.0:

* Fix misspelling from ConvertRoleExtention to ConvertRoleExtension

* New containter class IndexedSet\<T\>

* New class ObjectPool\<T\> and relative class ListPool\<T\>, DictionaryPool\<T\>, IndexedSetPool\<T\>, to reduce allocating new containers.

* Change some data structure from LinkList to InedexedSet (VivePose, Pointer3DInputModule, BaseMultiMethodRaycaster, BaseVivePoseTracker).

* Rewrite GraphicRaycastMethod to align GraphicRaycaster's behaviour.


## Changes for v1.1.0:

* New API VivePose.SetPose().

* New API VivePose.GetVelocity().

* New API VivePose.GetAngularVelocity().

* Fix some null reference in VivePose.