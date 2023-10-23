using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class MultiCaliberChamber : OpenScripts2_BasePlugin
    {
        public FVRFireArmChamber Chamber;

        [Serializable]
        public class AdditionalChambering
        {
            public FireArmRoundType AdditionalRoundType;
            [Tooltip("Will get deleted for performance reasons, do not put anything below it!")]
            public Transform RoundMountPoint;
            [HideInInspector]
            public TransformProxy RoundMountPointProxy;
            public float ChamberVelocityMultiplierOverride = 1f;
        }

        public AdditionalChambering[] AdditionalChamberings;

        private static readonly Dictionary<FVRFireArmChamber, MultiCaliberChamber> _existingMultiCaliberChamber = new();

        private FireArmRoundType _origRoundType;
        private Vector3 _origChamberPos;
        private Vector3 _origTriggerCenter;
        private float _origChamberVelMultiplier;

        public void Start()
        {
            _origChamberPos = transform.localPosition;
            _origRoundType = Chamber.RoundType;
            _origChamberVelMultiplier = Chamber.ChamberVelocityMultiplier;
            switch (Chamber.GetComponent<Collider>())
            {
                case SphereCollider s:
                    _origTriggerCenter = s.center; 
                    break;
                case CapsuleCollider c:
                    _origTriggerCenter = c.center;
                    break;
                case BoxCollider b:
                    _origTriggerCenter = b.center;
                    break;
            }

            foreach (var additionalChambering in AdditionalChamberings)
            {
                additionalChambering.RoundMountPoint.SetParent(Chamber.transform.parent);
                additionalChambering.RoundMountPointProxy = new(additionalChambering.RoundMountPoint, true);
            }
        }

#if !DEBUG
        static MultiCaliberChamber()
        {
            On.FistVR.FVRFireArmRound.OnTriggerEnter += FVRFireArmRound_OnTriggerEnter;
            On.FistVR.FVRFireArmChamber.SetRound_FVRFireArmRound_bool += FVRFireArmChamber_SetRound_FVRFireArmRound_bool;
            On.FistVR.FVRFireArmChamber.SetRound_FVRFireArmRound_Vector3_Quaternion += FVRFireArmChamber_SetRound_FVRFireArmRound_Vector3_Quaternion;
            On.FistVR.FVRFireArmRound.GetNumRoundsPulled += FVRFireArmRound_GetNumRoundsPulled;
            On.FistVR.FVRFireArmRound.DuplicateFromSpawnLock += FVRFireArmRound_DuplicateFromSpawnLock;
        }

        private static void FVRFireArmChamber_SetRound_FVRFireArmRound_Vector3_Quaternion(On.FistVR.FVRFireArmChamber.orig_SetRound_FVRFireArmRound_Vector3_Quaternion orig, FVRFireArmChamber self, FVRFireArmRound round, Vector3 p, Quaternion r)
        {
            if (round != null && _existingMultiCaliberChamber.TryGetValue(self, out MultiCaliberChamber multiCaliberChamber))
            {
                multiCaliberChamber.SetRoundType(round.RoundType);
            }

            orig(self, round, p, r);
        }

        private static void FVRFireArmChamber_SetRound_FVRFireArmRound_bool(On.FistVR.FVRFireArmChamber.orig_SetRound_FVRFireArmRound_bool orig, FVRFireArmChamber self, FVRFireArmRound round, bool animate)
        {
            if (round != null && _existingMultiCaliberChamber.TryGetValue(self, out MultiCaliberChamber multiCaliberChamber))
            {
                multiCaliberChamber.SetRoundType(round.RoundType);
            }

            orig(self, round, animate);
        }

        private static int FVRFireArmRound_GetNumRoundsPulled(On.FistVR.FVRFireArmRound.orig_GetNumRoundsPulled orig, FVRFireArmRound self, FVRViveHand hand)
        {
            int num = 0;
            if (hand.OtherHand.CurrentInteractable is FVRFireArm fvrfireArm)
            {
                FVRFireArmMagazine magazine = fvrfireArm.Magazine;
                if (magazine != null && magazine.IsDropInLoadable && magazine.RoundType == self.RoundType)
                {
                    num = magazine.m_capacity - magazine.m_numRounds;
                }

                foreach(var chamber in fvrfireArm.GetChambers())
                {
                    if (chamber.IsManuallyChamberable && (!chamber.IsFull || chamber.IsSpent) && _existingMultiCaliberChamber.TryGetValue(chamber, out MultiCaliberChamber multiCaliberChamber) && multiCaliberChamber.IsValidRound(self.RoundType))
                    {
                        num++;
                    }
                }
            }
            if (num != 0) return num;
            else return orig(self, hand);
        }

        private static GameObject FVRFireArmRound_DuplicateFromSpawnLock(On.FistVR.FVRFireArmRound.orig_DuplicateFromSpawnLock orig, FVRFireArmRound self, FVRViveHand hand)
        {
            GameObject returnGO = orig(self, hand);

            FVRFireArmRound firearmRoundComponent = returnGO.GetComponent<FVRFireArmRound>();

            if (GM.Options.ControlOptions.SmartAmmoPalming == ControlOptions.SmartAmmoPalmingMode.Enabled && firearmRoundComponent != null && hand.OtherHand.CurrentInteractable != null)
            {
                int num = 0;
                if (hand.OtherHand.CurrentInteractable is FVRFireArm fireArm && fireArm.GetComponentsInChildren<MultiCaliberChamber>().Length > 0)
                {
                    if (hand.OtherHand.CurrentInteractable is FVRFireArm fvrfireArm)
                    {
                        FVRFireArmMagazine magazine = fvrfireArm.Magazine;
                        if (magazine != null && magazine.IsDropInLoadable && magazine.RoundType == self.RoundType)
                        {
                            num = magazine.m_capacity - magazine.m_numRounds;
                        }

                        foreach (var chamber in fvrfireArm.GetChambers())
                        {
                            if (chamber.IsManuallyChamberable && (!chamber.IsFull || chamber.IsSpent) && _existingMultiCaliberChamber.TryGetValue(chamber, out MultiCaliberChamber multiCaliberChamber) && multiCaliberChamber.IsValidRound(self.RoundType))
                            {
                                num++;
                            }
                        }
                    }
                    if (num < 1)
                    {
                        num = self.ProxyRounds.Count;
                    }

                    firearmRoundComponent.DestroyAllProxies();
                    int num2 = Mathf.Min(self.ProxyRounds.Count, num - 1);
                    for (int k = 0; k < num2; k++)
                    {
                        firearmRoundComponent.AddProxy(self.ProxyRounds[k].Class, self.ProxyRounds[k].ObjectWrapper);
                    }
                    firearmRoundComponent.UpdateProxyDisplay();
                }
            }

            return returnGO;
        }

        private static void FVRFireArmRound_OnTriggerEnter(On.FistVR.FVRFireArmRound.orig_OnTriggerEnter orig, FVRFireArmRound self, Collider collider)
        {
            if (self.IsSpent)
            {
                return;
            }
            if (self.isManuallyChamberable && !self.IsSpent && collider.gameObject.CompareTag("FVRFireArmChamber") && self.HoveredOverChamber == null && self.m_hoverOverReloadTrigger == null)
            {
                FVRFireArmChamber chamber = collider.gameObject.GetComponent<FVRFireArmChamber>();
            
                if (_existingMultiCaliberChamber.TryGetValue(chamber, out MultiCaliberChamber multiCaliberChamber) && chamber.IsManuallyChamberable && chamber.IsAccessible && !chamber.IsFull)
                {
                    if (multiCaliberChamber.SetRoundType(self.RoundType)) self.HoveredOverChamber = chamber;
                }
            }

            orig(self, collider);
        }

        public void Awake()
        {
            _existingMultiCaliberChamber.Add(Chamber, this);
        }

        public void OnDestroy()
        {
            _existingMultiCaliberChamber.Remove(Chamber);
        }

        public bool SetRoundType(FireArmRoundType roundType)
        {
            if (AdditionalChamberings.Select(a => a.AdditionalRoundType).Contains(roundType))
            {
                Chamber.RoundType = roundType;
                if (Chamber.Firearm != null && Chamber.Firearm.ObjectWrapper != null)
                {
                    Chamber.Firearm.ObjectWrapper.RoundType = roundType;
                }

                AdditionalChambering additionalChambering = AdditionalChamberings.First(a => a.AdditionalRoundType == roundType);

                Vector3 deltaLocalPos = _origChamberPos - additionalChambering.RoundMountPointProxy.localPosition;
                Chamber.transform.localPosition = additionalChambering.RoundMountPointProxy.localPosition;
                Chamber.ChamberVelocityMultiplier = additionalChambering.ChamberVelocityMultiplierOverride;

                Collider col = Chamber.GetComponent<Collider>();
                switch (col)
                {
                    case SphereCollider s:
                        s.center = _origTriggerCenter + deltaLocalPos;
                        break;
                    case CapsuleCollider c:
                        c.center = _origTriggerCenter + deltaLocalPos;
                        break;
                    case BoxCollider b:
                        b.center = _origTriggerCenter + deltaLocalPos;
                        break;
                }

                return true;
            }
            else if (roundType == _origRoundType)
            {
                Chamber.RoundType = roundType;
                if (Chamber.Firearm != null && Chamber.Firearm.ObjectWrapper != null)
                {
                    Chamber.Firearm.ObjectWrapper.RoundType = roundType;
                }

                Chamber.transform.localPosition = _origChamberPos;
                Chamber.ChamberVelocityMultiplier = _origChamberVelMultiplier;

                switch (Chamber.GetComponent<Collider>())
                {
                    case SphereCollider s:
                        s.center = _origTriggerCenter;
                        break;
                    case CapsuleCollider c:
                        c.center = _origTriggerCenter;
                        break;
                    case BoxCollider b:
                        b.center = _origTriggerCenter;
                        break;
                }

                return true;
            }
            else return false;
        }

        public bool IsValidRound(FireArmRoundType roundType)
        {
            if (_origRoundType == roundType || AdditionalChamberings.Any(a => a.AdditionalRoundType == roundType)) return true;
            else return false;
        }
#endif
	}
}
