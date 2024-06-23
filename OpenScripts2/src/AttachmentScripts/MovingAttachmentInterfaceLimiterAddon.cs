using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    public class MovingAttachmentInterfaceLimiterAddon : OpenScripts2_BasePlugin
    {
        public FVRFireArmAttachment Attachment;

        public Vector2 NewXLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
        public Vector2 NewYLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
        public Vector2 NewZLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);

        private Vector2 _oldLowerLimit;
        private Vector2 _oldUpperLimit;

        private MovingFireArmAttachmentInterface _interface;

        public void Update()
        {
            if (_interface == null && Attachment.curMount != null && Attachment.curMount.MyObject is FVRFireArmAttachment curAttachment)
            {
                _interface = curAttachment.AttachmentInterface as MovingFireArmAttachmentInterface;

                if (_interface != null)
                {
                    _oldLowerLimit = _interface.LowerLimit;
                    _oldUpperLimit = _interface.UpperLimit;

                    _interface.LowerLimit = new Vector3(NewXLimits.x, NewYLimits.x, NewZLimits.x);
                    _interface.UpperLimit = new Vector3(NewXLimits.y, NewYLimits.y, NewZLimits.y);
                }
            }
            else if (_interface != null && Attachment.curMount == null)
            {
                _interface.LowerLimit = _oldLowerLimit;
                _interface.UpperLimit = _oldUpperLimit;

                _interface = null;
            }
        }
    }
}
