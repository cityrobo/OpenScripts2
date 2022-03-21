using System;
using UnityEngine;
using FistVR;
using System.Collections;
using System.Collections.Generic;

namespace OpenScripts2
{
	public class PreattachedForeignAttachments : OpenScripts2_BasePlugin
	{
		public FVRFireArmAttachmentMount AttachmentMount;
		public string[] PrimaryItemIDs;
		[Tooltip("If your item fails to spawn, it will spawn the backup ID.")]
		public string[] BackupItemIDs;
		[Tooltip("Position and Rotation to spawn the Attachment at.")]
		public Transform[] AttachmentPoints;

		private List<FVRFireArmAttachment> _attachments;
		private List<ItemCallerSet> _sets;

#if !DEBUG
		public void Start()
		{
			_attachments = new List<FVRFireArmAttachment>();
			_sets = new List<ItemCallerSet>();
			for (int i = 0; i < PrimaryItemIDs.Length; i++)
			{
				_sets.Add(new ItemCallerSet(PrimaryItemIDs[i], BackupItemIDs[i], AttachmentPoints[i]));
				//Debug.Log(string.Format("Added to Sets: {0}/{1} at position {2}.", _sets[i].primaryItemID, _sets[i].backupID, _sets[i].attachmentPoint));
			}

			SpawnAttachments();
			StartCoroutine("AttachAllToMount");
		}

		public IEnumerator AttachAllToMount()
		{
			yield return null;

			foreach (var attachment in _attachments)
			{
				//Debug.Log("Attaching: " + attachment.name);

				attachment.AttachToMount(AttachmentMount, false);
				if (attachment is Suppressor)
				{
					Suppressor tempSup = attachment as Suppressor;
					tempSup.AutoMountWell();
				}
				yield return null;
			}
		}
		public void SpawnAttachments()
		{
			GameObject gameObject;
			FVRFireArmAttachment spawned_attachment;
			FVRObject obj;
			foreach (var set in _sets)
			{
				gameObject = null;
				spawned_attachment = null;
				obj = null;
				try
				{
					obj = IM.OD[set.PrimaryItemID];
					gameObject = Instantiate(obj.GetGameObject(), set.AttachmentPoint.position, set.AttachmentPoint.rotation);
					spawned_attachment = gameObject.GetComponent<FVRFireArmAttachment>();
					//Debug.Log("Spawned: " + spawned_attachment.name);

					_attachments.Add(spawned_attachment);
				}
				catch
				{
					try
					{
						this.Log($"Item ID {set.PrimaryItemID} not found; attempting to spawn backupID!");
						obj = IM.OD[set.BackupItemID];
						gameObject = Instantiate(obj.GetGameObject(), set.AttachmentPoint.position, set.AttachmentPoint.rotation);
						spawned_attachment = gameObject.GetComponent<FVRFireArmAttachment>();
						//Debug.Log("Spawned: " + spawned_attachment.name);
						_attachments.Add(spawned_attachment);
					}
					catch
					{
						this.LogWarning($"Item ID {set.BackupItemID} not found; Continuing load with next object in list!");
					}
				}
			}

		}

#endif
		public class ItemCallerSet
		{
			public ItemCallerSet(string primaryItemID, string backupID, Transform attachmentPoint)
			{
				this.PrimaryItemID = primaryItemID;
				this.BackupItemID = backupID;
				this.AttachmentPoint = attachmentPoint;
			}

			public string PrimaryItemID;
			[Tooltip("If your item fails to spawn, it will spawn the backup ID.")]
			public string BackupItemID;
			[Tooltip("Position and Rotation to spawn the Attachment at.")]
			public Transform AttachmentPoint;
		}
	}
}
