using FistVR;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenScripts2
{
    public class ClosedBoltQuickbolting : OpenScripts2_BasePlugin
    {
        public ClosedBoltWeapon ClosedBoltWeapon;

        private static readonly List<ClosedBoltWeapon> _quickBoltableClosedBoltWeapons = [];
        private static readonly List<ClosedBolt> _quickBoltedBolt = [];

        public void Awake()
        {
            _quickBoltableClosedBoltWeapons.Add(ClosedBoltWeapon);
        } 

        public void OnDestroy()
        {
            _quickBoltableClosedBoltWeapons.Remove(ClosedBoltWeapon);
        }

#if !DEBUG
        static ClosedBoltQuickbolting()
        {
            On.FistVR.ClosedBoltWeapon.UpdateInteraction += ClosedBoltWeapon_UpdateInteraction;
            On.FistVR.ClosedBolt.BoltEvent_ArriveAtFore += ClosedBolt_BoltEvent_ArriveAtFore;
        }

        private static void ClosedBoltWeapon_UpdateInteraction(On.FistVR.ClosedBoltWeapon.orig_UpdateInteraction orig, ClosedBoltWeapon self, FVRViveHand hand)
        {
            orig(self, hand);
            bool quickBoltingEnabled = GM.Options.QuickbeltOptions.BoltActionModeSetting == QuickbeltOptions.BoltActionMode.Quickbolting;
            if (quickBoltingEnabled && _quickBoltableClosedBoltWeapons.Contains(self) && TouchpadDirDown(hand, Vector2.right))
            {
                hand.Buzz(hand.Buzzer.Buzz_BeginInteraction);
                hand.HandMadeGrabReleaseSound();
                hand.EndInteractionIfHeld(self);
                self.EndInteraction(hand);
                self.Bolt.BeginInteraction(hand);
                hand.ForceSetInteractable(self.Bolt);
                _quickBoltedBolt.Add(self.Bolt);
            }
        }

        private static void ClosedBolt_BoltEvent_ArriveAtFore(On.FistVR.ClosedBolt.orig_BoltEvent_ArriveAtFore orig, ClosedBolt self)
        {
            orig(self);
            if (_quickBoltedBolt.Contains(self))
            {
                FVRViveHand hand = self.m_hand;
                hand.Buzz(hand.Buzzer.Buzz_BeginInteraction);
                self.EndInteraction(hand);
                self.Weapon.BeginInteraction(hand);
                hand.ForceSetInteractable(self.Weapon);
                _quickBoltedBolt.Remove(self);
            }
        }
#endif
    }
}
