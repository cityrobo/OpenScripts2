using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace OpenScripts2
{
    public class SkinChanger : OpenScripts2_BasePlugin
    {
        public FVRInteractiveObject InteractiveObject;
        [Tooltip("Since any object can contain more than one renderer, you can define these here individually")]
        public SkinPart[] SkinParts;

        [Header("Skin Name Text Screen (optional)")]
        public string[] SkinNames;
        public Text SkinText;

        public int CurrentSkinIndex;

        private int _numberOfSkins;

        public const string SKINFLAGDICKEY = "SkinChanger Skin";

        [Serializable]
        public class SkinPart
        {
            public MeshRenderer Renderer;
            [Tooltip("A skin is one or multiple materials per mesh. Make sure the order is the same as on the mesh! the first skin is the default skin.")]
            public Skin[] Skins;
        }

        [Serializable]
        public class Skin
        {
            public Material[] Materials;
        }

        public void Awake()
        {
            if (SkinText != null) SkinText.text = SkinNames[CurrentSkinIndex];
            _numberOfSkins = SkinParts[0].Skins.Length;
        }

        public void Update()
        {
            FVRViveHand hand = InteractiveObject.m_hand;
            if (hand != null) 
            {
                if (TouchpadDirDown(hand, Vector2.left))
                {
                    NextSkin();
                    if (SkinText != null) SkinText.text = SkinNames[CurrentSkinIndex];
                }
                else if (TouchpadDirDown(hand, Vector2.right))
                {
                    PreviousSkin();
                    if (SkinText != null) SkinText.text = SkinNames[CurrentSkinIndex];
                }
            }
        }

        public void NextSkin()
        {
            CurrentSkinIndex++;
            if(CurrentSkinIndex >= _numberOfSkins) CurrentSkinIndex = 0;

            foreach (var skinPart in SkinParts)
            {
                skinPart.Renderer.materials = skinPart.Skins[CurrentSkinIndex].Materials;
            }
        }

        public void PreviousSkin()
        {
            CurrentSkinIndex--;
            if (CurrentSkinIndex < 0) CurrentSkinIndex = _numberOfSkins - 1;

            foreach (var skinPart in SkinParts)
            {
                skinPart.Renderer.materials = skinPart.Skins[CurrentSkinIndex].Materials;
            }
        }

        public void SelectSkin(int skinIndex)
        {
            CurrentSkinIndex = skinIndex;
            foreach (var skinPart in SkinParts)
            {
                skinPart.Renderer.materials = skinPart.Skins[CurrentSkinIndex].Materials;
            }
            if (SkinText != null) SkinText.text = SkinNames[CurrentSkinIndex];
        }
    }
}
