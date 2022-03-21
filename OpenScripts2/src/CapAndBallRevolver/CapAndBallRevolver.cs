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

#if!(UNITY_EDITOR||UNITY_5)
        public override void Start()
        {
            base.Start();
            Hook();
            _capCylinder = base.Cylinder as CapAndBallRevolverCylinder;

            NumberOfChambersBackwardsToRam = Mathf.Abs(NumberOfChambersBackwardsToRam);
        }

        public override void OnDestroy()
        {
            Unhook();
            base.OnDestroy();
        }
        private void Unhook()
        {
            On.FistVR.SingleActionRevolver.Fire -= SingleActionRevolver_Fire;
            //On.FistVR.SingleActionRevolver.EjectPrevCylinder -= SingleActionRevolver_EjectPrevCylinder;
            On.FistVR.SingleActionRevolver.UpdateCylinderRot -= SingleActionRevolver_UpdateCylinderRot;
            On.FistVR.SingleActionRevolver.AdvanceCylinder -= SingleActionRevolver_AdvanceCylinder;
        }

        private void Hook()
        {
            On.FistVR.SingleActionRevolver.Fire += SingleActionRevolver_Fire;
            //On.FistVR.SingleActionRevolver.EjectPrevCylinder += SingleActionRevolver_EjectPrevCylinder;
            On.FistVR.SingleActionRevolver.UpdateCylinderRot += SingleActionRevolver_UpdateCylinderRot;
            On.FistVR.SingleActionRevolver.AdvanceCylinder += SingleActionRevolver_AdvanceCylinder;
        }

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
            if (RamRodLever.localEulerAngles.IsLesserOrEqual( LowerRamRodRotationLimit + wiggleRoomVector) && RamRodLever.localEulerAngles.IsGreaterOrEqual(LowerRamRodRotationLimit - wiggleRoomVector))
            {
                _isRamRodExtended = false;
            }
            else _isRamRodExtended = true;

            float lerp = ExtendingVector3.InverseLerp(TouchingRamRodRotation,UpperRamRodRotationLimit, RamRodLever.localEulerAngles);
            if (_capCylinder.Chambers[RammingChamber].IsFull && lerp > 0f)
            {
                _capCylinder.RamChamber(RammingChamber, lerp);
            }
        }

        private void SingleActionRevolver_AdvanceCylinder(On.FistVR.SingleActionRevolver.orig_AdvanceCylinder orig, SingleActionRevolver self)
        {
            if (self == this)
            {
                if (_isRamRodExtended || (!_capCylinder.ChamberRammed(RammingChamber) && _capCylinder.Chambers[RammingChamber].IsFull))
                {
                    return;
                }
                
                if (_lastChamber == this.CurChamber)
                {
                    _lastChamber--;
                }
                else
                {
                    this.CurChamber++;
                    _lastChamber = this.CurChamber;
                }

                this.PlayAudioEvent(FirearmAudioEventType.FireSelector, 1f);
            }
            else orig(self);
        }

        private void SingleActionRevolver_UpdateCylinderRot(On.FistVR.SingleActionRevolver.orig_UpdateCylinderRot orig, SingleActionRevolver self)
        {
            if (self == this)
            {
                if (this.m_isStateToggled)
                {
                    int num = this.PrevChamber;
                    if (this.IsAccessTwoChambersBack)
                        num = this.PrevChamber2;
                    for (int index = 0; index < this._capCylinder.Chambers.Length; ++index)
                    {
                        this._capCylinder.Chambers[index].IsAccessible = index == num;
                        this._capCylinder.capNipples[index].IsAccessible = index == num;

                        if (_lastChamber == this.CurChamber)
                        {
                            if (!this.IsAccessTwoChambersBack)
                            {
                                this._capCylinder.Chambers[index].IsAccessible = index == this.PrevChamber2;
                                this._capCylinder.capNipples[index].IsAccessible = index == this.PrevChamber2;
                            }
                            else
                            {
                                this._capCylinder.Chambers[index].IsAccessible = index == this.PrevChamber3;
                                this._capCylinder.capNipples[index].IsAccessible = index == this.PrevChamber3;
                            }
                        }
                    }
                    if (this.DoesHalfCockHalfRotCylinder)
                    {
                        int cylinder = (this.CurChamber + 1) % this._capCylinder.NumChambers;
                        this._capCylinder.transform.localRotation = Quaternion.Slerp(this._capCylinder.GetLocalRotationFromCylinder(this.CurChamber), this._capCylinder.GetLocalRotationFromCylinder(cylinder), 0.5f);

                        if (_lastChamber == this.CurChamber) this._capCylinder.transform.localRotation = Quaternion.Slerp(this._capCylinder.GetLocalRotationFromCylinder(this.CurChamber), this._capCylinder.GetLocalRotationFromCylinder(cylinder), 0f);
                    }
                    else
                    {
                        int cylinder = (this.CurChamber + 1) % this._capCylinder.NumChambers;
                        this._capCylinder.transform.localRotation = this._capCylinder.GetLocalRotationFromCylinder(this.CurChamber);
                        if (_lastChamber == this.CurChamber) this._capCylinder.transform.localRotation = Quaternion.Slerp(this._capCylinder.GetLocalRotationFromCylinder(this.CurChamber), this._capCylinder.GetLocalRotationFromCylinder(cylinder), 0.5f);
                    }
                    if (this.DoesCylinderTranslateForward)
                        this._capCylinder.transform.localPosition = this.CylinderBackPos;
                    
                }
                else
                {
                    for (int index = 0; index < this._capCylinder.Chambers.Length; ++index)
                    {
                        this._capCylinder.Chambers[index].IsAccessible = false;
                        this._capCylinder.capNipples[index].IsAccessible = false;
                    }
                    this.m_tarChamberLerp = !this.m_isHammerCocking ? 0.0f : this.m_hammerCockLerp;
                    this.m_curChamberLerp = Mathf.Lerp(this.m_curChamberLerp, this.m_tarChamberLerp, Time.deltaTime * 16f);
                    int cylinder = (this.CurChamber + 1) % this._capCylinder.NumChambers;
                    this._capCylinder.transform.localRotation = Quaternion.Slerp(this._capCylinder.GetLocalRotationFromCylinder(this.CurChamber), this._capCylinder.GetLocalRotationFromCylinder(cylinder), this.m_curChamberLerp);

                    if (this.DoesCylinderTranslateForward)
                        this._capCylinder.transform.localPosition = Vector3.Lerp(this.CylinderBackPos, this.CylinderFrontPos, this.m_hammerCockLerp);


                    return;
                }
            }
            else orig(self);
        }

        private void SingleActionRevolver_EjectPrevCylinder(On.FistVR.SingleActionRevolver.orig_EjectPrevCylinder orig, SingleActionRevolver self)
        {
            if (self == this)
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

        private void SingleActionRevolver_Fire(On.FistVR.SingleActionRevolver.orig_Fire orig, SingleActionRevolver self)
        {
            if (self == this)
            {
                //Debug.Log("new fire");
                this.PlayAudioEvent(FirearmAudioEventType.HammerHit);

                bool capFired = this._capCylinder.capNipples[this.CurChamber].Fire();

                if (capFired)
                {
                    this.PlayAudioEvent(FirearmAudioEventType.Shots_LowPressure);
                }

                if (!capFired || !this._capCylinder.ChamberRammed(this.CurChamber) || !this._capCylinder.Chambers[this.CurChamber].Fire())
                    return;

                FVRFireArmChamber chamber = this._capCylinder.Chambers[this.CurChamber];
                this.Fire(chamber, this.GetMuzzle(), true);
                this.FireMuzzleSmoke();
                this.Recoil(this.IsTwoHandStabilized(), (Object)this.AltGrip != (Object)null, this.IsShoulderStabilized());
                this.PlayAudioGunShot(chamber.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment());

                if (GM.CurrentSceneSettings.IsAmmoInfinite && GM.CurrentPlayerBody.IsInfiniteAmmo)
                {
                    chamber.IsSpent = false;
                    this._capCylinder.capNipples[this.CurChamber].IsSpent = false;

                    chamber.UpdateProxyDisplay();
                }
                else
                {
                    chamber.SetRound(null);

                    this._capCylinder.ChamberRammed(this.CurChamber, true, false);
                }
            }
            else orig(self);
        }
#endif
    }
#if !(UNITY_EDITOR || UNITY_5)
    public static class ExtendingVector3
    {
        public static bool IsGreaterOrEqual(this Vector3 local, Vector3 other)
        {
            if (local.x >= other.x && local.y >= other.y && local.z >= other.z)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsLesserOrEqual(this Vector3 local, Vector3 other)
        {
            if (local.x <= other.x && local.y <= other.y && local.z <= other.z)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
        {
            /*
            float lerpx = Mathf.InverseLerp(a.x, b.x, value.x);
            float lerpy = Mathf.InverseLerp(a.y, b.y, value.y);
            float lerpz = Mathf.InverseLerp(a.z, b.z, value.z);

            Vector3 lerp = new Vector3(lerpx, lerpy, lerpz);
            return lerp.magnitude;
            */

            Vector3 AB = b - a;
            Vector3 AV = value - a;
            return Mathf.Clamp01(Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB));
        }
    }
#endif
}
