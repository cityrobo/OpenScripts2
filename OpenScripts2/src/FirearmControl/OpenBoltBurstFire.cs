using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
	public class OpenBoltBurstFire : OpenScripts2_BasePlugin
	{
		public OpenBoltReceiver OpenBoltReceiver;

		[Tooltip("Selector setting position that will be burst. Remember, selectors go pos: 0, 1 ,2, not 1, 2, 3")]
		public int SelectorSetting = 0;

		public  int BurstAmount = 1;
		private int _shotsSoFar;

		public void Awake()
		{
			OpenBoltReceiver.FireSelector_Modes[SelectorSetting].ModeType = OpenBoltReceiver.FireSelectorModeType.FullAuto;

			GM.CurrentSceneSettings.ShotFiredEvent += ShotFired;
		}

		public void OnDestroy()
        {
			GM.CurrentSceneSettings.ShotFiredEvent -= ShotFired;
		}

		public void Update()
		{
			// if it's not the correct selector, just don't do anything
			if (OpenBoltReceiver.m_hand == null) return;
			if (OpenBoltReceiver.m_fireSelectorMode != SelectorSetting)
			{
				_shotsSoFar = 0;
				return;
			}
			// if burst amount hit
			if (_shotsSoFar >= BurstAmount)
			{
				LockUp();
			}

			// reset amount if trigger is let go and unlock
			if (OpenBoltReceiver.m_hand.Input.TriggerFloat < OpenBoltReceiver.TriggerResetThreshold)
			{
				_shotsSoFar = 0;
				Unlock();
			}
		}

		public void ShotFired(FVRFireArm fireArm)
        {
			if (fireArm == OpenBoltReceiver && OpenBoltReceiver.m_fireSelectorMode == SelectorSetting) _shotsSoFar++;
		}

		public void LockUp()
		{
			// put to safe
			OpenBoltReceiver.FireSelector_Modes[SelectorSetting].ModeType = OpenBoltReceiver.FireSelectorModeType.Safe;
		}

		public void Unlock()
		{
			// put to auto; reset
			_shotsSoFar = 0;
			OpenBoltReceiver.FireSelector_Modes[SelectorSetting].ModeType = OpenBoltReceiver.FireSelectorModeType.FullAuto;
		}
	}
}
