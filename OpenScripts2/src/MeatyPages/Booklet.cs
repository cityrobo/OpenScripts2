using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class Booklet : FVRPhysicalObject
    {
        [Header("Booklet Config")]
        public GameObject[] Pages;

        public float FlipStartAngle = 0f;
        public float FlipStopAngle = 180f;
        public float FlipSpeed = 180f;

        public Axis PageAxis;
        [Header("Booklet Sounds")]
        public AudioEvent FlipPageLeftSounds;
        public AudioEvent FlipPageRightSounds;
        public AudioEvent CloseBookletSounds;

        private int _currentPage = 0;
        private bool _isFlipping = false;
        private bool _isClosing = false;
#if !(UNITY_EDITOR || UNITY_5)
        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            UpdateInputsAndAnimate(hand);
        }

        void UpdateInputsAndAnimate(FVRViveHand hand)
        {
            if (hand != null)
            {
                if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) FlipLeft();
                else if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) FlipRight();
                else if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) < 45f) CloseBooklet();
            }
        }

        void FlipLeft()
        {
            if (_isClosing || _currentPage >= Pages.Length) return;
            SM.PlayGenericSound(FlipPageLeftSounds,this.transform.position);
            if (_isFlipping)
            {
                StopAllCoroutines();
                Pages[_currentPage - 1].transform.localRotation = Quaternion.Euler(GetRotationalVector(FlipStopAngle));
            }
            StartCoroutine(FlipPage(Pages[_currentPage], FlipStopAngle));
            _currentPage++;
        }
        void FlipRight()
        {
            if (_isClosing || _currentPage <= 0) return;
            SM.PlayGenericSound(FlipPageRightSounds, this.transform.position);
            if (_isFlipping)
            {
                StopAllCoroutines();
                Pages[_currentPage].transform.localRotation = Quaternion.Euler(GetRotationalVector(FlipStartAngle));
            }
            StartCoroutine(FlipPage(Pages[_currentPage - 1], FlipStartAngle));
            _currentPage--;
        }
        void CloseBooklet()
        {
            if (_isClosing) return;
            SM.PlayGenericSound(CloseBooklet, this.transform.position);

            if (_isFlipping)
            {
                StopAllCoroutines();
            }

            StartCoroutine(ClosingBooklet());

            _currentPage = 0;
        }

        IEnumerator FlipPage(GameObject page, float angle)
        {
            _isFlipping = true;
            Vector3 angleVector = GetRotationalVector(angle);
            Quaternion targetRotation = Quaternion.Euler(angleVector);

            while (page.transform.localRotation != targetRotation)
            {
                page.transform.localRotation = Quaternion.RotateTowards(page.transform.localRotation, targetRotation, FlipSpeed * Time.deltaTime);
                yield return null;
            }

            _isFlipping = false;
        }

        IEnumerator ClosingBooklet()
        {
            _isClosing = true;
            for (int i = 0; i < Pages.Length; i++)
            {
                StartCoroutine(FlipPage(Pages[i], FlipStartAngle));
                yield return null;
            }
            _isClosing = false;
        }

        Vector3 GetRotationalVector(float angle)
        {
            Vector3 angleVector;
            switch (PageAxis)
            {
                case Axis.X:
                    angleVector = new Vector3(angle, 0f, 0f);
                    break;
                case Axis.Y:
                    angleVector = new Vector3(0f, angle, 0f);
                    break;
                case Axis.Z:
                    angleVector = new Vector3(0f, 0f, angle);
                    break;
                default:
                    angleVector = new Vector3();
                    break;
            }

            return angleVector;
        }
#endif
    }
}
