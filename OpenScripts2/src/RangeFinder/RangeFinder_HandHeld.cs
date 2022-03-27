using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace OpenScripts2
{
    class RangeFinder_HandHeld : OpenScripts2_BasePlugin
    {
        public FVRFireArmAttachment Attachment;
        public GameObject LaserSystem = null;
        public AudioEvent Sounds = null;

        private bool _isOn = false;
        private bool _lockControls = false;

        public void Start()
        {
            Attachment = this.gameObject.GetComponent<FVRFireArmAttachment>();
        }
        public void Update()
        {
            FVRViveHand hand = Attachment.m_hand;
            if (hand != null && Attachment.curMount == null)
            {
                if (hand.Input.TriggerDown && !_lockControls) StartCoroutine("MeasureOnce");
                else if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes,Vector2.up) < 45f && !_lockControls) ToggleMeasure();
                else if (hand.Input.TouchpadUp && _lockControls) _lockControls = false;
            }
            else if (Attachment.curMount != null)
            {
                if (Attachment.AttachmentInterface.m_hand != null)
                {
                    if (Attachment.AttachmentInterface.m_hand.Input.TouchpadDown && Vector2.Angle(Attachment.AttachmentInterface.m_hand.Input.TouchpadAxes, Vector2.up) < 45f) ToggleMeasure();
                    else if (Attachment.AttachmentInterface.m_hand.Input.TouchpadDown && Vector2.Angle(Attachment.AttachmentInterface.m_hand.Input.TouchpadAxes, Vector2.down) < 45f) _lockControls = true;
                }
            }
        }

        public IEnumerator MeasureOnce()
        {
            if (!_isOn)
            {
                ToggleMeasure();
            }
            yield return 0;
            ToggleMeasure();
        }

        public void ToggleMeasure()
        {
            switch (_isOn)
            {
                case false:
                    LaserSystem.SetActive(true);
                    SM.PlayGenericSound(Sounds,transform.position);
                    _isOn = true;
                    break;
                case true:
                    LaserSystem.SetActive(false);
                    _isOn = false;
                    break;
                default:
                    break;
            }
        }
    }
}
