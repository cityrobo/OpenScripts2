using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    public class RoundClassChangerAttachment : FVRFireArmAttachment
    {
        public FireArmRoundClass RoundClass = FireArmRoundClass.AP;

        private FVRFireArm _firearm = null;
        private FireArmRoundClass _origRoundClass;

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (curMount != null)
            {
                _firearm = curMount.GetRootMount().MyObject as FVRFireArm;
                if (_firearm != null)
                {
                    FVRFireArmChamber chamber = OpenScripts2_BasePlugin.GetCurrentChamber(_firearm);
                    if (chamber != null && chamber.m_round != null && chamber.m_round.RoundClass != RoundClass)
                    {
                        _origRoundClass = chamber.m_round.RoundClass;

                        chamber.m_round = AM.GetRoundSelfPrefab(chamber.m_round.RoundType, RoundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        chamber.UpdateProxyDisplay();
                    }
                }
            }
            else if (_firearm != null)
            {
                FVRFireArmChamber chamber = OpenScripts2_BasePlugin.GetCurrentChamber(_firearm);
                if (chamber != null && chamber.m_round != null)
                {
                    chamber.m_round = AM.GetRoundSelfPrefab(chamber.m_round.RoundType, _origRoundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                    chamber.UpdateProxyDisplay();
                }
                _firearm = null;
            }
        }
    }
}