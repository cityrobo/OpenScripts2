using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class ClosedBoltSeparateBoltLock : OpenScripts2_BasePlugin
    {
        public ClosedBolt Bolt;
        public Transform SeparateBoltLockPoint;
        public Transform LockButton;
        public TransformType LockButtonTransformType;
        public Axis LockButtonAxis;
        public float LockButtonUnlocked;
        public float LockButtonLocked;

        private float _boltZLock;
        private bool _lockEngaged = false;
        private static readonly Dictionary<ClosedBolt, ClosedBoltSeparateBoltLock> _existingSeparateBoltLocks = new();

        public void Awake()
        {
            _boltZLock = Bolt.transform.parent.InverseTransformPoint(SeparateBoltLockPoint.position).z;
            _existingSeparateBoltLocks.Add(Bolt, this);
        }

        public void OnDestroy()
        {
            _existingSeparateBoltLocks.Remove(Bolt);
        }

        public void Update()
        {
            float transformValue = _lockEngaged ? LockButtonLocked : LockButtonUnlocked;
            LockButton.ModifyLocalTransform(LockButtonTransformType, LockButtonAxis, transformValue);
        }

#if !DEBUG
        static ClosedBoltSeparateBoltLock()
        {
            On.FistVR.ClosedBolt.UpdateInteraction += ClosedBolt_UpdateInteraction;
            On.FistVR.ClosedBolt.UpdateBolt += ClosedBolt_UpdateBolt;
        }

        private static void ClosedBolt_UpdateInteraction(On.FistVR.ClosedBolt.orig_UpdateInteraction orig, ClosedBolt self, FVRViveHand hand)
        {
            orig(self, hand);
            if (_existingSeparateBoltLocks.TryGetValue(self, out ClosedBoltSeparateBoltLock closedBoltSeparateBoltLock))
            {
                if (self.m_boltZ_current <= closedBoltSeparateBoltLock._boltZLock)
                {
                    if (hand.IsInStreamlinedMode)
                    {
                        if (hand.Input.AXButtonDown)
                        {
                            closedBoltSeparateBoltLock._lockEngaged = true;
                            //self.ForceBreakInteraction();
                        }
                        else if (hand.Input.BYButtonDown)
                        {
                            closedBoltSeparateBoltLock._lockEngaged = false;
                            //self.ForceBreakInteraction();
                        }
                    }
                    else if (hand.Input.TouchpadDown)
                    {
                        if (Vector2.Angle(hand.Input.TouchpadAxes, Vector2.down) < 45f)
                        {
                            closedBoltSeparateBoltLock._lockEngaged = true;
                            //self.ForceBreakInteraction();
                        }
                        else if (Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) < 45f)
                        {
                            closedBoltSeparateBoltLock._lockEngaged = false;
                            //self.ForceBreakInteraction();
                        }
                    }
                }
            }
        }

        private static void ClosedBolt_UpdateBolt(On.FistVR.ClosedBolt.orig_UpdateBolt orig, ClosedBolt self)
        {
            orig(self);
            if (_existingSeparateBoltLocks.TryGetValue(self, out ClosedBoltSeparateBoltLock closedBoltSeparateBoltLock))
            {
                if (closedBoltSeparateBoltLock._lockEngaged)
                {
                    self.m_boltZ_current = closedBoltSeparateBoltLock._boltZLock;
                    self.m_curBoltSpeed = 0f;
                    self.transform.ModifyLocalPositionAxisValue(Axis.Z, self.m_boltZ_current);
                }
            }
        }
#endif
    }
}
