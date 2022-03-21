using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
	public class AttachableMagazine : OpenScripts2_BasePlugin
	{
		public FVRFireArmMagazine Magazine;
		
		public FVRFireArmAttachment Attachment;

		public bool AttachInstantly;

		private bool _AttachmentLocked = false;
		private FVRFireArmReloadTriggerMag _ReloadTriggerMag;


		private Vector3 _base_attachmentPos;
		private Vector3 _base_attachmentEuler;

		private Vector3 _secondary_attachmentPos;
		private Vector3 _secondary_attachmentEuler;
#if !DEBUG

		public void Start()
        {
			Hook();

			if (Attachment.transform.parent == Magazine.transform)
			{
				Transform magParent = Magazine.transform.parent;
				SetBaseTransform();
				UseSecondaryParenting();
				SetSecondaryTransform();
				UseBaseParenting(magParent);
				UseBaseTransform();

                //Debug.Log("Mag based Setup complete!");

            }
			else if (Magazine.transform.parent == Attachment.transform)
			{
				Transform attachmentParent = Attachment.transform.parent;
				SetSecondaryTransform();
				UseBaseParenting();
				SetBaseTransform();
				UseSecondaryParenting(attachmentParent);
				UseSecondaryTransform();
				_AttachmentLocked = true;
				Attachment.Sensor.CurHoveredMount = Attachment.curMount;
				//Debug.Log("Attachment based Setup complete!");
			}
            else
            {
				Debug.LogError("Attachable Mag Setup failed!");
            }


			if (Magazine.transform.parent == Attachment.transform)
			{
				Magazine.StoreAndDestroyRigidbody();
				Magazine.gameObject.layer = LayerMask.NameToLayer("NoCol");
			}
            else
            {
				Attachment.StoreAndDestroyRigidbody();
				Attachment.gameObject.layer = LayerMask.NameToLayer("NoCol");
			}

			_ReloadTriggerMag = Magazine.GetComponentInChildren<FVRFireArmReloadTriggerMag>();
		}
		
        public void Update()
        {
			if (Attachment.Sensor.CurHoveredMount != null && !_AttachmentLocked)
            {
				//StartHoverMode();
				/*attachmentLocked = true;
				Attachment.RecoverRigidbody();
				Attachment.AttachmentInterface.SetAllCollidersToLayer(true, "Interactable");
				FVRViveHand hand = Magazine.m_hand;
				Magazine.ForceBreakInteraction();
				hand.ForceSetInteractable(Attachment);
				Attachment.BeginInteraction(hand);
				Magazine.StoreAndDestroyRigidbody();
				Magazine.SetParentage(Attachment.transform);

				Magazine.gameObject.layer = LayerMask.NameToLayer("NoCol");*/
			}
			else if (Attachment.Sensor.CurHoveredMount == null && _AttachmentLocked)
            {
				_AttachmentLocked = false;
				Attachment.gameObject.layer = LayerMask.NameToLayer("NoCol");
				Magazine.SetParentage(null);
				Magazine.RecoverRigidbody();
				FVRViveHand hand = Attachment.m_hand;
				Attachment.ForceBreakInteraction();
				Attachment.SetParentage(Magazine.transform);
				Attachment.StoreAndDestroyRigidbody();
				if (hand != null)
				{
					hand.ForceSetInteractable(Magazine);
					Magazine.BeginInteraction(hand);
				}
				Magazine.gameObject.layer = LayerMask.NameToLayer("Interactable");
			}
			if (_AttachmentLocked) UseSecondaryTransform();
			else
			{
				UseBaseTransform();
			}

            if (Magazine.State == FVRFireArmMagazine.MagazineState.Locked)
            {
				Attachment.Sensor.gameObject.layer = LayerMask.NameToLayer("NoCol");
			}
			else if (Attachment.IsHovered)
            {
				_ReloadTriggerMag.gameObject.layer = LayerMask.NameToLayer("NoCol");
			}
            else
            {
				Attachment.Sensor.gameObject.layer = LayerMask.NameToLayer("Interactable");
				_ReloadTriggerMag.gameObject.layer = LayerMask.NameToLayer("Interactable");
			}
		}

		private void SetBaseTransform()
        {
			_base_attachmentPos = Attachment.transform.localPosition;
			_base_attachmentEuler = Attachment.transform.localEulerAngles;
		}
		private void SetSecondaryTransform()
        {
			_secondary_attachmentPos = Magazine.transform.localPosition;
			_secondary_attachmentEuler = Magazine.transform.localEulerAngles;
        }

		private void UseBaseTransform()
        {
			Attachment.transform.localPosition = _base_attachmentPos;
			Attachment.transform.localEulerAngles = _base_attachmentEuler;
		}

		private void UseSecondaryTransform()
        {
			Magazine.transform.localPosition = _secondary_attachmentPos;
			Magazine.transform.localEulerAngles = _secondary_attachmentEuler;
		}

		private void UseBaseParenting(Transform magParent = null)
        {
			Magazine.SetParentage(magParent);
			Attachment.SetParentage(Magazine.transform);
		}

		private void UseSecondaryParenting(Transform attachmentParent = null)
        {
			Attachment.SetParentage(attachmentParent);
			Magazine.SetParentage(Attachment.transform);
		}

		private void Hook()
        {

			On.FistVR.FVRFireArmAttachmentSensor.OnTriggerEnter += FVRFireArmAttachmentSensor_OnTriggerEnter;

		}

		private void FVRFireArmAttachmentSensor_OnTriggerEnter(On.FistVR.FVRFireArmAttachmentSensor.orig_OnTriggerEnter orig, FVRFireArmAttachmentSensor self, Collider collider)
        {
			if (self == Attachment.Sensor)
            {
				if (self.CurHoveredMount == null && self.Attachment.CanAttach() && collider.gameObject.tag == "FVRFireArmAttachmentMount")
				{
					FVRFireArmAttachmentMount component = collider.gameObject.GetComponent<FVRFireArmAttachmentMount>();
					if (component.Type == self.Attachment.Type && component.isMountableOn(self.Attachment))
					{
						if (!AttachInstantly)
						{
							if (!_AttachmentLocked) StartHoverMode();
							self.SetHoveredMount(component);
							component.BeginHover();
						}
                        else
                        {
							self.SetHoveredMount(component);
							if (!_AttachmentLocked) InstantlyAttachToMount(component);
						}
					}
				}
			}
			else 
			orig(self, collider);
        }


		private void StartHoverMode()
        {
			_AttachmentLocked = true;
			Attachment.RecoverRigidbody();
			Attachment.AttachmentInterface.gameObject.layer = LayerMask.NameToLayer("Interactable");
			FVRViveHand hand = Magazine.m_hand;
			Magazine.ForceBreakInteraction();
			hand.ForceSetInteractable(Attachment);
			Attachment.BeginInteraction(hand);
			Magazine.StoreAndDestroyRigidbody();
			Magazine.SetParentage(Attachment.transform);
			Magazine.gameObject.layer = LayerMask.NameToLayer("NoCol");
		}

		private void InstantlyAttachToMount(FVRFireArmAttachmentMount mount)
        {
			_AttachmentLocked = true;
			Magazine.ForceBreakInteraction();
			Magazine.StoreAndDestroyRigidbody();
			Attachment.RecoverRigidbody();
			Attachment.AttachmentInterface.gameObject.layer = LayerMask.NameToLayer("Interactable");
			UseSecondaryParenting();
            Attachment.AttachToMount(mount, true);
			Magazine.gameObject.layer = LayerMask.NameToLayer("NoCol");
		}
#endif
	}
}
