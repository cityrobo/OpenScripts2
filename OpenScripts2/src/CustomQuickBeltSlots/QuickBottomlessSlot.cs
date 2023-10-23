using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace OpenScripts2
{
    public class QuickBottomlessSlot : FVRQuickBeltSlot
    {
        [Header("QuickBottomlessSlot settings")]
        public int MaxItems = 30;
        public float TimeBetweenMagSwitch = 0.25f;
        public Color HoverColor;
        public bool StoresMagazines = true;
        public bool StoresClips = true;
        public bool StoresSpeedloaders = true;
        public bool OnlyStoresEmpty = false;

        public AudioEvent InsertSounds;
        public AudioEvent ExtractSounds;
        public AudioEvent FailureSounds;

        public GameObject TextRoot;
        public string TextPrefix = "Items: ";
        public Text NumberOfItemsDisplay;
        public bool TextTurnsOffOnNoItemsStored = true;

        [ContextMenu("CopyQBSlot")]
        public void CopyQBSlot()
        {
            FVRQuickBeltSlot QBS = GetComponent<FVRQuickBeltSlot>();

            this.CopyComponent(QBS);
        }

        private GameObject _currentSelectedObject;

        private readonly List<GameObject> _storedGameObjects = new();

        private bool _switchingObject = false;

        private int _selectedObjectIndex = 0;

        private float _timeWaited = 0f;
#if !DEBUG
        static QuickBottomlessSlot()
        {
            On.FistVR.FVRQuickBeltSlot.Update += FVRQuickBeltSlot_Update;
            On.FistVR.FVRQuickBeltSlot.MoveContents += FVRQuickBeltSlot_MoveContents;
            On.FistVR.FVRQuickBeltSlot.MoveContentsInstant += FVRQuickBeltSlot_MoveContentsInstant;
            On.FistVR.FVRQuickBeltSlot.MoveContentsCheap += FVRQuickBeltSlot_MoveContentsCheap;
        }

        #region QuickBeltMovement patches
        private static void FVRQuickBeltSlot_MoveContentsCheap(On.FistVR.FVRQuickBeltSlot.orig_MoveContentsCheap orig, FVRQuickBeltSlot self, Vector3 dir)
        {
            if (self is QuickBottomlessSlot quickBottomlessSlot)
            {
                if (quickBottomlessSlot._currentSelectedObject != null)
                {
                    FVRPhysicalObject mag = quickBottomlessSlot._currentSelectedObject.GetComponent<FVRPhysicalObject>();
                    if (mag.IsHeld)
                    {
                        return;
                    }
                    mag.RootRigidbody.position = mag.RootRigidbody.position + dir;
                    mag.RootRigidbody.velocity = Vector3.zero;
                }
            }
            else orig(self, dir);
        }

        private static void FVRQuickBeltSlot_MoveContentsInstant(On.FistVR.FVRQuickBeltSlot.orig_MoveContentsInstant orig, FVRQuickBeltSlot self, Vector3 dir)
        {
            if (self is QuickBottomlessSlot quickBottomlessSlot)
            {
                if (quickBottomlessSlot._currentSelectedObject != null)
                {
                    FVRPhysicalObject mag = quickBottomlessSlot._currentSelectedObject.GetComponent<FVRPhysicalObject>();
                    if (mag.IsHeld)
                    {
                        return;
                    }
                    mag.transform.position = mag.transform.position + dir;
                    mag.RootRigidbody.velocity = Vector3.zero;
                }
            }
            else orig(self, dir);
        }

        private static void FVRQuickBeltSlot_MoveContents(On.FistVR.FVRQuickBeltSlot.orig_MoveContents orig, FVRQuickBeltSlot self, Vector3 dir)
        {
            if (self is QuickBottomlessSlot quickBottomlessSlot)
            {
                if (quickBottomlessSlot._currentSelectedObject != null)
                {
                    FVRPhysicalObject mag = quickBottomlessSlot._currentSelectedObject.GetComponent<FVRPhysicalObject>();
                    if (mag.IsHeld)
                    {
                        return;
                    }
                    mag.transform.position = mag.transform.position + dir;
                    mag.RootRigidbody.velocity = Vector3.zero;
                }
            }
            else orig(self, dir);
        }
        #endregion

        private static void FVRQuickBeltSlot_Update(On.FistVR.FVRQuickBeltSlot.orig_Update orig, FVRQuickBeltSlot self)
        {
            if (self is QuickBottomlessSlot quickBottomlessSlot)
            {
                if (!GM.CurrentSceneSettings.IsSpawnLockingEnabled && quickBottomlessSlot.HeldObject != null && (quickBottomlessSlot.HeldObject as FVRPhysicalObject).m_isSpawnLock)
                {
                    (quickBottomlessSlot.HeldObject as FVRPhysicalObject).m_isSpawnLock = false;
                }
                // Color Changing
                if (quickBottomlessSlot.HeldObject != null)
                {
                    if ((quickBottomlessSlot.HeldObject as FVRPhysicalObject).m_isSpawnLock)
                    {
                        if (!quickBottomlessSlot.HoverGeo.activeSelf)
                        {
                            quickBottomlessSlot.HoverGeo.SetActive(true);
                        }
                        quickBottomlessSlot.m_hoverGeoRend.material.SetColor("_RimColor", new Color(0.3f, 0.3f, 1f, 1f));
                    }
                    else if ((quickBottomlessSlot.HeldObject as FVRPhysicalObject).m_isHardnessed)
                    {
                        if (!quickBottomlessSlot.HoverGeo.activeSelf)
                        {
                            quickBottomlessSlot.HoverGeo.SetActive(true);
                        }
                        quickBottomlessSlot.m_hoverGeoRend.material.SetColor("_RimColor", new Color(0.3f, 1f, 0.3f, 1f));
                    }
                    else
                    {
                        if (quickBottomlessSlot.HoverGeo.activeSelf != quickBottomlessSlot.IsHovered)
                        {
                            quickBottomlessSlot.HoverGeo.SetActive(quickBottomlessSlot.IsHovered);
                        }
                        quickBottomlessSlot.m_hoverGeoRend.material.SetColor("_RimColor", quickBottomlessSlot.HoverColor);
                    }
                }
                else
                {
                    if (quickBottomlessSlot.HoverGeo.activeSelf != quickBottomlessSlot.IsHovered)
                    {
                        quickBottomlessSlot.HoverGeo.SetActive(quickBottomlessSlot.IsHovered);
                    }
                    quickBottomlessSlot.m_hoverGeoRend.material.SetColor("_RimColor", quickBottomlessSlot.HoverColor);
                }

                if (quickBottomlessSlot.CurObject != null && quickBottomlessSlot._storedGameObjects.Count < quickBottomlessSlot.MaxItems)
                {
                    if (quickBottomlessSlot.StoresMagazines && quickBottomlessSlot.CurObject is FVRFireArmMagazine)
                    {
                        quickBottomlessSlot.StoreCurObject();
                    }
                    else if (quickBottomlessSlot.StoresClips && quickBottomlessSlot.CurObject is FVRFireArmClip)
                    {
                        quickBottomlessSlot.StoreCurObject();
                    }
                    else if (quickBottomlessSlot.StoresSpeedloaders && quickBottomlessSlot.CurObject is Speedloader)
                    {
                        quickBottomlessSlot.StoreCurObject();
                    }
                }
                else if (quickBottomlessSlot.CurObject != null)
                {
                    quickBottomlessSlot.EjectCurObject();
                }

                if (quickBottomlessSlot._storedGameObjects.Count > 0)
                {
                    quickBottomlessSlot._timeWaited += Time.deltaTime;
                    if (!quickBottomlessSlot._switchingObject)
                    {
                        quickBottomlessSlot._timeWaited = 0f;
                        quickBottomlessSlot.SelectObject();
                    }
                    else if (quickBottomlessSlot._timeWaited > quickBottomlessSlot.TimeBetweenMagSwitch)
                    {
                        quickBottomlessSlot._switchingObject = false;
                    }

                    int removalIndex = -1;

                    for (int i = 0; i < quickBottomlessSlot._storedGameObjects.Count; i++)
                    {
                        quickBottomlessSlot._storedGameObjects[i].SetActive(i == quickBottomlessSlot._selectedObjectIndex);

                        if (i != quickBottomlessSlot._selectedObjectIndex)
                        {
                            if (quickBottomlessSlot.QuickbeltRoot != null)
                            {
                                quickBottomlessSlot._storedGameObjects[i].transform.position = quickBottomlessSlot.QuickbeltRoot.position;
                                quickBottomlessSlot._storedGameObjects[i].transform.rotation = quickBottomlessSlot.QuickbeltRoot.rotation;
                            }
                            else if (quickBottomlessSlot.PoseOverride != null)
                            {
                                quickBottomlessSlot._storedGameObjects[i].transform.position = quickBottomlessSlot.PoseOverride.position;
                                quickBottomlessSlot._storedGameObjects[i].transform.rotation = quickBottomlessSlot.PoseOverride.rotation;
                            }
                            else
                            {
                                quickBottomlessSlot._storedGameObjects[i].transform.position = quickBottomlessSlot.transform.position;
                                quickBottomlessSlot._storedGameObjects[i].transform.rotation = quickBottomlessSlot.transform.rotation;
                            }
                        }

                        FVRPhysicalObject objectComponent = quickBottomlessSlot._storedGameObjects[i].GetComponent<FVRPhysicalObject>();
                        if (objectComponent.IsHeld)
                        {
                            removalIndex = i;
                        }
                        if (objectComponent.m_isSpawnLock)
                        {
                            objectComponent.m_isSpawnLock = false;
                        }
                    }

                    if (removalIndex >= 0)
                    {
                        quickBottomlessSlot._storedGameObjects[removalIndex].SetActive(true);
                        quickBottomlessSlot._storedGameObjects.RemoveAt(removalIndex);
                        SM.PlayGenericSound(quickBottomlessSlot.ExtractSounds, quickBottomlessSlot.transform.position);
                        quickBottomlessSlot._switchingObject = false;
                    }

                    if (quickBottomlessSlot.TextRoot != null && quickBottomlessSlot.NumberOfItemsDisplay != null)
                    {
                        if (quickBottomlessSlot.TextTurnsOffOnNoItemsStored) quickBottomlessSlot.TextRoot.SetActive(true);
                        quickBottomlessSlot.NumberOfItemsDisplay.text = quickBottomlessSlot.TextPrefix + quickBottomlessSlot._storedGameObjects.Count.ToString();
                    }
                }
                else if (quickBottomlessSlot.TextRoot != null && quickBottomlessSlot.NumberOfItemsDisplay != null)
                {
                    if (quickBottomlessSlot.TextTurnsOffOnNoItemsStored) quickBottomlessSlot.TextRoot.SetActive(false);
                    quickBottomlessSlot.NumberOfItemsDisplay.text = quickBottomlessSlot.TextPrefix + quickBottomlessSlot._storedGameObjects.Count.ToString();
                }
            }
            else orig(self);
        }

        private void StoreCurObject()
        {
            if (!_storedGameObjects.Contains(CurObject.gameObject))
            {
                if (OnlyStoresEmpty)
                {
                    if (!CheckEmpty()) return;
                }
                _storedGameObjects.Add(CurObject.gameObject);
                CurObject.gameObject.SetActive(false);

                CurObject = null;
                HeldObject = null;

                SM.PlayGenericSound(InsertSounds, transform.position);
            }
        }

        private bool EjectCurObject()
        {
            CurObject.SetQuickBeltSlot(null);
            CurObject = null;
            HeldObject = null;
            SM.PlayGenericSound(FailureSounds, transform.position);
            return false;
        }

        private bool CheckEmpty()
        {
            FVRFireArmMagazine mag = CurObject.gameObject.GetComponent<FVRFireArmMagazine>();
            FVRFireArmClip clip = CurObject.gameObject.GetComponent<FVRFireArmClip>();
            Speedloader speedloader = CurObject.gameObject.GetComponent<Speedloader>();

            if (mag != null && mag.m_numRounds > 0) return EjectCurObject();
            if (clip != null && clip.m_numRounds > 0) return EjectCurObject();
            if (speedloader != null)
            {
                bool notEmpty = false;
                foreach (var chamber in speedloader.Chambers)
                {
                    notEmpty = chamber.IsLoaded;
                }
                if (notEmpty) return EjectCurObject();
            }
            return true;
        }

        private Vector3 GetPointInside()
        {
            switch (Shape)
            {
                case QuickbeltSlotShape.Sphere:
                    return UnityEngine.Random.insideUnitSphere * HoverGeo.transform.localScale.x;
                case QuickbeltSlotShape.Rectalinear:
                    return new Vector3(UnityEngine.Random.Range(-RectBounds.localScale.x, RectBounds.localScale.x), UnityEngine.Random.Range(-RectBounds.localScale.y, RectBounds.localScale.y), UnityEngine.Random.Range(-RectBounds.localScale.z, RectBounds.localScale.z)); ;
                default:
                    return new Vector3();
            }
        }
        private void SelectObject()
        {
            _switchingObject = true;
            _selectedObjectIndex = UnityEngine.Random.Range(0, _storedGameObjects.Count);
            _currentSelectedObject = _storedGameObjects[_selectedObjectIndex];
            _currentSelectedObject.SetActive(true);
        }
#endif
    }
}
