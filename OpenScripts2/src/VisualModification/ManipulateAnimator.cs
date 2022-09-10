using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class ManipulateAnimator : OpenScripts2_BasePlugin
    {
        public Animator Animator;
		public GameObject ObservedObject;

        public string AnimationNodeName = "animation";

        public float Start;
        public float End;

        public Axis Direction;
        public bool IsRotation;

        public void Awake()
        {
        }
        public void Update()
        {
            float pos;
            if (!IsRotation) pos = Mathf.InverseLerp(Start, End, ObservedObject.transform.localPosition[(int)Direction]);
            else pos = Mathf.InverseLerp(Start, End, ObservedObject.transform.localEulerAngles[(int)Direction]);
            Animator.Play(AnimationNodeName, 0, pos);
        }
    }
}
