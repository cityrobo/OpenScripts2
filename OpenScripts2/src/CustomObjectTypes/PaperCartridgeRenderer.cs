using System.Collections;
using UnityEngine;
using FistVR;
using System.Collections.Generic;

namespace OpenScripts2
{
    public class PaperCartridgeRenderer : OpenScripts2_BasePlugin
    {
        public FVRFireArmChamber Chamber;

        public Mesh CartridgeMesh;
        public Material CartridgeMaterial;

        private GameObject _proxyGameObject;
        private MeshFilter _proxyMeshFilter;
        private MeshRenderer _proxyMeshRenderer;

        private static readonly Dictionary<FVRFireArmChamber, PaperCartridgeRenderer> _existingPaperCartridgeRenderers = new();
        private static readonly Dictionary<Mesh, GameObject> _paperCartridgeMeshProxyDictionary = new();

        public void Awake()
        {
            _existingPaperCartridgeRenderers.Add(Chamber, this);

            if (_paperCartridgeMeshProxyDictionary.TryGetValue(CartridgeMesh, out _proxyGameObject))
            {
                _proxyMeshFilter = _proxyGameObject.GetComponent<MeshFilter>();
                _proxyMeshRenderer = _proxyGameObject.GetComponent<MeshRenderer>();
            }
            else
            {
                _proxyGameObject = new GameObject("ProxyPaperCartridgeRenderer_" + CartridgeMesh.name);
                _proxyGameObject.SetActive(false);
                DontDestroyOnLoad(_proxyGameObject);

                _proxyMeshFilter = _proxyGameObject.AddComponent<MeshFilter>();
                _proxyMeshFilter.sharedMesh = CartridgeMesh;

                _proxyMeshRenderer = _proxyGameObject.AddComponent<MeshRenderer>();
                _proxyMeshRenderer.sharedMaterial = CartridgeMaterial;

                _paperCartridgeMeshProxyDictionary.Add(CartridgeMesh, _proxyGameObject);
            }
        }

        public void OnDestroy()
        {
            _existingPaperCartridgeRenderers.Remove(Chamber);
        }

#if !DEBUG
        static PaperCartridgeRenderer()
        {
            On.FistVR.FVRFireArmChamber.UpdateProxyDisplay += FVRFireArmChamber_UpdateProxyDisplay;
        }

        private static void FVRFireArmChamber_UpdateProxyDisplay(On.FistVR.FVRFireArmChamber.orig_UpdateProxyDisplay orig, FVRFireArmChamber self)
        {
            if (_existingPaperCartridgeRenderers.TryGetValue(self, out PaperCartridgeRenderer paperCartridgeRenderer))
            {
                if (self.m_round == null)
                {
                    self.ProxyMesh.mesh = null;
                    self.ProxyRenderer.material = null;
                    self.ProxyRenderer.enabled = false;
                }
                else
                {
                    if (self.IsSpent)
                    {
                        if (self.m_round.FiredRenderer != null)
                        {
                            self.ProxyMesh.mesh = self.m_round.FiredRenderer.GetComponent<MeshFilter>().sharedMesh;
                            self.ProxyRenderer.material = self.m_round.FiredRenderer.sharedMaterial;
                        }
                        else
                        {
                            self.ProxyMesh.mesh = null;
                        }
                    }
                    else
                    {
                        self.ProxyMesh.mesh = paperCartridgeRenderer._proxyMeshFilter.sharedMesh;
                        self.ProxyRenderer.material = paperCartridgeRenderer._proxyMeshRenderer.sharedMaterial;
                    }
                    self.ProxyRenderer.enabled = true;
                }
            }
            else orig(self);
        }
#endif
        //public void LateUpdate()
        //{
        //    if (!_materialChanged && Chamber.m_round != null && !Chamber.m_round.IsSpent && Chamber.ProxyMesh.mesh != CartridgeMesh)
        //    {
        //        Chamber.ProxyMesh.mesh = CartridgeMesh;
        //        Chamber.ProxyRenderer.material = CartridgeMaterial;
        //        _materialChanged = true;
        //    }
        //    else if (_materialChanged && Chamber.m_round == null)
        //    {
        //        _materialChanged = false;
        //    }
        //}
    }
}