using FistVR;
//using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class CapAndBallRevolver : SingleActionRevolver
    {
        [Header("Cap and Ball Revolver Config")]
        public Transform RamRodLever;
        public Vector3 LowerRamRodRotationLimit;
        public Vector3 UpperRamRodRotationLimit;

        public Vector3 TouchingRamRodRotation;
        public float RamRodWiggleRoom = 5f;
        public int NumberOfChambersBackwardsToRam;

        private CapAndBallRevolverCylinder _capCylinder;

        private int _lastChamber = -1;

        private bool _isRamRodExtended = false;

        static CapAndBallRevolver()
        {
            Hook();
        }


        public override void Start()
        {
            base.Start();
            _capCylinder = base.Cylinder as CapAndBallRevolverCylinder;
            NumberOfChambersBackwardsToRam = Mathf.Abs(NumberOfChambersBackwardsToRam);

            //Hook();
        }

        public override void OnDestroy()
        {
            //Unhook();
            base.OnDestroy();
        }

        // Returns the Chamber that would be rammed
        public int RammingChamber
        {
            get
            {
                int num = this.CurChamber - NumberOfChambersBackwardsToRam;
                if (num < 0)
                {
                    return this.Cylinder.NumChambers + num;
                }
                return num;
            }
        }

        // Returns the third previous Chamber
        public int PrevChamber3
        {
            get
            {
                int num = this.CurChamber - 3;
                if (num < 0)
                {
                    return this.Cylinder.NumChambers + num;
                }
                return num;
            }
        }
        public override void FVRUpdate()
        {
            base.FVRUpdate();

            Vector3 wiggleRoomVector = new Vector3(RamRodWiggleRoom, RamRodWiggleRoom, RamRodWiggleRoom);
            if (RamRodLever.localEulerAngles.IsLessThanOrEqual(LowerRamRodRotationLimit + wiggleRoomVector) && RamRodLever.localEulerAngles.IsGreaterThanOrEqual(LowerRamRodRotationLimit - wiggleRoomVector))
            {
                _isRamRodExtended = false;
            }
            else _isRamRodExtended = true;

            float lerp = Vector3Utils.InverseLerp(TouchingRamRodRotation, UpperRamRodRotationLimit, RamRodLever.localEulerAngles);
            if (_capCylinder.Chambers[RammingChamber].IsFull && lerp > 0f)
            {
                _capCylinder.RamChamber(RammingChamber, lerp);
            }
        }

        private void Unhook()
        {
#if !DEBUG
            On.FistVR.SingleActionRevolver.Fire -= SingleActionRevolver_Fire;
            //On.FistVR.SingleActionRevolver.EjectPrevCylinder -= SingleActionRevolver_EjectPrevCylinder;
            On.FistVR.SingleActionRevolver.UpdateCylinderRot -= SingleActionRevolver_UpdateCylinderRot;
            On.FistVR.SingleActionRevolver.AdvanceCylinder -= SingleActionRevolver_AdvanceCylinder;
#endif
        }

        static private void Hook()
        {
#if !DEBUG
            On.FistVR.SingleActionRevolver.Fire += SingleActionRevolver_Fire;
            //On.FistVR.SingleActionRevolver.EjectPrevCylinder += SingleActionRevolver_EjectPrevCylinder;
            On.FistVR.SingleActionRevolver.UpdateCylinderRot += SingleActionRevolver_UpdateCylinderRot;
            On.FistVR.SingleActionRevolver.AdvanceCylinder += SingleActionRevolver_AdvanceCylinder;
#endif
        }
#if !DEBUG
        static private void SingleActionRevolver_AdvanceCylinder(On.FistVR.SingleActionRevolver.orig_AdvanceCylinder orig, SingleActionRevolver self)
        {
            if (self is CapAndBallRevolver capAndBallRevolver)
            {
                if (capAndBallRevolver._isRamRodExtended || (!capAndBallRevolver._capCylinder.GetChamberRammed(capAndBallRevolver.RammingChamber) && capAndBallRevolver._capCylinder.Chambers[capAndBallRevolver.RammingChamber].IsFull))
                {
                    return;
                }
                
                if (capAndBallRevolver._lastChamber == capAndBallRevolver.CurChamber)
                {
                    capAndBallRevolver._lastChamber--;
                }
                else
                {
                    capAndBallRevolver.CurChamber++;
                    capAndBallRevolver._lastChamber = capAndBallRevolver.CurChamber;
                }

                capAndBallRevolver.PlayAudioEvent(FirearmAudioEventType.FireSelector, 1f);
            }
            else orig(self);
        }

        static private void SingleActionRevolver_UpdateCylinderRot(On.FistVR.SingleActionRevolver.orig_UpdateCylinderRot orig, SingleActionRevolver self)
        {
            if (self is CapAndBallRevolver capAndBallRevolver)
            {
                if (capAndBallRevolver.m_isStateToggled)
                {
                    int num = capAndBallRevolver.PrevChamber;
                    if (capAndBallRevolver.IsAccessTwoChambersBack)
                        num = capAndBallRevolver.PrevChamber2;
                    for (int index = 0; index < capAndBallRevolver._capCylinder.Chambers.Length; ++index)
                    {
                        capAndBallRevolver._capCylinder.Chambers[index].IsAccessible = index == num;
                        capAndBallRevolver._capCylinder.CapNipples[index].IsAccessible = index == num;

                        if (capAndBallRevolver._lastChamber == capAndBallRevolver.CurChamber)
                        {
                            if (!capAndBallRevolver.IsAccessTwoChambersBack)
                            {
                                capAndBallRevolver._capCylinder.Chambers[index].IsAccessible = index == capAndBallRevolver.PrevChamber2;
                                capAndBallRevolver._capCylinder.CapNipples[index].IsAccessible = index == capAndBallRevolver.PrevChamber2;
                            }
                            else
                            {
                                capAndBallRevolver._capCylinder.Chambers[index].IsAccessible = index == capAndBallRevolver.PrevChamber3;
                                capAndBallRevolver._capCylinder.CapNipples[index].IsAccessible = index == capAndBallRevolver.PrevChamber3;
                            }
                        }
                    }
                    if (capAndBallRevolver.DoesHalfCockHalfRotCylinder)
                    {
                        int cylinder = (capAndBallRevolver.CurChamber + 1) % capAndBallRevolver._capCylinder.NumChambers;
                        capAndBallRevolver._capCylinder.transform.localRotation = Quaternion.Slerp(capAndBallRevolver._capCylinder.GetLocalRotationFromCylinder(capAndBallRevolver.CurChamber), capAndBallRevolver._capCylinder.GetLocalRotationFromCylinder(cylinder), 0.5f);

                        if (capAndBallRevolver._lastChamber == capAndBallRevolver.CurChamber) capAndBallRevolver._capCylinder.transform.localRotation = Quaternion.Slerp(capAndBallRevolver._capCylinder.GetLocalRotationFromCylinder(capAndBallRevolver.CurChamber), capAndBallRevolver._capCylinder.GetLocalRotationFromCylinder(cylinder), 0f);
                    }
                    else
                    {
                        int cylinder = (capAndBallRevolver.CurChamber + 1) % capAndBallRevolver._capCylinder.NumChambers;
                        capAndBallRevolver._capCylinder.transform.localRotation = capAndBallRevolver._capCylinder.GetLocalRotationFromCylinder(capAndBallRevolver.CurChamber);
                        if (capAndBallRevolver._lastChamber == capAndBallRevolver.CurChamber) capAndBallRevolver._capCylinder.transform.localRotation = Quaternion.Slerp(capAndBallRevolver._capCylinder.GetLocalRotationFromCylinder(capAndBallRevolver.CurChamber), capAndBallRevolver._capCylinder.GetLocalRotationFromCylinder(cylinder), 0.5f);
                    }
                    if (capAndBallRevolver.DoesCylinderTranslateForward)
                        capAndBallRevolver._capCylinder.transform.localPosition = capAndBallRevolver.CylinderBackPos;
                    
                }
                else
                {
                    for (int index = 0; index < capAndBallRevolver._capCylinder.Chambers.Length; ++index)
                    {
                        capAndBallRevolver._capCylinder.Chambers[index].IsAccessible = false;
                        capAndBallRevolver._capCylinder.CapNipples[index].IsAccessible = false;
                    }
                    capAndBallRevolver.m_tarChamberLerp = !capAndBallRevolver.m_isHammerCocking ? 0.0f : capAndBallRevolver.m_hammerCockLerp;
                    capAndBallRevolver.m_curChamberLerp = Mathf.Lerp(capAndBallRevolver.m_curChamberLerp, capAndBallRevolver.m_tarChamberLerp, Time.deltaTime * 16f);
                    int cylinder = (capAndBallRevolver.CurChamber + 1) % capAndBallRevolver._capCylinder.NumChambers;
                    capAndBallRevolver._capCylinder.transform.localRotation = Quaternion.Slerp(capAndBallRevolver._capCylinder.GetLocalRotationFromCylinder(capAndBallRevolver.CurChamber), capAndBallRevolver._capCylinder.GetLocalRotationFromCylinder(cylinder), capAndBallRevolver.m_curChamberLerp);

                    if (capAndBallRevolver.DoesCylinderTranslateForward)
                        capAndBallRevolver._capCylinder.transform.localPosition = Vector3.Lerp(capAndBallRevolver.CylinderBackPos, capAndBallRevolver.CylinderFrontPos, capAndBallRevolver.m_hammerCockLerp);


                    return;
                }
            }
            else orig(self);
        }

        static private void SingleActionRevolver_EjectPrevCylinder(On.FistVR.SingleActionRevolver.orig_EjectPrevCylinder orig, SingleActionRevolver self)
        {
            if (self is CapAndBallRevolver capAndBallRevolver)
            {
                /*if (!this.m_isStateToggled)
                    return;
                int index = this.PrevChamber;
                if (this.IsAccessTwoChambersBack)
                    index = this.PrevChamber2;
                FVRFireArmChamber chamber = this.CapCylinder.Chambers[index];
                if (chamber.IsFull)
                    this.PlayAudioEvent(FirearmAudioEventType.MagazineEjectRound);
                chamber.EjectRound(chamber.transform.position + chamber.transform.forward * (1f / 400f), chamber.transform.forward, Vector3.zero);*/
            }
            else
            {
                orig(self);
            }
        }

        static private void SingleActionRevolver_Fire(On.FistVR.SingleActionRevolver.orig_Fire orig, SingleActionRevolver self)
        {
            if (self is CapAndBallRevolver capAndBallRevolver)
            {
                //Debug.Log("new fire");
                capAndBallRevolver.PlayAudioEvent(FirearmAudioEventType.HammerHit);

                bool capFired = capAndBallRevolver._capCylinder.CapNipples[capAndBallRevolver.CurChamber].Fire();

                if (capFired)
                {
                    capAndBallRevolver.PlayAudioEvent(FirearmAudioEventType.Shots_LowPressure);
                }

                if (!capFired || !capAndBallRevolver._capCylinder.GetChamberRammed(capAndBallRevolver.CurChamber) || !capAndBallRevolver._capCylinder.Chambers[capAndBallRevolver.CurChamber].Fire())
                    return;

                FVRFireArmChamber chamber = capAndBallRevolver._capCylinder.Chambers[capAndBallRevolver.CurChamber];
                capAndBallRevolver.Fire(chamber, capAndBallRevolver.GetMuzzle(), true);
                capAndBallRevolver.FireMuzzleSmoke();
                capAndBallRevolver.Recoil(capAndBallRevolver.IsTwoHandStabilized(), capAndBallRevolver.AltGrip != null, capAndBallRevolver.IsShoulderStabilized());
                capAndBallRevolver.PlayAudioGunShot(chamber.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment());

                if (GM.CurrentSceneSettings.IsAmmoInfinite && GM.CurrentPlayerBody.IsInfiniteAmmo)
                {
                    chamber.IsSpent = false;
                    capAndBallRevolver._capCylinder.CapNipples[capAndBallRevolver.CurChamber].IsSpent = false;

                    chamber.UpdateProxyDisplay();
                }
                else
                {
                    chamber.SetRound(null);

                    capAndBallRevolver._capCylinder.RamChamber(capAndBallRevolver.CurChamber, true);
                }
            }
            else orig(self);
        }
#endif
    }
}
