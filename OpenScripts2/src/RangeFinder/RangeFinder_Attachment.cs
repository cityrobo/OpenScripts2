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
                if (TouchpadDirPressed(hand, Vector2.left)) RotateScreenLeft();
                else if (TouchpadDirPressed(hand, Vector2.right)) RotateScreenRight();
            }
        }

        public void RotateScreenLeft()
        {
            switch (RangefinderRaycast.CurrentScreen)
            {
                case RangeFinder_Raycast.ECurrentScreen.Up:
                    RangefinderRaycast.CurrentScreen = RangeFinder_Raycast.ECurrentScreen.Left;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ECurrentScreen.Left:
                    RangefinderRaycast.CurrentScreen = RangeFinder_Raycast.ECurrentScreen.Down;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ECurrentScreen.Down:
                    RangefinderRaycast.CurrentScreen = RangeFinder_Raycast.ECurrentScreen.Right;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ECurrentScreen.Right:
                    RangefinderRaycast.CurrentScreen = RangeFinder_Raycast.ECurrentScreen.Up;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
            }
        }

        public void RotateScreenRight()
        {
            switch (RangefinderRaycast.CurrentScreen)
            {
                case RangeFinder_Raycast.ECurrentScreen.Up:
                    RangefinderRaycast.CurrentScreen = RangeFinder_Raycast.ECurrentScreen.Right;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ECurrentScreen.Left:
                    RangefinderRaycast.CurrentScreen = RangeFinder_Raycast.ECurrentScreen.Up;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ECurrentScreen.Down:
                    RangefinderRaycast.CurrentScreen = RangeFinder_Raycast.ECurrentScreen.Left;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
                case RangeFinder_Raycast.ECurrentScreen.Right:
                    RangefinderRaycast.CurrentScreen = RangeFinder_Raycast.ECurrentScreen.Down;
                    RangefinderRaycast.ChangeActiveScreen();
                    break;
            }
        }
    }
}
