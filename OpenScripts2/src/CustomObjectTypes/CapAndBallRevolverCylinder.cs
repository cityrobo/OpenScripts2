using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    public class CapAndBallRevolverCylinder : SingleActionRevolverCylinder
    {
        [Header("Cap and Ball Revolver Cylinder Config")]
        public FVRFireArmChamber[] CapNipples;

        public float UnrammedCartridgePosition;
        public float RammedCartridgePosition;

        private bool[] _chamberRammed;

        private float[] _lastLerp;

        public void Awake()
        {
            _chamberRammed = new bool[NumChambers];
            _lastLerp = new float[NumChambers];
            for (int i = 0; i < NumChambers; i++)
            {
                Chambers[i].transform.localPosition = new Vector3(Chambers[i].transform.localPosition.x, Chambers[i].transform.localPosition.y, UnrammedCartridgePosition);
                _chamberRammed[i] = false;
                _lastLerp[i] = 0f;
            }
        }

        public bool GetChamberRammed(int chamber)
        {
            return _chamberRammed[chamber];
        }

        public void RamChamber(int chamber, bool value)
        {
            _chamberRammed[chamber] = value;

            if (value)
            {
                Chambers[chamber].transform.ModifyLocalPositionAxis(OpenScripts2_BasePlugin.Axis.Z, RammedCartridgePosition);
            }
            else Chambers[chamber].transform.ModifyLocalPositionAxis(OpenScripts2_BasePlugin.Axis.Z, RammedCartridgePosition);
        }

        public void RamChamber(int chamber, float lerp) 
        {
            if (!_chamberRammed[chamber] && lerp > _lastLerp[chamber])
            {
                /*
                Vector3 lerpPos = Vector3.Lerp(new Vector3(Chambers[chamber].transform.localPosition.x, Chambers[chamber].transform.localPosition.y, UnrammedCartridgePosition), new Vector3(Chambers[chamber].transform.localPosition.x, Chambers[chamber].transform.localPosition.y, RammedCartridgePosition), lerp);
                Chambers[chamber].transform.localPosition = lerpPos;
                */
                Chambers[chamber].transform.ModifyLocalPositionAxis(OpenScripts2_BasePlugin.Axis.Z, lerp);

                _lastLerp[chamber] = lerp;
                if (lerp == 1f)
                {
                    _chamberRammed[chamber] = true;
                    _lastLerp[chamber] = 0f;
                }
            }
        }
    }
}
