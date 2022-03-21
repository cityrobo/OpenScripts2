using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class ManipulateBlendShape : OpenScripts2_BasePlugin
    {
        public SkinnedMeshRenderer SkinnedMeshRenderer;
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
            float lerp;
            lerp = Mathf.InverseLerp(ObservedObject_Start[(int)Direction], ObservedObject_End[(int)Direction], ObservedObject.localPosition[(int)Direction]);
            if (!Mathf.Approximately(lerp, _lastLerp)) SkinnedMeshRenderer.SetBlendShapeWeight(BlendShapeIndex, lerp);

            _lastLerp = lerp;
        }
    }
}
