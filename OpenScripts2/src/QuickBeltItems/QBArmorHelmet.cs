using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class QBArmorHelmet : QBArmorPiece
    {
		[Header("QBArmorHelmet Config")]
		public Transform[] HelmetRootPieces;
        public Vector3 HeadOffsetPosition;

        [HideInInspector]
        public bool IsInHelmetSlot = false; 

		private readonly List<TransformProxy> _originalHelmetPiecePositions = new();

        public override void Awake()
        {
            base.Awake();

            foreach (var piece in HelmetRootPieces)
            {
                _originalHelmetPiecePositions.Add(new TransformProxy(piece));
            }
        }

        public override void SetQuickBeltSlot(FVRQuickBeltSlot slot)
		{
			base.SetQuickBeltSlot(slot);

			if (slot != null && slot is HeadQBSlot)
			{
                IsInHelmetSlot = true;
                foreach (Transform piece in HelmetRootPieces)
                {
                    piece.SetParent(GM.CurrentPlayerBody.Head, false);
                    piece.localPosition += HeadOffsetPosition;
                }
            }
            else if (slot == null && IsInHelmetSlot)
			{
                IsInHelmetSlot = false;
                for (int i = 0; i < HelmetRootPieces.Length; i++)
                {
                    HelmetRootPieces[i].SetParent(transform, false);
                    HelmetRootPieces[i].localPosition = _originalHelmetPiecePositions[i].localPosition;
                    HelmetRootPieces[i].localRotation = _originalHelmetPiecePositions[i].localRotation;
                }
            }
		}
	}
}