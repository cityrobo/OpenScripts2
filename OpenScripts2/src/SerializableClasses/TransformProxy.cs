﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    [Serializable]
    public class TransformProxy
    {
        public readonly Transform parent;
        public readonly Vector3 localPosition;
        public readonly Quaternion localRotation;
        public readonly Vector3 localScale;

        public TransformProxy(Transform transform, bool deleteReferenceGameObject = false)
        {
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;

            parent = transform.parent;

            if (deleteReferenceGameObject) UnityEngine.Object.Destroy(transform.gameObject);
        }

        public TransformProxy(Transform transform, Transform parent, bool deleteReferenceGameObject = false)
        {
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;

            this.parent = parent;

            if (deleteReferenceGameObject) UnityEngine.Object.Destroy(transform.gameObject);
        }

        public Vector3 position => parent.TransformPoint(localPosition);
        public Quaternion rotation => parent.TransformRotation(localRotation);

        public Vector3 GetGlobalPositionRelativeToParent(Transform parent) => parent.TransformPoint(localPosition);

        public Quaternion GetGlobalRotationRelativeToParent(Transform parent) => parent.TransformRotation(localRotation);
    }
}
