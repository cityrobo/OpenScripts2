using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace OpenScripts2
{
    class RangeFinder_Attachment : OpenScripts2_BasePlugin
    {
        public FVRFireArmAttachmentInterface AttachmentInterface = null;
        public RangeFinder_Raycast RangefinderRaycast = null;

        public void Update()
        {
            FVRViveHand hand = AttachmentInterface.m_hand;
            if (hand != null)
            {
                if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f) RotateScreenLeft();
                else if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f) RotateScreenRight();
            }
        }

        public void RotateScreenLeft()
        {
            switch (RangefinderRaycast.chosenScreen)
            {
                case RangeFinder_Raycast.ChosenScreen.Up:
                    RangefinderRaycast.chosenScreen = RangeFinder_Raycast.ChosenScreen.Left;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ChosenScreen.Left:
                    RangefinderRaycast.chosenScreen = RangeFinder_Raycast.ChosenScreen.Down;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ChosenScreen.Down:
                    RangefinderRaycast.chosenScreen = RangeFinder_Raycast.ChosenScreen.Right;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ChosenScreen.Right:
                    RangefinderRaycast.chosenScreen = RangeFinder_Raycast.ChosenScreen.Up;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                default:
                    RangefinderRaycast.chosenScreen = RangeFinder_Raycast.ChosenScreen.Up;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
            }
        }

        public void RotateScreenRight()
        {
            switch (RangefinderRaycast.chosenScreen)
            {
                case RangeFinder_Raycast.ChosenScreen.Up:
                    RangefinderRaycast.chosenScreen = RangeFinder_Raycast.ChosenScreen.Right;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ChosenScreen.Left:
                    RangefinderRaycast.chosenScreen = RangeFinder_Raycast.ChosenScreen.Up;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ChosenScreen.Down:
                    RangefinderRaycast.chosenScreen = RangeFinder_Raycast.ChosenScreen.Left;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ChosenScreen.Right:
                    RangefinderRaycast.chosenScreen = RangeFinder_Raycast.ChosenScreen.Down;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                default:
                    RangefinderRaycast.chosenScreen = RangeFinder_Raycast.ChosenScreen.Up;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
            }
        }
    }
}
