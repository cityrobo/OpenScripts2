using UnityEngine;
using FistVR;
using System.Collections;

namespace OpenScripts2
{
	public class PreattachedAttachment : OpenScripts2_BasePlugin
	{
		public FVRFireArmAttachment Attachment;
		public FVRFireArmAttachmentMount AttachmentMount;

		[HideInInspector]
		public bool AttachmentDone = false;
		public void Start()
		{
			StartCoroutine("AttachToMount");
		}

		public IEnumerator AttachToMount()
		{
			yield return null;
			Attachment.AttachToMount(AttachmentMount, false);
            if (Attachment is Suppressor suppressor)
            {
				suppressor.AutoMountWell();
            }

			AttachmentDone = true;
		}
	}
}