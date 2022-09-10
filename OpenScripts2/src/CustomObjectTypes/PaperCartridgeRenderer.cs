using System.Collections;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class PaperCartridgeRenderer : OpenScripts2_BasePlugin
    {
        public FVRFireArmChamber Chamber;

        public Mesh CartridgeMesh;
        public Material CartridgeMaterial;

        public void LateUpdate()
        {
            if (Chamber.m_round != null && !Chamber.m_round.IsSpent && Chamber.ProxyMesh.mesh != CartridgeMesh)
            {
                Chamber.ProxyMesh.mesh = CartridgeMesh;
                Chamber.ProxyRenderer.material = CartridgeMaterial;
            }
        }
    }
}