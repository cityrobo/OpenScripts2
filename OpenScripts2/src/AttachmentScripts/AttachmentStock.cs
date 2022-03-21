using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class AttachmentStock : FVRFireArmAttachment
    {
		[Header("VirtualStock config")]
		public bool HasActiveShoulderStock = true;
		public Transform StockPos;

		public override bool HasStockPos()
		{
			return this.HasActiveShoulderStock;
		}

		public override Transform GetStockPos()
		{
			return this.StockPos;
		}
	}
}
