using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR;

[RequireComponent(typeof(InputField))]
public class OverlayKeyboardSample : MonoBehaviour
    , ISelectHandler
    , IDeselectHandler
{
    private static OverlayKeyboardSample activeKeyboard;
    private static StringBuilder strBuilder;

    public bool minimalMode;

    private InputField textEntry;
    private string text = "";

    static OverlayKeyboardSample()
    {
        SteamVR_Events.SystemAction(EVREventType.VREvent_KeyboardCharInput, OnKeyboardCharInput).Enable(true);
        SteamVR_Events.SystemAction(EVREventType.VREvent_KeyboardClosed, OnKeyboardClosed).Enable(true);
    }

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

    public void OnSelect(BaseEventData eventData)
    {
        ShowKeyboard(this);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        HideKeyboard();
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
                vr.overlay.ShowKeyboard(0, 0, "Description", 256, caller.text, caller.minimalMode, 0);
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

    private static void OnKeyboardCharInput(VREvent_t arg)
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

        var input = Encoding.UTF8.GetString(inputBytes, 0, len);

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
                if (strBuilder == null) { strBuilder = new StringBuilder(1024); }

                vr.overlay.GetKeyboardText(strBuilder, 1024);
                activeKeyboard.text = strBuilder.ToString();
                activeKeyboard.textEntry.text = activeKeyboard.text;

                strBuilder.Length = 0;
            }
        }
    }

    private static void OnKeyboardClosed(VREvent_t arg)
    {
        activeKeyboard = null;
    }
}
