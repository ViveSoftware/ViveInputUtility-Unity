//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Vive.BindingInterface
{
    public class BindingInterfaceSpriteManager : MonoBehaviour
    {
        public const string MODEL_ICON_SPRITE_PREFIX = "binding_ui_icons_";
        public const string MODEL_PROJECT_SPRITE_PREFIX = "binding_ui_project_";

        private static Dictionary<string, Sprite> s_spriteTable;

        [SerializeField]
        private string[] texturePath;

        private void Awake()
        {
            if (s_spriteTable != null) { return; }

            s_spriteTable = new Dictionary<string, Sprite>();

            foreach (var texture in texturePath)
            {
                if (string.IsNullOrEmpty(texture)) { continue; }

                var atlas = Resources.LoadAll<Sprite>(texture);
                foreach (var sprite in atlas)
                {
                    s_spriteTable.Add(sprite.name, sprite);
                }
            }
        }

        public static Sprite GetSprite(string spriteName)
        {
            Sprite sprite;
            if (s_spriteTable == null || !s_spriteTable.TryGetValue(spriteName, out sprite)) { return null; }
            return sprite;
        }

        public static void SetupDeviceIcon(Image image, VRModuleDeviceModel deviceModel, bool connected)
        {
            string spriteName;
            switch (deviceModel)
            {
                case VRModuleDeviceModel.KnucklesLeft:
                    spriteName = MODEL_ICON_SPRITE_PREFIX + VRModuleDeviceModel.KnucklesRight;
                    image.transform.localScale = new Vector3(-1f, 1f, 1f);
                    break;
                case VRModuleDeviceModel.OculusTouchLeft:
                    spriteName = MODEL_ICON_SPRITE_PREFIX + VRModuleDeviceModel.OculusTouchRight;
                    image.transform.localScale = new Vector3(-1f, 1f, 1f);
                    break;
                default:
                    spriteName = MODEL_ICON_SPRITE_PREFIX + deviceModel.ToString();
                    image.transform.localScale = new Vector3(1f, 1f, 1f);
                    break;
            }

            image.sprite = GetSprite(spriteName);

            if (connected)
            {
                image.color = new Color32(0x53, 0xBB, 0x00, 0xFF);
            }
            else
            {
                image.color = new Color32(0x56, 0x56, 0x56, 0xFF);
            }
        }

        public static void SetupTrackingDeviceIcon(Image image, IVRModuleDeviceState deviceState, bool bound)
        {
            string spriteName;
            var scale = Vector3.one;
            switch (deviceState.deviceModel)
            {
                case VRModuleDeviceModel.KnucklesLeft:
                    spriteName = MODEL_PROJECT_SPRITE_PREFIX + VRModuleDeviceModel.KnucklesRight;
                    scale.x = -1f;
                    break;
                case VRModuleDeviceModel.OculusTouchLeft:
                    spriteName = MODEL_PROJECT_SPRITE_PREFIX + VRModuleDeviceModel.OculusTouchRight;
                    scale.x = -1f;
                    break;
                default:
                    switch (deviceState.deviceClass)
                    {
                        case VRModuleDeviceClass.HMD:
                            spriteName = "binding_ui_project_HMD";
                            break;
                        case VRModuleDeviceClass.Controller:
                            spriteName = MODEL_PROJECT_SPRITE_PREFIX + VRModuleDeviceModel.ViveController;
                            break;
                        case VRModuleDeviceClass.GenericTracker:
                            spriteName = MODEL_PROJECT_SPRITE_PREFIX + VRModuleDeviceModel.ViveTracker;
                            break;
                        default:
                            spriteName = MODEL_PROJECT_SPRITE_PREFIX + deviceState.deviceModel.ToString();
                            break;
                    }
                    break;
            }

            var sprite = GetSprite(spriteName);
            //Debug.Log("SetupTrackingDeviceIcon " + deviceModel + " " + bound + " " + spriteName + " null?" + (sprite == null));
            if (sprite == null)
            {
                image.enabled = false;
            }
            else
            {
                image.enabled = true;
                image.sprite = sprite;

                var spriteRect = sprite.rect;
                var spritePivot = sprite.pivot;
                image.SetNativeSize();
                image.rectTransform.sizeDelta *= 0.2326f;
                image.rectTransform.pivot = new Vector2(spritePivot.x / spriteRect.width, spritePivot.y / spriteRect.height);

                image.transform.localScale = scale;

                if (bound)
                {
                    image.color = new Color32(0x21, 0xE3, 0xD8, 0xFF);
                }
                else
                {
                    image.color = new Color32(0x9F, 0xEC, 0x28, 0xFF);
                }
            }
        }
    }
}