using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class HeadQBSlot : FVRQuickBeltSlot
    {
        [Header("HeadQBSlot Config")]
		public Vector3 HeadOffsetPosition;
        public Vector3 HeadOffsetRotation;

        [ContextMenu("CopyQBSlot")]
        public void CopyQBSlot()
        {
            this.CopyComponent(GetComponent<FVRQuickBeltSlot>());
        }

        //private FVRPhysicalObject _lastObject;
        //private bool _didFollowHead = false;
        //private GameObject _geo;
        //private Transform[] _children;

		public void Start()
        {
            if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.Head != null)
            {
                transform.SetParent(GM.CurrentPlayerBody.Head);
            }

            transform.localPosition = HeadOffsetPosition;
            transform.localRotation = Quaternion.Euler(HeadOffsetRotation);
        }

        //public void LateUpdate()
        //{
        //    if (CurObject != null && CurObject is not QBArmorHelmet && _lastObject == null)
        //    {
        //        _lastObject = CurObject;
        //        Transform geoObj = CurObject.transform.Find("Geo");
        //        if (geoObj == null)
        //        {
        //            _geo = new GameObject("Geo");
        //            _geo.transform.SetParent(CurObject.transform);
        //            _geo.transform.localPosition = Vector3.zero;
        //            _geo.transform.localRotation = Quaternion.identity;
        //        }
        //        else _geo = geoObj.gameObject;

        //        _children = CurObject.GetComponentsInDirectChildren<Transform>().Where(t => t.gameObject != _geo).ToArray();
                
        //        foreach (Transform child in _children)
        //        {
        //            child.SetParent(_geo.transform);
        //        }

        //        _geo.transform.SetParent(GM.CurrentPlayerBody.Head);
        //        _geo.transform.localPosition = HeadOffsetPosition;
        //        _geo.transform.localRotation = Quaternion.Euler(HeadOffsetRotation);

        //        foreach (Transform child in _children)
        //        {
        //            child.SetParent(_geo.transform.parent);
        //        }

        //        if (CurObject.DoesQuickbeltSlotFollowHead)
        //        {
        //            CurObject.DoesQuickbeltSlotFollowHead = false;
        //            CurObject.DoesQuickbeltSlotFollowHead = false;
        //            _didFollowHead = true;
        //        }
        //    }
        //    else if (CurObject == null && CurObject is not QBArmorHelmet && _lastObject != null)
        //    {
        //        foreach (Transform child in _children)
        //        {
        //            child.SetParent(_geo.transform);
        //        }

        //        _geo.transform.SetParent(_lastObject.transform);
        //        _geo.transform.localPosition = Vector3.zero;
        //        _geo.transform.localRotation = Quaternion.identity;

        //        foreach (Transform child in _children)
        //        {
        //            child.SetParent(_geo.transform.parent);
        //        }

        //        _geo = null;
        //        if (_didFollowHead)
        //        {
        //            _lastObject.DoesQuickbeltSlotFollowHead = true;
        //            _lastObject = null;
        //            _didFollowHead = false;
        //        }
        //    }
        //}
	}
}