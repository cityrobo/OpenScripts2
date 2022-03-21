using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class MeatBeatScanner : FVRFireArmAttachment
    {
        [Header("MeatBeat Scanner Config")]
        public float ScanningRange;
		public float EngageAngle;
        public RectTransform CurrentScreen;
        public GameObject PingDotReference;

		public LayerMask LatchingMask;

        public bool CanRotateScreen = false;
        [Tooltip("Different Screen orientations")]
        public GameObject[] Images;
		//public LayerMask BlockingMask;

        private Dictionary<SosigLink,GameObject> Pings;
        private GameObject DirectionReference;

        private int _currentImage;

#if !(UNITY_EDITOR || UNITY_5)

        public override void Awake()
        {
            base.Awake();

            PingDotReference.SetActive(false);
            Pings = new Dictionary<SosigLink, GameObject>();

            DirectionReference = new GameObject("MeatBeatScanner DirectionReference");

            DirectionReference.transform.parent = transform;
            DirectionReference.transform.localPosition = new Vector3();

            if (CanRotateScreen) CurrentScreen = Images[0].GetComponent<RectTransform>();
        }

        public override void Start()
        {
            base.Start();
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (CanRotateScreen && curMount != null && AttachmentInterface.m_hand != null) UpdateInputs(AttachmentInterface.m_hand);

            UpdateScreen();
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);

            if(CanRotateScreen) UpdateInputs(hand);
        }

        public void UpdateScreen()
        {
			List<SosigLink> sosigs = FindSosigs();
            ClearPings(sosigs);
            foreach (var sosig in sosigs)
            {
                Vector3 sosigPos = sosig.transform.position;
                Vector3 correctedForwardDir = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

                DirectionReference.transform.rotation = Quaternion.LookRotation(correctedForwardDir, Vector3.up);

                Vector3 projectedPos = DirectionReference.transform.InverseTransformPoint(sosigPos);
                float distance = projectedPos.magnitude;
                GameObject pingObject;
                if (!Pings.TryGetValue(sosig, out pingObject))
                {
                    pingObject = Instantiate(PingDotReference);
                    pingObject.transform.SetParent(CurrentScreen);

                    Pings.Add(sosig, pingObject);
                }

                Vector3 screenTransform = new Vector3(0f, 0f, -0.0001f);

                float xMax = CurrentScreen.rect.width / 2;
                float yMax = CurrentScreen.rect.height;
                float max = Mathf.Max(xMax, yMax);
                screenTransform.x = (projectedPos.x / ScanningRange) * max;
                screenTransform.y = (projectedPos.z / ScanningRange) * max;

                if (Mathf.Abs(screenTransform.x) > xMax || Mathf.Abs(screenTransform.y) > yMax)
                {
                    pingObject.SetActive(false);
                }
                else pingObject.SetActive(true);

                pingObject.transform.localPosition = screenTransform;
                pingObject.transform.localRotation = PingDotReference.transform.localRotation;
                pingObject.transform.localScale = PingDotReference.transform.localScale;
            }
        }

        List<SosigLink> FindSosigs()
        {
            List<SosigLink> sosigs = new List<SosigLink>();
            
            Collider[] array = Physics.OverlapSphere(transform.position, ScanningRange, LatchingMask);
			List<Rigidbody> list = new List<Rigidbody>();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].attachedRigidbody != null && !list.Contains(array[i].attachedRigidbody))
				{
					list.Add(array[i].attachedRigidbody);
				}
			}
			SosigLink sosigLink = null;
			float maxAngle = EngageAngle / 2;
			for (int j = 0; j < list.Count; j++)
			{
                SosigLink component = list[j].GetComponent<SosigLink>();
                
                if (!(component == null))
				{
					if (component.S.BodyState != Sosig.SosigBodyState.Dead)
					{
                        Vector3 toTarget = component.transform.position - transform.position;
                        float angle;
                        try
                        {
                            angle = Vector3.Angle(new Vector3(transform.forward.x, 0, transform.forward.z), new Vector3(toTarget.x, 0, toTarget.z));
                        }
                        catch (Exception)
                        {
                            angle = 360;
                        }

                        //Debug.Log("angle: " + angle);
						Sosig s = component.S;
						sosigLink = s.Links[0];

                        if (angle < maxAngle)
                        {
                            if (!sosigs.Contains(sosigLink)) sosigs.Add(sosigLink);
                        }
					}
				}
			}
			return sosigs;
		}

        void ClearPings(List<SosigLink> sosigs)
        {
            if (Pings == null) Pings = new Dictionary<SosigLink, GameObject>();
            if (sosigs.Count > 0)
            {
                for (int i = 0; i < Pings.Count; i++)
                {
                    SosigLink toRemove = Pings.ElementAt(i).Key;
                    if (!sosigs.Contains(toRemove))
                    {
                        GameObject toRemoveValue;
                        Pings.TryGetValue(toRemove, out toRemoveValue);
                        Destroy(toRemoveValue);
                        Pings.Remove(toRemove);
                    }
                }
            }
            else
            {
                foreach (var ping in Pings)
                {
                    Destroy(ping.Value);
                }
                Pings.Clear();
            }
        }

        void UpdateInputs(FVRViveHand hand)
        {
            if (!hand.IsInStreamlinedMode)
            {
                if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) NextRotation();
                else if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) PreviousRotation();
            }
            else
            {
                if (hand.Input.BYButtonDown) NextRotation();
            }
        }

        void NextRotation()
        {
            _currentImage++;
            if (_currentImage >= Images.Length) _currentImage = 0;

            for (int i = 0; i < Images.Length; i++)
            {
                Images[i].SetActive(i == (int)_currentImage);
            }

            CurrentScreen = Images[(int)_currentImage].GetComponent<RectTransform>();

            foreach (var ping in Pings)
            {
                Vector3 pos = ping.Value.transform.localPosition;
                ping.Value.transform.SetParent(CurrentScreen);
                ping.Value.transform.localPosition = pos;
            }
        }

        void PreviousRotation()
        {
            _currentImage--;
            if (_currentImage < 0) _currentImage = Images.Length - 1;

            for (int i = 0; i < Images.Length; i++)
            {
                Images[i].SetActive(i == (int)_currentImage);
            }

            CurrentScreen = Images[(int)_currentImage].GetComponent<RectTransform>();

            foreach (var ping in Pings)
            {
                Vector3 pos = ping.Value.transform.localPosition;
                ping.Value.transform.SetParent(CurrentScreen);
                ping.Value.transform.localPosition = pos;
            }
        }
#endif
    }
}
