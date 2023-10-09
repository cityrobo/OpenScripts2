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
            Attachment = gameObject.GetComponent<FVRFireArmAttachment>();
        }
        public void Update()
        {
            FVRViveHand hand = Attachment.m_hand;
            if (hand != null && Attachment.curMount == null)
            {
                if (hand.Input.TriggerDown && !_lockControls) StartCoroutine(MeasureOnce());
                else if (TouchpadDirDown(hand, Vector2.up) && !_lockControls) ToggleMeasure();
                else if (hand.Input.TouchpadUp && _lockControls) _lockControls = false;
            }
            else if (Attachment.curMount != null)
            {
                if (Attachment.AttachmentInterface.m_hand != null)
                {
                    if (TouchpadDirDown(Attachment.AttachmentInterface.m_hand,Vector2.up)) ToggleMeasure();
                    else if (TouchpadDirDown( Attachment.AttachmentInterface.m_hand, Vector2.down)) _lockControls = true;
                }
            }
        }

        private IEnumerator MeasureOnce()
        {
            if (!_isOn)
            {
                ToggleMeasure();
            }
            yield return null;
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
            }
        }
    }
}
