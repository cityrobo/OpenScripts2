using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Cityrobo
{
    public class ItemLauncherQBSlot : FVRQuickBeltSlot
    {
		[Header("ItemLauncherQBSlot Config")]
		[Tooltip("Should the Launcher duplicate spawn-locked item?")]
		public bool AllowSpawnLock = true;
		[Tooltip("Should the Launcher duplicate harnessed item?")]
		public bool AllowHarnessing = true;
		[Tooltip("Should the Launcher automatically fire a cartridge placed in the slot or just launch it?")]
		public bool FireAmmunition = true;
		[Tooltip("Should the Launcher automatically pull the pin or cap of a grenade?")]
		public bool AutoArmGrenades = true;
		[Tooltip("Should the Launcher automatically align the object in the slot so it points forward?")]
		public bool AutoAlignZAxis = true;

		/*
        public void Start()
        {
			if (GM.CurrentPlayerBody != null)
			{
				this.RegisterQuickbeltSlot();
			}
        }

		public void OnDestroy()
        {
			if (GM.CurrentPlayerBody != null)
			{
				this.DeRegisterQuickbeltSlot();
			}
		}
		

		public void RegisterQuickbeltSlot()
		{
			if (!GM.CurrentPlayerBody.QuickbeltSlots.Contains(this))
			{
				GM.CurrentPlayerBody.QuickbeltSlots.Add(this);
			}
		}

		public void DeRegisterQuickbeltSlot()
		{
			if (GM.CurrentPlayerBody.QuickbeltSlots.Contains(this))
			{
				GM.CurrentPlayerBody.QuickbeltSlots.Remove(this);
			}
		}
		*/

		public  void LateUpdate()
        {
			if (!AllowHarnessing && CurObject != null && CurObject.m_isHardnessed)
            {
				CurObject.m_isHardnessed = false;
			}
			if (!AllowSpawnLock && CurObject != null && CurObject.m_isSpawnLock)
			{
				CurObject.m_isSpawnLock = false;
			}

			if (AutoAlignZAxis) AlignHeldObject();
		}

		public bool LaunchHeldObject(float speed, Vector3 point)
        {
			if (CurObject == null) return false;

			FVRPhysicalObject physicalObject;

			if (CurObject.m_isSpawnLock || CurObject.m_isHardnessed)
            {
				physicalObject = DuplicateFromSpawnLock(CurObject).GetComponent<FVRPhysicalObject>();
			}
			else
            {
				physicalObject = CurObject;
				CurObject.SetQuickBeltSlot(null);
			}

			physicalObject.RootRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			physicalObject.transform.SetParent(null);
			physicalObject.transform.position = point;
			physicalObject.transform.rotation = this.transform.rotation;
			physicalObject.RootRigidbody.velocity = this.transform.forward * speed;

			switch (physicalObject)
            {
				case PinnedGrenade g:
					if (AutoArmGrenades) PrimeGrenade(g);
					break;
				case FVRCappedGrenade g:
					if (AutoArmGrenades) PrimeGrenade(g);
					break;
				case FVRFireArmRound g:
					if (FireAmmunition) FireRound(g);
					break;
				default:
                    break;
            }

            return true;
        }

		private void PrimeGrenade(PinnedGrenade grenade)
        {
			grenade.ReleaseLever();
        }

		private void PrimeGrenade(FVRCappedGrenade grenade)
        {
			grenade.m_IsFuseActive = true;
		}

		private void FireRound(FVRFireArmRound round)
        {
			round.Splode(1f, false, true);
        }

		private GameObject DuplicateFromSpawnLock(FVRPhysicalObject physicalObject)
        {
			GameObject gameObject = Instantiate(physicalObject.ObjectWrapper.GetGameObject(), physicalObject.Transform.position, physicalObject.Transform.rotation);
			FVRPhysicalObject component = gameObject.GetComponent<FVRPhysicalObject>();
			component.SetQuickBeltSlot(null);
			if (physicalObject.MP.IsMeleeWeapon && component.MP.IsThrownDisposable)
			{
				component.MP.IsCountingDownToDispose = true;
				if (component.MP.m_isThrownAutoAim)
				{
					component.MP.SetReadyToAim(true);
					component.MP.SetPose(physicalObject.MP.PoseIndex);
				}
			}
			return gameObject;
		}

		private void AlignHeldObject()
        {
			if (CurObject != null && CurObject.transform.forward != transform.forward)
			{
				Quaternion objectRot = CurObject.transform.localRotation;
				PoseOverride.transform.localRotation = Quaternion.Inverse(objectRot);
			}
		}
	}
}
