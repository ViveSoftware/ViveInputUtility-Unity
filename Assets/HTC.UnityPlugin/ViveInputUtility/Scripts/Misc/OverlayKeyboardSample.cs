//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE
using Valve.VR;
#endif

[RequireComponent(typeof(InputField))]
public class OverlayKeyboardSample : MonoBehaviour
    , ISelectHandler
    , IDeselectHandler
{
    public bool minimalMode;

    public void OnSelect(BaseEventData eventData)
    {
        ShowKeyboard(this);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        HideKeyboard();
    }

#if VIU_STEAMVR && UNITY_STANDALONE

    private static OverlayKeyboardSample activeKeyboard;
    private static System.Text.StringBuilder strBuilder;

    private InputField textEntry;
    private string text = "";

    protected virtual void Start()
    {
        textEntry = GetComponent<InputField>();
    }

    protected virtual void OnDisable()
    {
        if (activeKeyboard == this)
        {
            HideKeyboard();
        }
    }

    static OverlayKeyboardSample()
    {
#if VIU_STEAMVR_1_2_1_OR_NEWER
        SteamVR_Events.System(Valve.VR.EVREventType.VREvent_KeyboardCharInput).AddListener(OnKeyboardCharInput);
        SteamVR_Events.System(Valve.VR.EVREventType.VREvent_KeyboardClosed).AddListener(OnKeyboardClosed);
#elif VIU_STEAMVR_1_2_0_OR_NEWER
        SteamVR_Events.System("KeyboardCharInput").AddListener(OnKeyboardCharInput);
        SteamVR_Events.System("KeyboardClosed").AddListener(OnKeyboardClosed);
#elif VIU_STEAMVR_1_1_1
        SteamVR_Utils.Event.Listen("KeyboardCharInput", OnKeyboardCharInputArgs);
        SteamVR_Utils.Event.Listen("KeyboardClosed", OnKeyboardClosedArgs);
#endif
    }

    public static void ShowKeyboard(OverlayKeyboardSample caller)
    {
        if (activeKeyboard != null)
        {
            HideKeyboard();
        }

        if (activeKeyboard == null)
        {
            var vr = SteamVR.instance;
            if (vr != null)
            {
                caller.text = caller.textEntry.text;
#if VIU_STEAMVR_2_6_0_OR_NEWER
                uint flag = 0;
                if (caller.minimalMode)
                {
                    flag = (uint)EKeyboardFlags.KeyboardFlag_Minimal;
                }
                vr.overlay.ShowKeyboard(0, 0, flag, "Description", 256, caller.text, 0);
#else
                vr.overlay.ShowKeyboard(0, 0, "Description", 256, caller.text, caller.minimalMode, 0);
#endif
            }

            activeKeyboard = caller;
        }
    }

    public static void HideKeyboard()
    {
        if (activeKeyboard != null)
        {
            var vr = SteamVR.instance;
            if (vr != null)
            {
                vr.overlay.HideKeyboard();
            }
        }

        activeKeyboard = null;
    }
#if VIU_STEAMVR_1_1_1
    private static void OnKeyboardCharInputArgs(params object[] args) { OnKeyboardCharInput((Valve.VR.VREvent_t)args[0]); }

    private static void OnKeyboardClosedArgs(params object[] args) { OnKeyboardClosed((Valve.VR.VREvent_t)args[0]); }
#endif
    private static void OnKeyboardCharInput(Valve.VR.VREvent_t arg)
    {
        if (activeKeyboard == null) { return; }

        var keyboard = arg.data.keyboard;

        var inputBytes = new byte[]
        {
            keyboard.cNewInput0,
            keyboard.cNewInput1,
            keyboard.cNewInput2,
            keyboard.cNewInput3,
            keyboard.cNewInput4,
            keyboard.cNewInput5,
            keyboard.cNewInput6,
            keyboard.cNewInput7
        };

        var len = 0;
        for (; inputBytes[len] != 0 && len < 7; len++) ;

        var input = System.Text.Encoding.UTF8.GetString(inputBytes, 0, len);

        if (activeKeyboard.minimalMode)
        {
            if (input == "\b")
            {
                if (activeKeyboard.text.Length > 0)
                {
                    activeKeyboard.text = activeKeyboard.text.Substring(0, activeKeyboard.text.Length - 1);
                }
            }
            else if (input == "\x1b")
            {
                // Close the keyboard
                HideKeyboard();
            }
            else
            {
                activeKeyboard.text += input;
            }

            activeKeyboard.textEntry.text = activeKeyboard.text;
        }
        else
        {
            var vr = SteamVR.instance;
            if (vr != null)
            {
                if (strBuilder == null) { strBuilder = new System.Text.StringBuilder(1024); }

                vr.overlay.GetKeyboardText(strBuilder, 1024);
                activeKeyboard.text = strBuilder.ToString();
                activeKeyboard.textEntry.text = activeKeyboard.text;

                strBuilder.Length = 0;
            }
        }
    }

    private static void OnKeyboardClosed(Valve.VR.VREvent_t arg)
    {
        activeKeyboard = null;
    }

#else

    protected virtual void Start()
    {
        Debug.LogWarning("SteamVR plugin not found! install it to enable OverlayKeyboard!");
    }

    protected virtual void OnDisable() { }

    public static void ShowKeyboard(OverlayKeyboardSample caller) { }

    public static void HideKeyboard() { }
#endif
}
