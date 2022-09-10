using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
	public class RangeFinder_Raycast : OpenScripts2_BasePlugin
	{
		public Transform Direction;
		public LayerMask layerMask;
		public Text[] TextObjects;

		public enum ECurrentScreen
		{
			Up = 0,
			Left = 1,
			Down = 2,
			Right = 3
		}

		public ECurrentScreen CurrentScreen;

		public void Awake()
		{
			Direction = gameObject.transform;
			CurrentScreen = ECurrentScreen.Up;
		}

		public void ChangeActiveScreen()
		{
			for (int i = 0; i < 4; i++)
			{
				if (i == (int)CurrentScreen) TextObjects[i].gameObject.SetActive(true);
				else TextObjects[i].gameObject.SetActive(false);
			}
		}

		public void FixedUpdate()
		{
			RaycastHit hit;
			if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, layerMask))
			{
				//Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
				float distance = hit.distance;

				if (distance < 10f)
				{
					TextObjects[(int)CurrentScreen].text = string.Format("{0:F3} {1}", distance, "m");
				}
				else if (distance < 100f)
				{
					TextObjects[(int)CurrentScreen].text = string.Format("{0:F2} {1}", distance, "m");
				}
				else if (distance < 1000f)
				{
					TextObjects[(int)CurrentScreen].text = string.Format("{0:F1} {1}", distance, "m");
				}
				else TextObjects[(int)CurrentScreen].text = string.Format("{0:F0} {1}", distance, "m");

			}
			else
			{
				//Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
				TextObjects[(int)CurrentScreen].text = "inf";
			}
		}
	}
}
