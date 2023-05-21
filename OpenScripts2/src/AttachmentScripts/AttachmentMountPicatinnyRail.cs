using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;


namespace OpenScripts2
{
    public class AttachmentMountPicatinnyRail : OpenScripts2_BasePlugin
    {
        [Header("Picatinny Rail Config")]
        public FVRFireArmAttachmentMount Mount;
        public int NumberOfPicatinnySlots = 0;
        [Tooltip("Sound played while moving attachment between slots.")]
        public AudioEvent SlotSound;
        [Header("Optional")]
        [Tooltip("Excluding Front and Back pos!")]
        public List<Transform> SpecificSlotPositions = new();


        [HideInInspector]
        public static Dictionary<FVRFireArmAttachment, AttachmentPicatinnyRailForwardStop> ExistingForwardStops = new();
        [HideInInspector]
        public static Dictionary<FVRFireArmAttachmentMount, AttachmentMountPicatinnyRail> _exisingAttachmentMountPicatinnyRail = new();

        private bool _isPatched = false;

        private float _slotLerpFactor = 0f;
        private static IntPtr _methodPointer;

        private int _lastPosIndex = -1;

        private bool _usesSpecificSlotLerps = false;
        private List<float> _specificSlotLerps = new();
        private List<Vector3> _specificSlotPos = new();

