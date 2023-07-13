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
        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            UpdateInputsAndAnimate(hand);
        }

        private void UpdateInputsAndAnimate(FVRViveHand hand)
        {
            if (hand != null)
            {
                if (OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.left)) FlipLeft();
                else if (OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.right)) FlipRight();
                else if (OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.up)) CloseBooklet();
            }
        }

        private void FlipLeft()
        {
            if (_isClosing || _currentPage >= Pages.Length) return;
            SM.PlayGenericSound(FlipPageLeftSounds, transform.position);
            if (_isFlipping)
            {
                StopAllCoroutines();
                Pages[_currentPage - 1].transform.localRotation = Quaternion.Euler(GetRotationalVector(FlipStopAngle));
            }
            StartCoroutine(FlipPage(Pages[_currentPage], FlipStopAngle));
            _currentPage++;
        }

        private void FlipRight()
        {
            if (_isClosing || _currentPage <= 0) return;
            SM.PlayGenericSound(FlipPageRightSounds, transform.position);
            if (_isFlipping)
            {
                StopAllCoroutines();
                Pages[_currentPage].transform.localRotation = Quaternion.Euler(GetRotationalVector(FlipStartAngle));
            }
            StartCoroutine(FlipPage(Pages[_currentPage - 1], FlipStartAngle));
            _currentPage--;
        }
        private void CloseBooklet()
        {
            if (_isClosing) return;
            SM.PlayGenericSound(CloseBookletSounds, transform.position);

            if (_isFlipping)
            {
                StopAllCoroutines();
            }

            StartCoroutine(ClosingBooklet());

            _currentPage = 0;
        }

        private IEnumerator FlipPage(GameObject page, float angle)
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

        private IEnumerator ClosingBooklet()
        {
            _isClosing = true;
            for (int i = 0; i < Pages.Length; i++)
            {
                StartCoroutine(FlipPage(Pages[i], FlipStartAngle));
                yield return null;
            }
            _isClosing = false;
        }

        private Vector3 GetRotationalVector(float angle)
        {
            switch (PageAxis)
            {
                case Axis.X:
                    return new Vector3(angle, 0f, 0f);
                case Axis.Y:
                    return new Vector3(0f, angle, 0f);
                case Axis.Z:
                    return new Vector3(0f, 0f, angle);
            }
            return new Vector3();
        }
    }
}
