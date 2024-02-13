using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class ManipulateBlendShape : OpenScripts2_BasePlugin
    {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public int BlendShapeIndex = 0;

		public Transform ObservedObject;

        public Vector3 ObservedObject_Start;
        public Vector3 ObservedObject_End;

        public Axis Direction;

        private float _lastLerp;
        public void Awake()
        {
        }
        public void Update()
        {
            float lerp = Mathf.InverseLerp(ObservedObject_Start.GetAxisValue(Direction), ObservedObject_End.GetAxisValue(Direction), ObservedObject.GetLocalPositionAxisValue(Direction));
            if (!Mathf.Approximately(lerp, _lastLerp)) skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeIndex, lerp);

            _lastLerp = lerp;
        }
    }
}
