using System.Collections;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
	public class SwitchBlade : FVRMeleeWeapon
	{
		public Transform Blade;
		public Vector2 BladeRotRange = new Vector2(-90f, 90f);
		public float BladeOpeningTime;
		public float BladeClosingTime;
		public AudioEvent OpenSounds;
		public AudioEvent CloseSounds;
		public enum SwitchBladeState
		{
			Closing,
			Closed,
			Opening,
			Open,
		}

		private SwitchBladeState _switchbladeState = SwitchBladeState.Closed;
		private float _timeElapsed;

		public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);
			if (base.IsHeld && this.m_hand.Input.TriggerDown && this.m_hasTriggeredUpSinceBegin)
			{
				this.ToggleSwitchBladeState();
			}
		}

		private void ToggleSwitchBladeState()
		{
			if (this.MP.IsJointedToObject)
			{
				return;
			}
			if (this._switchbladeState == SwitchBladeState.Closed)
			{
				SM.PlayGenericSound(OpenSounds, transform.position);
				this.StartCoroutine("OpenBlade");
				this.MP.CanNewStab = true;
			}
			else if (this._switchbladeState == SwitchBladeState.Open)
			{
				SM.PlayGenericSound(CloseSounds, transform.position);
				this.StartCoroutine("CloseBlade");
				this.MP.CanNewStab = false;
			}
		}

		private void SetBladeRot(float f)
		{
			this.Blade.localEulerAngles = new Vector3(Mathf.Lerp(this.BladeRotRange.x, this.BladeRotRange.y, f), 0f, 0f);
		}

		private IEnumerator OpenBlade()
        {
			this._switchbladeState = SwitchBladeState.Opening;
			_timeElapsed = 0f;
			while (_timeElapsed < BladeOpeningTime)
			{
				_timeElapsed += Time.deltaTime;
				SetBladeRot(_timeElapsed / BladeOpeningTime);
				yield return null;
			}
			SetBladeRot(1f);
			this._switchbladeState = SwitchBladeState.Open;
		}

		private IEnumerator CloseBlade()
		{
			this._switchbladeState = SwitchBladeState.Closing;
			_timeElapsed = 0f;
			while (_timeElapsed < BladeClosingTime)
            {
				_timeElapsed += Time.deltaTime;
				SetBladeRot(1f - (_timeElapsed / BladeClosingTime));
				yield return null;
			}
			SetBladeRot(0f);
			this._switchbladeState = SwitchBladeState.Closed;
		}

    }
}
