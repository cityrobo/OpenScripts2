using System;
using UnityEngine;
using FistVR;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OpenScripts2
{
	public class PreattachedForeignAttachments : OpenScripts2_BasePlugin
	{
		[Serializable]
		public class ForeignAttachmentSet
		{
			public ForeignAttachmentSet(string primaryItemID, string backupID, Transform attachmentPoint)
			{
				PrimaryItemID = primaryItemID;
				BackupItemID = backupID;
				
				AttachmentPoint = attachmentPoint;
			}

			public string PrimaryItemID;
			[Tooltip("If your item fails to spawn, it will spawn the backup ID.")]
			public string BackupItemID;
            [Header("These points are getting destroyed at the end. Do NOT put anthing important on them.")]
            [Tooltip("Position and Rotation to spawn the Attachment at.")]
			public Transform AttachmentPoint;
		}

		public FVRFireArmAttachmentMount AttachmentMount;
		public ForeignAttachmentSet[] ForeignAttachmentSets;

		private readonly List<FVRFireArmAttachment> _spawnedAttachments = new();

#if !DEBUG
		public void Start()
		{
			SpawnAttachments();
			StartCoroutine(AttachAllToMount());
		}

		private IEnumerator AttachAllToMount()
		{
			yield return null;

			FVRPhysicalObject physicalObject = GetComponentInParent<FVRPhysicalObject>();
			PreattachedAttachment[] preattachedAttachments = physicalObject.GetComponentsInChildren<PreattachedAttachment>();
            PreattachedAttachments[] multiplePreattachedAttachments = physicalObject.GetComponentsInChildren<PreattachedAttachments>();

			bool attachmentsDone = false;
			bool multipleAttachmentsDone = false;

			if (preattachedAttachments.Length == 0) attachmentsDone = true;
			else
			{
				attachmentsDone = preattachedAttachments.All(a => a.AttachmentDone == true);
            }
			if (multiplePreattachedAttachments.Length == 0) multipleAttachmentsDone = true;
			else
			{
                multipleAttachmentsDone = multiplePreattachedAttachments.All(a => a.AttachmentsDone == true);
            }

			while (!attachmentsDone && !multipleAttachmentsDone)
			{
				yield return null;
                if (!attachmentsDone) 
                {
                    attachmentsDone = preattachedAttachments.All(a => a.AttachmentDone == true);
                }
                if (!multipleAttachmentsDone) 
                {
                    multipleAttachmentsDone = multiplePreattachedAttachments.All(a => a.AttachmentsDone == true);
                }
            }

            foreach (var spawnedAttachment in _spawnedAttachments)
			{
				spawnedAttachment.gameObject.SetActive(true);
                spawnedAttachment.AttachToMount(AttachmentMount, false);
				if (spawnedAttachment is Suppressor suppressor)
				{
					suppressor.AutoMountWell();
				}
				yield return null;
			}
		}

		private void SpawnAttachments()
		{
			GameObject spawnedGameObject;
			FVRFireArmAttachment spawnedAttachment;
			FVRObject objectReference;
			foreach (var foreignAttachmentSet in ForeignAttachmentSets)
			{
				spawnedGameObject = null;
				spawnedAttachment = null;
				objectReference = null;
				try
				{
					objectReference = IM.OD[foreignAttachmentSet.PrimaryItemID];
					spawnedGameObject = Instantiate(objectReference.GetGameObject(), foreignAttachmentSet.AttachmentPoint.position, foreignAttachmentSet.AttachmentPoint.rotation);
					spawnedAttachment = spawnedGameObject.GetComponent<FVRFireArmAttachment>();
					_spawnedAttachments.Add(spawnedAttachment);
					spawnedGameObject.SetActive(false);
                }
				catch
				{
					Log($"Item ID {foreignAttachmentSet.PrimaryItemID} not found; attempting to spawn backupID!");
					try
					{
						objectReference = IM.OD[foreignAttachmentSet.BackupItemID];
						spawnedGameObject = Instantiate(objectReference.GetGameObject(), foreignAttachmentSet.AttachmentPoint.position, foreignAttachmentSet.AttachmentPoint.rotation);
						spawnedAttachment = spawnedGameObject.GetComponent<FVRFireArmAttachment>();
						_spawnedAttachments.Add(spawnedAttachment);
                        spawnedGameObject.SetActive(false);
                    }
					catch
					{
						LogWarning($"Item ID {foreignAttachmentSet.BackupItemID} not found; continuing with next attachment in list!");
					}
				}

				if (foreignAttachmentSet.AttachmentPoint != AttachmentMount.Point_Front && foreignAttachmentSet.AttachmentPoint != AttachmentMount.Point_Rear) Destroy(foreignAttachmentSet.AttachmentPoint.gameObject);
			}
		}
#endif
	}
}
