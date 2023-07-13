using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class ForceMagazineMountingToMagMountPos : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm;
        private bool _posChanged = false;

        public void Update()
        {
            if (_posChanged && FireArm.Magazine == null)
            {
                if (_posChanged) _posChanged = false;
            }
            else if (!_posChanged && FireArm.Magazine != null)
            {
                if (!FireArm.Magazine.IsBeltBox) FireArm.Magazine.SetParentage(FireArm.MagazineMountPos);
                else FireArm.Magazine.SetParentage(FireArm.BeltBoxMountPos);
                _posChanged = true;
            }
        }
    }
}