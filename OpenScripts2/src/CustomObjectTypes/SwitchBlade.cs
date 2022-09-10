using System.Collections;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
	public class SwitchBlade : FVRMeleeWeapon
	{
		[Header("Switchblade Config")]
		public Transform Blade = null!;
		public OpenScripts2_BasePlugin.Axis BladeRotAxis = OpenScripts2_BasePlugin.Axis.X;
		public float BladeOpenRot = 0f;
		public float BladeClosedRot = 180f;
		public float BladeOpeningTime;
		public float BladeClosingTime;
		public AudioEvent OpeningSounds;
		public AudioEvent ClosingSounds;
		public enum SwitchBladeState
		{
			Closed,
			Open
		}

		private SwitchBladeState _switchbladeState = SwitchBladeState.Closed;
		private float _timeElapsed;

		public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);
			if (IsHeld && m_hasTriggeredUpSinceBegin && hand.Input.TriggerDown) ToggleSwitchBladeState();
		}
		private void ToggleSwitchBladeState()
		{
			if (MP.IsJointedToObject) return;
			else if (_switchbladeState == SwitchBladeState.Closed)
			{
				SM.PlayGenericSound(OpeningSounds, transform.position);
				StopAllCoroutines();
				StartCoroutine(OpenBlade());
			}
			else if (_switchbladeState == SwitchBladeState.Open)
			{
				SM.PlayGenericSound(ClosingSounds, transform.position);
				StopAllCoroutines();
				StartCoroutine(CloseBlade());
			}
		}
		private void SetBladeRot(float f)
		{
			Blade.localRotation = OpenScripts2_BasePlugin.GetTargetQuaternionFromAxis( Mathf.Lerp(BladeClosedRot, BladeOpenRot, f),BladeRotAxis );
		}
		private IEnumerator OpenBlade()
        {
			_timeElapsed = 0f;
			_switchbladeState = SwitchBladeState.Open;
			while (_timeElapsed < BladeOpeningTime)
			{
				_timeElapsed += Time.deltaTime;
				SetBladeRot(_timeElapsed / BladeOpeningTime);
				yield return null;
			}
			SetBladeRot(1f);
			MP.CanNewStab = true;
		}
		private IEnumerator CloseBlade()
		{
			_timeElapsed = 0f;
			MP.CanNewStab = false;
			_switchbladeState = SwitchBladeState.Closed;
			while (_timeElapsed < BladeClosingTime)
            {
				_timeElapsed += Time.deltaTime;
				SetBladeRot(1f - (_timeElapsed / BladeClosingTime));
				yield return null;
			}
			SetBladeRot(0f);
		}
    }
}