        static AttachmentMountPicatinnyRail()
        {
            MethodInfo _methodInfo = typeof(FVRPhysicalObject).GetMethod(nameof(FVRPhysicalObject.GetPosTarget), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _methodPointer = _methodInfo.MethodHandle.GetFunctionPointer();

#if !DEBUG
            On.FistVR.FVRFireArmAttachment.AttachToMount += FVRFireArmAttachment_AttachToMount;
            On.FistVR.FVRFireArmAttachment.DetachFromMount += FVRFireArmAttachment_DetachFromMount;
            On.FistVR.FVRFireArmAttachment.GetPosTarget += FVRFireArmAttachment_GetPosTarget;
#endif
        }
#if !DEBUG
        private static void FVRFireArmAttachment_AttachToMount(On.FistVR.FVRFireArmAttachment.orig_AttachToMount orig, FVRFireArmAttachment self, FVRFireArmAttachmentMount m, bool playSound)
        {
            if (_exisingAttachmentMountPicatinnyRail.TryGetValue(m, out AttachmentMountPicatinnyRail picatinnyRail))
            {
                self.curMount = m;
                self.StoreAndDestroyRigidbody();
                if (self.curMount.GetComponent<AttachmentMountParentToThis>() != null)
                {
                    self.SetParentage(self.curMount.transform);
                }
                else if (self.curMount.GetRootMount().ParentToThis)
                {
                    self.SetParentage(self.curMount.GetRootMount().transform);
                }
                else
                {
                    self.SetParentage(self.curMount.MyObject.transform);
                }
                if (self.IsBiDirectional)
                {
                    if (Vector3.Dot(self.transform.forward, self.curMount.transform.forward) >= 0f) self.transform.rotation = self.curMount.transform.rotation;
                    else self.transform.rotation = Quaternion.LookRotation(-self.curMount.transform.forward, self.curMount.transform.up);
                }
                else self.transform.rotation = self.curMount.transform.rotation;

                // New Code
                Vector3 front = self.curMount.Point_Front.position;
                Vector3 rear = self.curMount.Point_Rear.position;

                int posIndex = picatinnyRail.GetPosIndex(self, false);

                Vector3 snapPos = picatinnyRail._usesSpecificSlotLerps
                    ? m.transform.TransformPoint(picatinnyRail._specificSlotPos[posIndex])
                    : Vector3.Lerp(front, rear, posIndex * picatinnyRail._slotLerpFactor);

                self.transform.position = snapPos;

                self.curMount.Parent?.RegisterAttachment(self);
                self.curMount.RegisterAttachment(self);
                if (self.curMount.Parent != null && self.curMount.Parent.QuickbeltSlot != null)
                {
                    self.SetAllCollidersToLayer(false, "NoCol");
                }
                else
                {
                    self.SetAllCollidersToLayer(false, "Default");
                }
                if (self.AttachmentInterface != null)
                {
                    self.AttachmentInterface.OnAttach();
                    self.AttachmentInterface.gameObject.SetActive(true);
                }
                self.SetTriggerState(false);
                self.DisableOnAttached?.SetActive(false);
            }
            else orig(self, m, playSound);
        }

        private static void FVRFireArmAttachment_DetachFromMount(On.FistVR.FVRFireArmAttachment.orig_DetachFromMount orig, FVRFireArmAttachment self)
        {
            if (_exisingAttachmentMountPicatinnyRail.TryGetValue(self.curMount, out AttachmentMountPicatinnyRail attachmentMountPicatinnyRail))
            {
                attachmentMountPicatinnyRail._lastPosIndex = -1;
            }
            orig(self);
        }
        private static Vector3 FVRFireArmAttachment_GetPosTarget(On.FistVR.FVRFireArmAttachment.orig_GetPosTarget orig, FVRFireArmAttachment self)
        {
            if (self.Sensor.CurHoveredMount != null && _exisingAttachmentMountPicatinnyRail.TryGetValue(self.Sensor.CurHoveredMount, out AttachmentMountPicatinnyRail rail))
            {
                Func<Vector3> func = (Func<Vector3>)Activator.CreateInstance(typeof(Func<Vector3>), self, _methodPointer);

                if (self.Sensor.CurHoveredMount == null)
                {
                    return func();
                }
                Vector3 front = self.Sensor.CurHoveredMount.Point_Front.position;
                Vector3 rear = self.Sensor.CurHoveredMount.Point_Rear.position;
                Vector3 closestValidPoint = self.GetClosestValidPoint(front, rear, self.m_handPos);

                if (Vector3.Distance(closestValidPoint, self.m_handPos) < 0.15f)
                {
                    int posIndex = rail.GetPosIndex(self, true);

                    Vector3 snapPos;
                    if (rail._usesSpecificSlotLerps)
                    {
                        snapPos = rail.Mount.transform.TransformPoint(rail._specificSlotPos[posIndex]);
                    }
                    else
                    {
                        snapPos = Vector3.Lerp(front, rear, posIndex * rail._slotLerpFactor);
                    }

                    if (posIndex != rail._lastPosIndex)
                    {
                        SM.PlayGenericSound(rail.SlotSound, self.transform.position);
                    }
                    rail._lastPosIndex = posIndex;
                    return snapPos;
                }
                return func();
            }
            return orig(self);
        }
#endif
        public void Awake()
        {
            _exisingAttachmentMountPicatinnyRail.Add(Mount, this);

            _slotLerpFactor = 1f/(NumberOfPicatinnySlots - 1);

            if (SpecificSlotPositions.Count > 0)
            {
                _usesSpecificSlotLerps = true;
                _specificSlotLerps.AddRange(new float[]{0f, 1f});
                foreach (Transform slotPos in SpecificSlotPositions)
                {
                    _specificSlotLerps.Add(Vector3Utils.InverseLerp(Mount.Point_Front.position, Mount.Point_Rear.position, slotPos.position));
                }
                foreach (float specificLerp in _specificSlotLerps)
                {
                    _specificSlotPos.Add(Mount.transform.InverseTransformPoint(Vector3.Lerp(Mount.Point_Front.position, Mount.Point_Rear.position, specificLerp)));
                }

                NumberOfPicatinnySlots = _specificSlotPos.Count;

                for (int i = 0; i < SpecificSlotPositions.Count; i++)
                {
                    Destroy(SpecificSlotPositions[i].gameObject);
                }
                SpecificSlotPositions.Clear();
            }
        }

        public void OnDestroy()
        {
            _exisingAttachmentMountPicatinnyRail.Remove(Mount);
        }

        private int GetPosIndex(FVRFireArmAttachment attachment, bool useHandPos)
        {
            Vector3 front = attachment.curMount != null ? attachment.curMount.Point_Front.position : attachment.Sensor.CurHoveredMount.Point_Front.position;
            Vector3 rear = attachment.curMount != null ? attachment.curMount.Point_Rear.position : attachment.Sensor.CurHoveredMount.Point_Rear.position;
            Vector3 closestValidPoint = useHandPos ? attachment.GetClosestValidPoint(front, rear, attachment.m_handPos) : attachment.GetClosestValidPoint(front, rear, attachment.transform.position);
            float inverseLerp = Vector3Utils.InverseLerp(front, rear, closestValidPoint);

            int posIndex = 0;
            if (_usesSpecificSlotLerps)
            {
                float min = float.MaxValue;
                float diff;
                for (int i = 0; i < _specificSlotLerps.Count; i++)
                {
                    diff = Mathf.Abs(_specificSlotLerps[i] - inverseLerp);
                    if (diff < min)
                    {
                        posIndex = i;
                        min = diff;
                    }
                }
            }
            else
            {
                posIndex = Mathf.RoundToInt(inverseLerp / _slotLerpFactor);
            }

            if (ExistingForwardStops.TryGetValue(attachment, out AttachmentPicatinnyRailForwardStop forwardStop))
            {
                Vector3 closestValidPointLimit = closestValidPoint + attachment.transform.forward * Vector3.Distance(forwardStop.transform.position, attachment.transform.position);
                float inverseLerpUnclamped = Vector3Utils.InverseLerpUnclamped(front, rear, closestValidPointLimit);
                int posIndexLimit = 0;
                if (_usesSpecificSlotLerps)
                {
                    if (inverseLerpUnclamped > 1f)
                    {
                        inverseLerp -= (inverseLerpUnclamped - 1f);
                    }
                    else if (inverseLerpUnclamped < 0f)
                    {
                        inverseLerp -= inverseLerpUnclamped;
                    }

                    float min = float.MaxValue;
                    float diff;
                    for (int i = 0; i < _specificSlotLerps.Count; i++)
                    {
                        diff = Mathf.Abs(_specificSlotLerps[i] - inverseLerp);
                        if (diff < min)
                        {
                            posIndex = i;
                            min = diff;
                        }
                    }
                }
                else
                {
                    posIndexLimit = Mathf.RoundToInt(inverseLerpUnclamped / _slotLerpFactor);
                }
                if (posIndexLimit < 0) posIndex -= posIndexLimit;
                else if (posIndexLimit >= NumberOfPicatinnySlots) posIndex -= (posIndexLimit - (NumberOfPicatinnySlots - 1));

                posIndex = Mathf.Clamp(posIndex, 0, NumberOfPicatinnySlots - 1);
            }
            return posIndex;
        }
    }
}