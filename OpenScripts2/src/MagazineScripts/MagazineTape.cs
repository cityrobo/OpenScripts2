using System;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using System.ComponentModel;

namespace OpenScripts2
{
    public class MagazineTape : OpenScripts2_BasePlugin
    {
        [Tooltip("Main Magazine")]
        public FVRFireArmMagazine PrimaryMagazine;
        [Tooltip("Attached Magazine")]
        public FVRFireArmMagazine SecondaryMagazine;
        [Tooltip("Tape visuals and colliders (not a requirement)")]
        public GameObject Tape = null;

        [Header("Relative Mag Positions (Use Context Menu to calculate)")]
        [Tooltip("Primary mag position when parented to secondary mag.")]
        [ReadOnly] public Vector3 Primary2SecondaryPos;
        [Tooltip("Primary mag rotation when parented to secondary mag.")]
        [ReadOnly] public Quaternion Primary2SecondaryRot;

        [Tooltip("Secondary mag position when parented to primary mag.")]
        [ReadOnly] public Vector3 Secondary2PrimaryPos;
        [Tooltip("Primary mag rotation when parented to primary mag.")]
        [ReadOnly] public Quaternion Secondary2PrimaryRot;

        [ContextMenu("Calculate Relative Mag Positions")]
        public void CalculateReltativeMagPositions()
        {
            Secondary2PrimaryPos = PrimaryMagazine.transform.InverseTransformPoint(SecondaryMagazine.transform.position);
            Secondary2PrimaryRot = Quaternion.Inverse(PrimaryMagazine.transform.rotation) * SecondaryMagazine.transform.rotation;

            Primary2SecondaryPos = SecondaryMagazine.transform.InverseTransformPoint(PrimaryMagazine.transform.position);
            Primary2SecondaryRot = Quaternion.Inverse(SecondaryMagazine.transform.rotation) * PrimaryMagazine.transform.rotation;
        }

        private enum ActiveMagazine
        {
            primary,
            secondary
        }

        private enum AttachedMagazine
        {
            none,
            primary,
            secondary
        }

        private ActiveMagazine _activeMagazine = ActiveMagazine.primary;
        private AttachedMagazine _attachedMagazine = AttachedMagazine.none;

#if !(DEBUG || MEATKIT)
        public void Start()
        {
            if (PrimaryMagazine.State == FVRFireArmMagazine.MagazineState.Locked)
            {
                _attachedMagazine = AttachedMagazine.primary;
                SecondaryMagazine.StoreAndDestroyRigidbody();
                SecondaryMagazine.gameObject.layer = LayerMask.NameToLayer("NoCol");
            }
            else if (SecondaryMagazine.State == FVRFireArmMagazine.MagazineState.Locked)
            {
                _attachedMagazine = AttachedMagazine.secondary;
                _activeMagazine = ActiveMagazine.secondary;

                PrimaryMagazine.StoreAndDestroyRigidbody();
                PrimaryMagazine.gameObject.layer = LayerMask.NameToLayer("NoCol");
            }
            else if (PrimaryMagazine.transform.parent == SecondaryMagazine.transform)
            {
                _activeMagazine = ActiveMagazine.secondary;

                PrimaryMagazine.StoreAndDestroyRigidbody();
                PrimaryMagazine.gameObject.layer = LayerMask.NameToLayer("NoCol");
            }
            else
            {
                _activeMagazine = ActiveMagazine.primary;

                SecondaryMagazine.StoreAndDestroyRigidbody();
                SecondaryMagazine.gameObject.layer = LayerMask.NameToLayer("NoCol");
            }

            Hook();
        }

