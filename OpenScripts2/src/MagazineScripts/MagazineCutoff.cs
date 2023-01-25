using FistVR;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class MagazineCutoff : FVRInteractiveObject
    {
        [Header("Magazine Cutoff Config")]
        public FVRFireArm FireArm;
        public Transform CutoffLever;

        public float StartLimit;
        public float StopLimit;
        public float Speed;

        public OpenScripts2_BasePlugin.TransformType MovementType;
        public OpenScripts2_BasePlugin.Axis Axis;

        public bool MakesMagazineUnaccessibleOnActive = false;
        public bool MakesChamberAccessibleOnActive = false;

        [Header("Sound")]
        public AudioEvent Sounds;

        private Vector3 _startPos;
        private Vector3 _stopPos;

        private Quaternion _startRot;
        private Quaternion _stopRot;

        private bool _MagazineCuttoffActive = false;

        private FVRFireArmMagazine _mag;

        private Quaternion _targetRotation;
        private Vector3 _targetPosition;

        private bool _wasClipFed = false;

        private static List<FVRFireArm> s_firearmsWithMagazineCutoff = new();

#if !DEBUG
        static MagazineCutoff()
        {
            On.FistVR.FVRFireArmRound.GetNumRoundsPulled += FVRFireArmRound_GetNumRoundsPulled;
            On.FistVR.FVRFireArmRound.DuplicateFromSpawnLock += FVRFireArmRound_DuplicateFromSpawnLock;
        }

        private static GameObject FVRFireArmRound_DuplicateFromSpawnLock(On.FistVR.FVRFireArmRound.orig_DuplicateFromSpawnLock orig, FVRFireArmRound self, FVRViveHand hand)
        {
            GameObject gO = orig(self, hand);
            FVRFireArmRound component = gO.GetComponent<FVRFireArmRound>();
            if (GM.Options.ControlOptions.SmartAmmoPalming == ControlOptions.SmartAmmoPalmingMode.Enabled && component != null && hand.OtherHand.CurrentInteractable != null)
            {
                int num = 0;
                if (hand.OtherHand.CurrentInteractable is FVRFireArm fireArm && s_firearmsWithMagazineCutoff.Contains(fireArm))
                {
                    if (fireArm.RoundType == self.RoundType)
                    {
                        FVRFireArmMagazine magazine = fireArm.Magazine;
                        if (magazine != null && magazine.IsDropInLoadable)
                        {
                            num = magazine.m_capacity - magazine.m_numRounds;
                        }
                        for (int i = 0; i < fireArm.GetChambers().Count; i++)
                        {
                            FVRFireArmChamber fvrfireArmChamber = fireArm.GetChambers()[i];
                            if (fvrfireArmChamber.IsManuallyChamberable && (!fvrfireArmChamber.IsFull || fvrfireArmChamber.IsSpent))
                            {
                                num++;
                            }
                        }
                    }
                    component.DestroyAllProxies();
                    if (num < 1)
                    {
                        num = self.ProxyRounds.Count;
                    }
                    int num2 = Mathf.Min(self.ProxyRounds.Count, num - 1);
                    for (int k = 0; k < num2; k++)
                    {
                        component.AddProxy(self.ProxyRounds[k].Class, self.ProxyRounds[k].ObjectWrapper);
                    }
                }
                component.UpdateProxyDisplay();
            }

            return gO;
        }

        private static int FVRFireArmRound_GetNumRoundsPulled(On.FistVR.FVRFireArmRound.orig_GetNumRoundsPulled orig, FVRFireArmRound self, FVRViveHand hand)
        {
            int num = 0;
            if (hand.OtherHand.CurrentInteractable is FVRFireArm fireArm && s_firearmsWithMagazineCutoff.Contains(fireArm))
            {
                if (fireArm.RoundType == self.RoundType)
                {
                    FVRFireArmMagazine magazine = fireArm.Magazine;
                    if (magazine != null && magazine.IsDropInLoadable)
                    {
                        num = magazine.m_capacity - magazine.m_numRounds;
                    }
                    for (int i = 0; i < fireArm.GetChambers().Count; i++)
                    {
                        FVRFireArmChamber fvrfireArmChamber = fireArm.GetChambers()[i];
                        if (fvrfireArmChamber.IsManuallyChamberable && (!fvrfireArmChamber.IsFull || fvrfireArmChamber.IsSpent))
                        {
                            num++;
                        }
                    }
                }
                return num;
            } 
            else return orig(self, hand);
        }
#endif
        public override void Awake()
        {
            base.Awake();

            s_firearmsWithMagazineCutoff.Add(FireArm);

            IsSimpleInteract = true;
            CalculatePositions();

            _targetPosition = _startPos;
            _targetRotation = _startRot;

            _mag = FireArm.Magazine;

            _wasClipFed = FireArm.UsesClips;
        }

        public override void OnDestroy()
        {
            s_firearmsWithMagazineCutoff.Remove(FireArm);

            base.OnDestroy();
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            base.SimpleInteraction(hand);

            _MagazineCuttoffActive = !_MagazineCuttoffActive;

            SM.PlayGenericSound(Sounds, CutoffLever.position);
            switch (MovementType)
            {
                case OpenScripts2_BasePlugin.TransformType.Movement:
                    _targetPosition = _MagazineCuttoffActive ? _stopPos : _startPos;
                    break;
                case OpenScripts2_BasePlugin.TransformType.Rotation:
                    _targetRotation = _MagazineCuttoffActive ? _stopRot : _startRot;
                    break;
                default:
                    OpenScripts2_BepInExPlugin.LogWarning(this, "Scale not supported!");
                    break;
            }
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (_MagazineCuttoffActive && _mag != null)
            {
                if (_mag.FireArm == FireArm)
                {
                    _mag.IsExtractable = false;
                    if (MakesMagazineUnaccessibleOnActive)
                    {
                        _mag.IsDropInLoadable = false;
                        if (_wasClipFed)
                        {
                            FireArm.UsesClips = false;
                            FireArm.ClipTrigger.SetActive(false);
                        }
                    }

                    if (MakesChamberAccessibleOnActive)
                    {
                        List<FVRFireArmChamber> chambers = FireArm.GetChambers();
                        foreach (FVRFireArmChamber chamber in chambers)
                        {
                            chamber.IsAccessible = true;
                        }
                    }
                }
                else
                {
                    _mag.IsExtractable = true;
                    if (MakesMagazineUnaccessibleOnActive) _mag.IsDropInLoadable = true;
                    _mag = null;
                }
            }
            else if (!_MagazineCuttoffActive && _mag != null)
            {
                _mag.IsExtractable = true;
                if (MakesMagazineUnaccessibleOnActive)
                {
                    _mag.IsDropInLoadable = true;
                    if (_wasClipFed)
                    {
                        FireArm.UsesClips = true;

                        if (FireArm is BoltActionRifle boltActionRifle && boltActionRifle.CurBoltHandleState == BoltActionRifle_Handle.BoltActionHandleState.Rear) FireArm.ClipTrigger.SetActive(true);
                    }
                }

                if (MakesChamberAccessibleOnActive)
                {
                    List<FVRFireArmChamber> chambers = FireArm.GetChambers();
                    foreach (FVRFireArmChamber chamber in chambers)
                    {
                        chamber.IsAccessible = false;
                    }
                }
            }

            _mag = FireArm.Magazine;

            if (MovementType == OpenScripts2_BasePlugin.TransformType.Rotation && CutoffLever.localRotation != _targetRotation)
            {
                CutoffLever.localRotation = Quaternion.RotateTowards(CutoffLever.localRotation, _targetRotation, Speed * Time.deltaTime);
            }
            else if (MovementType == OpenScripts2_BasePlugin.TransformType.Movement && CutoffLever.localPosition != _targetPosition)
            {
                CutoffLever.localPosition = Vector3.MoveTowards(CutoffLever.localPosition, _targetPosition, Speed * Time.deltaTime);
            }
        }

        private void CalculatePositions()
        {
            switch (MovementType)
            {
                case OpenScripts2_BasePlugin.TransformType.Movement:
                    switch (Axis)
                    {
                        case OpenScripts2_BasePlugin.Axis.X:
                            _startPos = new Vector3(StartLimit, CutoffLever.localPosition.y, CutoffLever.localPosition.z);
                            _stopPos = new Vector3(StopLimit, CutoffLever.localPosition.y, CutoffLever.localPosition.z);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Y:
                            _startPos = new Vector3(CutoffLever.localPosition.x, StartLimit, CutoffLever.localPosition.z);
                            _stopPos = new Vector3(CutoffLever.localPosition.x, StopLimit, CutoffLever.localPosition.z);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Z:
                            _startPos = new Vector3(CutoffLever.localPosition.x, CutoffLever.localPosition.y, StartLimit);
                            _stopPos = new Vector3(CutoffLever.localPosition.x, CutoffLever.localPosition.y, StopLimit);
                            break;
                    }
                    CutoffLever.localPosition = _startPos;
                    break;
                case OpenScripts2_BasePlugin.TransformType.Rotation:
                    switch (Axis)
                    {
                        case OpenScripts2_BasePlugin.Axis.X:
                            _startRot = Quaternion.Euler(StartLimit, CutoffLever.localEulerAngles.y, CutoffLever.localEulerAngles.z);
                            _stopRot = Quaternion.Euler(StopLimit, CutoffLever.localEulerAngles.y, CutoffLever.localEulerAngles.z);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Y:
                            _startRot = Quaternion.Euler(CutoffLever.localEulerAngles.x, StartLimit, CutoffLever.localEulerAngles.z);
                            _stopRot = Quaternion.Euler(CutoffLever.localEulerAngles.x, StopLimit, CutoffLever.localEulerAngles.z);
                            break;
                        case OpenScripts2_BasePlugin.Axis.Z:
                            _startRot = Quaternion.Euler(CutoffLever.localEulerAngles.x, CutoffLever.localEulerAngles.y, StartLimit);
                            _stopRot = Quaternion.Euler(CutoffLever.localEulerAngles.x, CutoffLever.localEulerAngles.y, StopLimit);
                            break;
                    }
                    CutoffLever.localRotation = _startRot;
                    break;
                default:
                    OpenScripts2_BepInExPlugin.LogWarning(this, "Scale not supported!");
                    break;
            }
        }
    }
}
