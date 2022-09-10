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

        private float _curAngle;
        private float _deltaAngle;
        private Quaternion _curRot;
        private Quaternion _lastRot;
        private Quaternion _deltaRot;

        public void Awake()
        {
            _curRot = ObservedObject.transform.localRotation;
            _lastRot = ObservedObject.transform.localRotation;
        }
        public void Update()
        {
            float pos;
            if (!IsRotation) pos = Mathf.InverseLerp(Start, End, ObservedObject.transform.localPosition[(int)Direction]);
            else GetRotPos(out pos);
            Animator.Play(AnimationNodeName, -1, pos);
        }

        private void GetRotPos(out float pos)
        {
            _curRot = ObservedObject.transform.localRotation;
            _deltaRot = _curRot.Subtract(_lastRot);
            Vector3 axis;
            _deltaRot.ToAngleAxis(out _deltaAngle, out axis);

            _curAngle += _deltaAngle * axis.GetAxisValue(Direction);
            _lastRot = _curRot;

            pos = Mathf.InverseLerp(Start, End, _curAngle);
        }
    }
}