        public void OnDestroy()
        {
            Unhook();
        }
        public void Update()
        {
            try
            {
                if (PrimaryMagazine.State == FVRFireArmMagazine.MagazineState.Locked && _attachedMagazine == AttachedMagazine.none && _activeMagazine == ActiveMagazine.secondary)
                {
                    _attachedMagazine = AttachedMagazine.primary;
                    SecondaryMagazine.ForceBreakInteraction();
                    SecondaryMagazine.IsHeld = false;
                    SecondaryMagazine.gameObject.layer = LayerMask.NameToLayer("NoCol");
                    PrimaryMagazine.gameObject.layer = LayerMask.NameToLayer("Interactable");
                    UsePrimaryAsParent(PrimaryMagazine.transform.parent);
                    SecondaryMagazine.StoreAndDestroyRigidbody();
                }
                else if (SecondaryMagazine.State == FVRFireArmMagazine.MagazineState.Locked && _attachedMagazine == AttachedMagazine.none && _activeMagazine == ActiveMagazine.primary)
                {
                    _attachedMagazine = AttachedMagazine.secondary;
                    PrimaryMagazine.ForceBreakInteraction();
                    PrimaryMagazine.IsHeld = false;
                    PrimaryMagazine.gameObject.layer = LayerMask.NameToLayer("NoCol");
                    SecondaryMagazine.gameObject.layer = LayerMask.NameToLayer("Interactable");
                    UseSecondaryAsParent(SecondaryMagazine.transform.parent);
                    PrimaryMagazine.StoreAndDestroyRigidbody();
                }
                else if (PrimaryMagazine.State == FVRFireArmMagazine.MagazineState.Free && SecondaryMagazine.State == FVRFireArmMagazine.MagazineState.Free)
                {
                    _attachedMagazine = AttachedMagazine.none;
                }

                if (_activeMagazine == ActiveMagazine.primary) UpdateSecondaryMagTransform();
                else if (_activeMagazine == ActiveMagazine.secondary) UpdatePrimaryMagTransform();
            }
            catch (Exception e)
            {
                if (PrimaryMagazine == null || SecondaryMagazine == null)
                {
                    Destroy(Tape);
                    Destroy(this.GetComponent<MagazineTape>());
                }
                else
                {
                    this.LogError("Error in MagazineTapeMK2 Script!");
                    this.LogException(e);
                }
                
            }

            if (_activeMagazine == ActiveMagazine.primary)
            {
                if (PrimaryMagazine.m_hand != null)
                {
                    FVRViveHand hand = PrimaryMagazine.m_hand;
                    if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f)
                    {
                        ChangeActiveToSecondary(hand);
                    }
                }
            }
            else if (_activeMagazine == ActiveMagazine.secondary)
            {
                if (SecondaryMagazine.m_hand != null)
                {
                    FVRViveHand hand = SecondaryMagazine.m_hand;
                    if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f)
                    {
                        ChangeActiveToPrimary(hand);
                    }
                }
            }
        }

        private void UsePrimaryAsParent(Transform parent = null)
        {
            _activeMagazine = ActiveMagazine.primary;
            PrimaryMagazine.transform.SetParent(parent);
            SecondaryMagazine.transform.SetParent(PrimaryMagazine.transform);
        }

        private void UseSecondaryAsParent(Transform parent = null)
        {
            _activeMagazine = ActiveMagazine.secondary;
            SecondaryMagazine.transform.SetParent(parent);
            PrimaryMagazine.transform.SetParent(SecondaryMagazine.transform);
        }

        private void UpdateSecondaryMagTransform()
        {
            SecondaryMagazine.transform.localPosition = Secondary2PrimaryPos;
            SecondaryMagazine.transform.localRotation = Secondary2PrimaryRot;
        }

        private void UpdatePrimaryMagTransform()
        {
            PrimaryMagazine.transform.localPosition = Primary2SecondaryPos;
            PrimaryMagazine.transform.localRotation = Primary2SecondaryRot;
        }

        private void ChangeActiveToPrimary(FVRViveHand hand)
        {
            SecondaryMagazine.ForceBreakInteraction();
            SecondaryMagazine.IsHeld = false;
            SecondaryMagazine.gameObject.layer = LayerMask.NameToLayer("NoCol");
            PrimaryMagazine.RecoverRigidbody();
            UsePrimaryAsParent();

            SecondaryMagazine.StoreAndDestroyRigidbody();
            hand.ForceSetInteractable(PrimaryMagazine);
            PrimaryMagazine.BeginInteraction(hand);
            PrimaryMagazine.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }

        private void ChangeActiveToSecondary(FVRViveHand hand)
        {
            PrimaryMagazine.ForceBreakInteraction();
            PrimaryMagazine.IsHeld = false;
            PrimaryMagazine.gameObject.layer = LayerMask.NameToLayer("NoCol");
            SecondaryMagazine.RecoverRigidbody();
            UseSecondaryAsParent();

            PrimaryMagazine.StoreAndDestroyRigidbody();
            hand.ForceSetInteractable(SecondaryMagazine);
            SecondaryMagazine.BeginInteraction(hand);
            SecondaryMagazine.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }

        void Unhook()
        {
            //On.FistVR.FVRFireArmMagazine.ReloadMagWithType -= FVRFireArmMagazine_ReloadMagWithType;
            On.FistVR.FVRFireArmMagazine.DuplicateFromSpawnLock -= FVRFireArmMagazine_DuplicateFromSpawnLock;
        }

        void Hook()
        {
            //On.FistVR.FVRFireArmMagazine.ReloadMagWithType += FVRFireArmMagazine_ReloadMagWithType;
            On.FistVR.FVRFireArmMagazine.DuplicateFromSpawnLock += FVRFireArmMagazine_DuplicateFromSpawnLock;
            On.FistVR.FVRWristMenu.CleanUpScene_Empties += FVRWristMenu_CleanUpScene_Empties;
        }

        private void FVRWristMenu_CleanUpScene_Empties(On.FistVR.FVRWristMenu.orig_CleanUpScene_Empties orig, FVRWristMenu self)
        {
            self.Aud.PlayOneShot(self.AudClip_Engage, 1f);
            if (!self.askConfirm_CleanupEmpties)
            {
                self.ResetConfirm();
                self.AskConfirm_CleanupEmpties();
                return;
            }
            self.ResetConfirm();
            FVRFireArmMagazine[] array = UnityEngine.Object.FindObjectsOfType<FVRFireArmMagazine>();
            for (int i = array.Length - 1; i >= 0; i--)
            {
                if (!array[i].IsHeld && array[i].QuickbeltSlot == null && array[i].FireArm == null && array[i].m_numRounds == 0 && array[i].GetComponentInChildren<MagazineTape>() == null)
                {
                    UnityEngine.Object.Destroy(array[i].gameObject);
                }
            }
            FVRFireArmRound[] array2 = UnityEngine.Object.FindObjectsOfType<FVRFireArmRound>();
            for (int j = array2.Length - 1; j >= 0; j--)
            {
                if (!array2[j].IsHeld && array2[j].QuickbeltSlot == null && array2[j].RootRigidbody != null)
                {
                    UnityEngine.Object.Destroy(array2[j].gameObject);
                }
            }
            FVRFireArmClip[] array3 = UnityEngine.Object.FindObjectsOfType<FVRFireArmClip>();
            for (int k = array3.Length - 1; k >= 0; k--)
            {
                if (!array3[k].IsHeld && array3[k].QuickbeltSlot == null && array3[k].FireArm == null && array3[k].m_numRounds == 0)
                {
                    UnityEngine.Object.Destroy(array3[k].gameObject);
                }
            }
            Speedloader[] array4 = UnityEngine.Object.FindObjectsOfType<Speedloader>();
            for (int l = array4.Length - 1; l >= 0; l--)
            {
                if (!array4[l].IsHeld && array4[l].QuickbeltSlot == null)
                {
                    UnityEngine.Object.Destroy(array4[l].gameObject);
                }
            }
        }

        private GameObject FVRFireArmMagazine_DuplicateFromSpawnLock(On.FistVR.FVRFireArmMagazine.orig_DuplicateFromSpawnLock orig, FVRFireArmMagazine self, FVRViveHand hand)
        {
            GameObject gameObject = orig(self, hand);

            if (self == PrimaryMagazine)
            {
                MagazineTape tape = gameObject.GetComponent<MagazineTape>();

                FVRFireArmMagazine component = tape.SecondaryMagazine;
                for (int i = 0; i < Mathf.Min(this.SecondaryMagazine.LoadedRounds.Length, component.LoadedRounds.Length); i++)
                {
                    if (this.SecondaryMagazine.LoadedRounds[i] != null && this.SecondaryMagazine.LoadedRounds[i].LR_Mesh != null)
                    {
                        component.LoadedRounds[i].LR_Class = this.SecondaryMagazine.LoadedRounds[i].LR_Class;
                        component.LoadedRounds[i].LR_Mesh = this.SecondaryMagazine.LoadedRounds[i].LR_Mesh;
                        component.LoadedRounds[i].LR_Material = this.SecondaryMagazine.LoadedRounds[i].LR_Material;
                        component.LoadedRounds[i].LR_ObjectWrapper = this.SecondaryMagazine.LoadedRounds[i].LR_ObjectWrapper;
                    }
                }
                component.m_numRounds = this.SecondaryMagazine.m_numRounds;
                component.UpdateBulletDisplay();
                return gameObject;
            }
            else if (self == SecondaryMagazine)
            {
                MagazineTape tape = gameObject.GetComponentInChildren<MagazineTape>();

                FVRFireArmMagazine component = tape.PrimaryMagazine;
                for (int i = 0; i < Mathf.Min(this.PrimaryMagazine.LoadedRounds.Length, component.LoadedRounds.Length); i++)
                {
                    if (this.PrimaryMagazine.LoadedRounds[i] != null && this.PrimaryMagazine.LoadedRounds[i].LR_Mesh != null)
                    {
                        component.LoadedRounds[i].LR_Class = this.PrimaryMagazine.LoadedRounds[i].LR_Class;
                        component.LoadedRounds[i].LR_Mesh = this.PrimaryMagazine.LoadedRounds[i].LR_Mesh;
                        component.LoadedRounds[i].LR_Material = this.PrimaryMagazine.LoadedRounds[i].LR_Material;
                        component.LoadedRounds[i].LR_ObjectWrapper = this.PrimaryMagazine.LoadedRounds[i].LR_ObjectWrapper;
                    }
                }
                component.m_numRounds = this.PrimaryMagazine.m_numRounds;
                component.UpdateBulletDisplay();
                return gameObject;
            }
            else return gameObject;
        }

        private void FVRFireArmMagazine_ReloadMagWithType(On.FistVR.FVRFireArmMagazine.orig_ReloadMagWithType orig, FVRFireArmMagazine self, FireArmRoundClass rClass)
        {
            if (self == PrimaryMagazine || self == SecondaryMagazine)
            {
                PrimaryMagazine.m_numRounds = 0;
                for (int i = 0; i < PrimaryMagazine.m_capacity; i++)
                {
                    PrimaryMagazine.AddRound(rClass, false, false);
                }
                PrimaryMagazine.UpdateBulletDisplay();

                SecondaryMagazine.m_numRounds = 0;
                for (int i = 0; i < SecondaryMagazine.m_capacity; i++)
                {
                    SecondaryMagazine.AddRound(rClass, false, false);
                }
                SecondaryMagazine.UpdateBulletDisplay();
            }
            else orig(self, rClass);
        }
#endif
    }
}

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}
