using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class ForceMagazineMountingToMagMountPos : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm;

        private bool _posChanged;
#if!DEBUG
        public void Update()
        {
            if (_posChanged && FireArm.Magazine == null)
            {
                if (_posChanged) _posChanged = false;
            }
            else if (!_posChanged && FireArm.Magazine != null)
            {
                FireArm.Magazine.SetParentage(FireArm.MagazineMountPos.transform);
                _posChanged = true;
            }
        }
#endif
    }
}
