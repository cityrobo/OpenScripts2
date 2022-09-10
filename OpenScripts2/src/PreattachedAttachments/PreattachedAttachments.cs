using UnityEngine;
using FistVR;
using System.Collections;

namespace OpenScripts2
{
	public class PreattachedAttachments : OpenScripts2_BasePlugin
	{
		public FVRFireArmAttachment[] Attachments;
		public FVRFireArmAttachmentMount AttachmentMount;

#if !DEBUG
		public void Start()
		{
			StartCoroutine("AttachAllToMount");
		}

		public IEnumerator AttachAllToMount()
        {
			yield return null;
			foreach (var attachment in Attachments)
            {
				attachment.AttachToMount(AttachmentMount, false);
				if (attachment is Suppressor suppressor)
				{
					suppressor.AutoMountWell();
				}
				yield return null;
			}
        }
#endif
	}
}
