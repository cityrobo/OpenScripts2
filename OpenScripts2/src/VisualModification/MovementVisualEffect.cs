using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    [RequireComponent(typeof(VisualModifier))]
    public class MovementVisualEffect : OpenScripts2_BasePlugin
    {
        [Header("Movement Visual Effect Config")]
        public ManipulateTransforms.TransformObservationDefinition ObjectToMonitor;

        private VisualModifier _visualModifier;

        public void Awake()
        {
            _visualModifier = GetComponent<VisualModifier>();
        }

        public void Update()
        {
            _visualModifier.UpdateVisualEffects(ObjectToMonitor.GetObservationLerp());
        }
    }
}
