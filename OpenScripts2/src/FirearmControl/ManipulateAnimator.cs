using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class ManipulateAnimator : OpenScripts2_BasePlugin
    {
        public Animator animator;
		public GameObject ObservedObject;

        public float EndstopWiggleroom = 0.05f;

        public Vector3 Start;
        public Vector3 End;

        public Axis Direction;
        public bool IsRotation;


        public void Awake()
        {
        }
        public void Update()
        {
            float pos;
            if (!IsRotation) pos = Mathf.InverseLerp(Start[(int)Direction], End[(int)Direction], ObservedObject.transform.localPosition[(int)Direction]);
            else pos = Mathf.InverseLerp(Start[(int)Direction], End[(int)Direction], ObservedObject.transform.localEulerAngles[(int)Direction]);
            animator.Play("animation", 0, pos);
        }
    }
}
