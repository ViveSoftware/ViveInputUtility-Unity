//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Vive
{
    public class DefaultTooltipPrefab : MonoBehaviour
    {
        public Transform lineOrigin;
        public Transform lineEnd;
        public Transform lineCylinder;
        public Transform labelOrigin;
        public Transform labelAnchor;
        public Text labelText;
        public TextMesh labelTextMesh;
        public float labelWidth;
        public float labelHeight;

        public void ShowRenderData(TooltipRig tooltipRig, DefaultTooltipRenderData data)
        {
            lineOrigin.gameObject.SetActive(true);
            lineEnd.gameObject.SetActive(true);
            labelOrigin.gameObject.SetActive(true);

            var start = tooltipRig.buttonPosition + 0.005f * tooltipRig.buttonNormal;
            var end = tooltipRig.labelPosition;
            var lineVector = end - start;
            lineOrigin.localPosition = start;
            lineOrigin.localRotation = Quaternion.LookRotation(lineVector);
            lineCylinder.localScale = new Vector3(1f, 1f, lineVector.magnitude);
            lineEnd.localPosition = end;
            labelOrigin.localPosition = tooltipRig.labelPosition;
            labelOrigin.localRotation = Quaternion.LookRotation(tooltipRig.labelNormal, tooltipRig.labelUp);
            if (labelText != null) { labelText.text = data.labelText; }
            if (labelTextMesh != null) { labelTextMesh.text = data.labelText; }

            switch (tooltipRig.labelAnchor)
            {
                case TextAnchor.UpperLeft:
                    labelAnchor.localPosition = new Vector3(labelWidth * 0.5f, -labelHeight * 0.5f, 0f);
                    break;
                case TextAnchor.UpperCenter:
                    labelAnchor.localPosition = new Vector3(0f, -labelHeight * 0.5f, 0f);
                    break;
                case TextAnchor.UpperRight:
                    labelAnchor.localPosition = new Vector3(-labelWidth * 0.5f, -labelHeight * 0.5f, 0f);
                    break;
                case TextAnchor.MiddleLeft:
                    labelAnchor.localPosition = new Vector3(labelWidth * 0.5f, 0f, 0f);
                    break;
                case TextAnchor.MiddleCenter:
                    labelAnchor.localPosition = new Vector3(0f, 0f, 0f);
                    break;
                case TextAnchor.MiddleRight:
                    labelAnchor.localPosition = new Vector3(-labelWidth * 0.5f, 0f, 0f);
                    break;
                case TextAnchor.LowerLeft:
                    labelAnchor.localPosition = new Vector3(labelWidth * 0.5f, labelHeight * 0.5f, 0f);
                    break;
                case TextAnchor.LowerCenter:
                    labelAnchor.localPosition = new Vector3(0f, labelHeight * 0.5f, 0f);
                    break;
                case TextAnchor.LowerRight:
                    labelAnchor.localPosition = new Vector3(-labelWidth * 0.5f, labelHeight * 0.5f, 0f);
                    break;
            }

            //var start = tooltipRig.buttonPosition + 0.005f * tooltipRig.buttonNormal;
            //var end = tooltipRig.labelPosition - 0.005f * tooltipRig.labelNormal;
            //var lineVector = end - start;
            //var labelForward = -tooltipRig.labelNormal;
            //lineOrigin.localPosition = start;
            //lineOrigin.localRotation = Quaternion.LookRotation(lineVector);
            //lineCylinder.localScale = new Vector3(1f, 1f, lineVector.magnitude);
            //lineEnd.localPosition = end;
            //labelOrigin.localPosition = tooltipRig.labelPosition;
            //labelOrigin.localRotation = Quaternion.LookRotation(labelForward, Vector3.forward);
            //if (labelText != null) { labelText.text = data.labelText; }
            //if (labelTextMesh != null) { labelTextMesh.text = data.labelText; }
        }

        public void HideRenderData()
        {
            lineOrigin.gameObject.SetActive(false);
            lineEnd.gameObject.SetActive(false);
            labelOrigin.gameObject.SetActive(false);
        }
    }
}