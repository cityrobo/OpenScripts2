using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
#if DEBUG
using UnityEditor;
#endif
using UnityEngine;
using FistVR;
using System.Linq;
using System.IO;

namespace OpenScripts2
{
    public class MagazineHelper : OpenScripts2_BasePlugin
    {
        // Magazine properties
        public FVRFireArmMagazine Magazine;
        public bool IsCurved;
        public Transform MagazineRadiusCenter;

        // Cartridge properties
        public List<GameObject> ManuallyAddedCartridgePositions = new();
        public GameObject FirstCartridgeToGenerateFrom;
        public int NumberOfCartridgesToGenerate = 1;

        public bool MirrorX;
        public float CartridgeOffsetY = 0f;
        public float CartridgeOffsetZ = 0f;

        public float CartridgeAngleOffsetX;

        // Follower properties
        public bool GenerateFollowerPoints;
        public bool UseFollowerOffsets = false;
        public bool InvertFollowerOffsets = false;

        public GameObject EmptyMagazineFollowerPosition;
        public List<GameObject> ManuallyAddedFollowerPositions = new();
        public GameObject FirstFollowerPositionToGenerateFrom;

        public float FollowerOffsetY = 0f;
        public float FollowerOffsetZ = 0f;

        // Spring properties
        public bool GenerateSpringScales = false;
        public float FullMagazineSpringScale;
        public List<float> ManuallyAddedSpringScales = new();
        public float FirstRoundSpringScaleToGenerateFrom;
        public float EmptyMagazineSpringScale;

        //Gizmo properties
        public bool ShowGizmoToggle = false;
        public float GizmoSize = 0.01f;

        public List<GameObject> AddedCartridgePositions;
        public List<GameObject> AddedFollowerPositions;
    }

#if DEBUG
    [UnityEditor.CustomEditor(typeof(MagazineHelper))]
    public class MagazineHelperEditor : Editor
    {
        //// Magazine properties
        //public FVRFireArmMagazine Magazine;
        //public bool IsCurved;
        //public Transform MagazineRadiusCenter;

        //// Cartridge properties
        //public GameObject FirstCartridge;
        //public GameObject SecondCartridge;

        //public List<GameObject> ManuallyAddedCartridgePositions = new List<GameObject>();
        //public GameObject FirstCartridgeToGenerateFrom;
        //public int NumberOfCartridges = 1;

        //public bool MirrorX;
        //public float CartridgeOffsetY = 0f;
        //public float CartridgeOffsetZ = 0f;

        //public float CartridgeAngleOffsetX;

        //// Follower properties
        //public bool GenerateFollowerPoints;
        //public bool UseFollowerOffsets = false;
        //public bool InvertFollowerOffsets = false;

        //public GameObject EmptyMagazineFollowerPosition;
        //public List<GameObject> ManuallyAddedFollowerPositions = new List<GameObject>();
        //public GameObject FirstFollowerPositionToGenerateFrom;

        //public GameObject FirstRoundFollower;
        //public GameObject SecondRoundFollower;

        //public float FollowerOffsetY = 0f;
        //public float FollowerOffsetZ = 0f;

        //// Spring properties
        //public bool GenerateSpringScales = false;

        //public float SecondRoundSpringScale;

        //public float EmptyMagazineSpringScale;

        //public float[] ManuallyAddedSpringScales;

        //public float FirstRoundSpringScaleToGenerateFrom;
        //public float FullMagazineSpringScale;

        ////Gizmo properties
        //public bool ShowGizmoToggle = false;
        //public float GizmoSize = 0.01f;

        //public List<GameObject> AddedCartridgePositions = new List<GameObject>();
        //public List<GameObject> AddedFollowerPositions = new List<GameObject>();

        // Private variables
        GameObject _cartridgeRoot;

        List<GameObject> _cartridgeObjectList = new List<GameObject>();
        List<MeshFilter> _cartridgeMeshFilterList = new List<MeshFilter>();
        List<MeshRenderer> _cartridgeMeshRendererList = new List<MeshRenderer>();

        float _cartridgeCurrentX;
        float _cartridgeCurrentY;
        float _cartridgeCurrentZ;

        bool _cartridgeGenerationReady = true;
        bool _followerGenerationReady = true;

        GameObject _followerRoot;

        List<GameObject> _followerObjectList = new List<GameObject>();

        List<float> _springScales = new List<float>();

        float _followerCurrentX;
        float _followerCurrentY;
        float _followerCurrentZ;

        float _cartridgeAngleX;
        float _cartridgeAngleY;

        GameObject _gizmoObject;
        MagazineHelperGizmo _gizmo;
        MagazineHelper _m;

        public void Awake()
        {
            _m = (target as MagazineHelper);

            //UpdateEditorValues(_m);
        }

        public void OnDestroy()
        {
            DestroyImmediate(_gizmoObject);
        }

        public override void OnInspectorGUI()
        {
            _m = (target as MagazineHelper);

            GUILayout.Label("Cartridge Settings", EditorStyles.boldLabel);
            EditorGUIUtility.labelWidth = 300f;
            //myString = EditorGUILayout.TextField ("Text Field", myString);

            //groupEnabled = EditorGUILayout.BeginToggleGroup ("Optional Settings", groupEnabled);
            //myBool = EditorGUILayout.Toggle ("Toggle", myBool);
            //myFloat = EditorGUILayout.Slider ("Slider", myFloat, -3f, 3f);
            //EditorGUILayout.EndToggleGroup ();

            SerializedObject sO = new SerializedObject(_m);

            _m.Magazine = (FVRFireArmMagazine)EditorGUILayout.ObjectField("Magazine", _m.Magazine, typeof(FVRFireArmMagazine), true);
            if (_m.Magazine == null)
            {
                EditorGUILayout.HelpBox("Please add Magazine!", MessageType.Error);
                _cartridgeGenerationReady = false;
            }
            else
            {
                _m.IsCurved = EditorGUILayout.Toggle("Magazine is curved", _m.IsCurved);
                if (_m.IsCurved)
                {
                    _m.MagazineRadiusCenter = EditorGUILayout.ObjectField("Magazine Radius Center", _m.MagazineRadiusCenter, typeof(Transform), true) as Transform;
                    if (_m.MagazineRadiusCenter == null)
                    {
                        EditorGUILayout.HelpBox("Please add Magazine Radius Center!", MessageType.Error);
                        _cartridgeGenerationReady = false;
                    }
                    else
                    {
                        _m.CartridgeAngleOffsetX = EditorGUILayout.Slider("Cartridge Angle Offset X", _m.CartridgeAngleOffsetX, -10f, 10f);
                        _m.ShowGizmoToggle = EditorGUILayout.Toggle("Show Curved Mag Gizmo", _m.ShowGizmoToggle);
                        _m.GizmoSize = EditorGUILayout.FloatField("Gizmo Size", _m.GizmoSize);
                    }
                }
                else _m.ShowGizmoToggle = false;

                ShowGizmo(_m.ShowGizmoToggle);
                _m.FirstCartridgeToGenerateFrom = (GameObject)EditorGUILayout.ObjectField("First cartridge position to generate from", _m.FirstCartridgeToGenerateFrom, typeof(GameObject), true);
                if (_m.FirstCartridgeToGenerateFrom == null)
                {
                    EditorGUILayout.HelpBox("Please add Reference Cartridge!", MessageType.Error);
                    _cartridgeGenerationReady = false;
                }
                else
                {
                    //GUILayout.Label("Single Feed Options", EditorStyles.boldLabel);
                    //FirstCartridge = (GameObject)EditorGUILayout.ObjectField("Custom first cartridge position", FirstCartridge, typeof(GameObject), true);
                    //SecondCartridge = (GameObject)EditorGUILayout.ObjectField("Custom second cartridge position", SecondCartridge, typeof(GameObject), true);

                    EditorGUILayout.PropertyField(sO.FindProperty("ManuallyAddedCartridgePositions"), true);
                }

                _m.MirrorX = EditorGUILayout.Toggle("Is magazine double stacked? (Mirror X axis coordinates for alternating rounds.)", _m.MirrorX);
                if (!_m.IsCurved)
                {
                    _m.CartridgeOffsetY = EditorGUILayout.Slider("Y axis distance between rounds", _m.CartridgeOffsetY, -0.1f, 0.1f);
                    _m.CartridgeOffsetZ = EditorGUILayout.Slider("Z Axis distance between rounds", _m.CartridgeOffsetZ, -0.1f, 0.1f);
                }

                _m.GenerateFollowerPoints = EditorGUILayout.Toggle("Generate Follower Points?", _m.GenerateFollowerPoints);
                if (_m.GenerateFollowerPoints)
                {
                    _m.FirstFollowerPositionToGenerateFrom = (GameObject)EditorGUILayout.ObjectField("First follower position to generate from", _m.FirstFollowerPositionToGenerateFrom, typeof(GameObject), true);
                    if (_m.FirstFollowerPositionToGenerateFrom == null)
                    {
                        EditorGUILayout.HelpBox("Please add FirstFollowerPositionToGenerateFrom!", MessageType.Error);
                        _followerGenerationReady = false;
                    }
                    else
                    {
                        _m.EmptyMagazineFollowerPosition = (GameObject)EditorGUILayout.ObjectField("Empty magazine follower position", _m.EmptyMagazineFollowerPosition, typeof(GameObject), true);
                        //GUILayout.Label("Single Feed Options", EditorStyles.boldLabel);
                        //FirstRoundFollower = (GameObject)EditorGUILayout.ObjectField("Custom first round follower position", FirstRoundFollower, typeof(GameObject), true);
                        //SecondRoundFollower = (GameObject)EditorGUILayout.ObjectField("Custom second rounds follower position", SecondRoundFollower, typeof(GameObject), true);
                        EditorGUILayout.PropertyField(sO.FindProperty("ManuallyAddedFollowerPositions"), true);
                    }

                    _m.FollowerOffsetY = _m.CartridgeOffsetY;
                    _m.FollowerOffsetZ = _m.CartridgeOffsetZ;
                }

                _m.GenerateSpringScales = EditorGUILayout.Toggle("Generate Spring Scales?", _m.GenerateSpringScales);
                if (_m.GenerateSpringScales)
                {
                    _m.EmptyMagazineSpringScale = EditorGUILayout.FloatField("Zero Round Spring Scale", _m.EmptyMagazineSpringScale);
                    _m.FirstRoundSpringScaleToGenerateFrom = EditorGUILayout.FloatField("First Round Spring Scale", _m.FirstRoundSpringScaleToGenerateFrom);
                    EditorGUILayout.PropertyField(sO.FindProperty("ManuallyAddedSpringScales"), true);
                    //_m.SecondRoundSpringScale = EditorGUILayout.FloatField("Second Round Spring Scale", _m.SecondRoundSpringScale);
                    _m.FullMagazineSpringScale = EditorGUILayout.FloatField("Last Round Spring Scale", _m.FullMagazineSpringScale);
                }

                _m.NumberOfCartridgesToGenerate = EditorGUILayout.IntField($"Number of cartridge{(_m.GenerateFollowerPoints ? " and follower" : "")} positions to generate", _m.NumberOfCartridgesToGenerate);
                if (_m.NumberOfCartridgesToGenerate <= 0) _m.NumberOfCartridgesToGenerate = 1;

                if (_cartridgeGenerationReady && !_m.GenerateFollowerPoints)
                {
                    if (GUILayout.Button("Add Cartridges", GUILayout.ExpandWidth(true)))
                    {
                        AddCartridges();
                    }

                    if (GUILayout.Button("Clear Cartridges", GUILayout.ExpandWidth(true)))
                    {
                        ClearCartridges(true);
                    }
                }
                else if (_cartridgeGenerationReady && _followerGenerationReady)
                {
                    if (GUILayout.Button("Add cartridges and follower points", GUILayout.ExpandWidth(true)))
                    {
                        AddCartridges();
                        AddFollowerPoints();
                    }
                    if (GUILayout.Button("Clear cartridges and follower points", GUILayout.ExpandWidth(true)))
                    {
                        ClearCartridges(true);
                        ClearFollowerPoints(true);
                    }
                    if (GUILayout.Button("Remove follower points", GUILayout.ExpandWidth(true)))
                    {
                        RemoveFollowerPointVisuals();
                    }
                }

                if (_m.GenerateSpringScales && _m.EmptyMagazineSpringScale != 0f && _m.FirstRoundSpringScaleToGenerateFrom != 0f && _m.FullMagazineSpringScale != 0f)
                {
                    if (GUILayout.Button("Generate spring scales and populate magazine", GUILayout.ExpandWidth(true)))
                    {
                        AddSpringScales();
                    }
                    if (GUILayout.Button("Clear spring scales", GUILayout.ExpandWidth(true)))
                    {
                        ClearSpringScales();
                    }
                }

                _cartridgeGenerationReady = true;
                _followerGenerationReady = true;

                //UpdateConfigValues(m);
            }
        }

        private void AddCartridges()
        {
            ClearCartridges();
            _cartridgeObjectList.Clear();
            _cartridgeMeshFilterList.Clear();
            _cartridgeMeshRendererList.Clear();

            _cartridgeObjectList.AddRange(_m.ManuallyAddedCartridgePositions);
            _cartridgeMeshFilterList.AddRange(_m.ManuallyAddedCartridgePositions.Select(c => c.GetComponent<MeshFilter>()));
            _cartridgeMeshRendererList.AddRange(_m.ManuallyAddedCartridgePositions.Select(c => c.GetComponent<MeshRenderer>()));

            //if (FirstCartridge != null)
            //{
            //    _cartridgeObjectList.Add(FirstCartridge);
            //    _cartridgeMeshFilterList.Add(FirstCartridge.GetComponent<MeshFilter>());
            //    _cartridgeMeshRendererList.Add(FirstCartridge.GetComponent<MeshRenderer>());
            //}
            //if (SecondCartridge != null)
            //{
            //    _cartridgeObjectList.Add(SecondCartridge);
            //    _cartridgeMeshFilterList.Add(SecondCartridge.GetComponent<MeshFilter>());
            //    _cartridgeMeshRendererList.Add(SecondCartridge.GetComponent<MeshRenderer>());
            //}

            _cartridgeObjectList.Add(_m.FirstCartridgeToGenerateFrom);
            _cartridgeMeshFilterList.Add(_m.FirstCartridgeToGenerateFrom.GetComponent<MeshFilter>());
            _cartridgeMeshRendererList.Add(_m.FirstCartridgeToGenerateFrom.GetComponent<MeshRenderer>());

            _cartridgeCurrentX = _m.FirstCartridgeToGenerateFrom.transform.position.x;
            _cartridgeCurrentY = _m.FirstCartridgeToGenerateFrom.transform.position.y;
            _cartridgeCurrentZ = _m.FirstCartridgeToGenerateFrom.transform.position.z;

            //if (_cartridgeRoot == null)
            //{
            //    _cartridgeRoot = new GameObject
            //    {
            //        name = "Cartridge Root"
            //    };
            //    _cartridgeRoot.transform.parent = Magazine.Viz;
            //    _cartridgeRoot.transform.localPosition = new Vector3(0, 0, 0);
            //    _cartridgeRoot.transform.localEulerAngles = new Vector3(0, 0, 0);
            //    _cartridgeRoot.transform.localScale = new Vector3(1, 1, 1);
            //}

            for (int i = 1; i <= _m.NumberOfCartridgesToGenerate; i++)
            {
                if (_m.IsCurved)
                {
                    Vector3 curvedPos = CalculateNextCartridgePos(_m.CartridgeAngleOffsetX * i );

                    _cartridgeCurrentY = curvedPos.y;
                    _cartridgeCurrentZ = curvedPos.z;
                }
                else
                {
                    _cartridgeCurrentY += _m.CartridgeOffsetY;
                    _cartridgeCurrentZ += _m.CartridgeOffsetZ;
                }

                int sign = i % 2;
                if (!_m.MirrorX) sign = 1;
                else if (sign == 0) sign = 1;
                else if (sign == 1) sign = -1;

                Quaternion nextCartridgeRotation = _m.FirstCartridgeToGenerateFrom.transform.rotation;
                Vector3 eulerAngles;
                if (_m.IsCurved)
                {
                    eulerAngles = new Vector3(_m.FirstCartridgeToGenerateFrom.transform.rotation.eulerAngles.x - _m.CartridgeAngleOffsetX * i, sign * _m.FirstCartridgeToGenerateFrom.transform.rotation.eulerAngles.y, _m.FirstCartridgeToGenerateFrom.transform.rotation.eulerAngles.z);
                }
                else
                {
                    eulerAngles = new Vector3(_m.FirstCartridgeToGenerateFrom.transform.rotation.eulerAngles.x, sign * _m.FirstCartridgeToGenerateFrom.transform.rotation.eulerAngles.y, _m.FirstCartridgeToGenerateFrom.transform.rotation.eulerAngles.z);
                }

                nextCartridgeRotation = Quaternion.Euler(eulerAngles);
                Vector3 nextCartridgePosition = new Vector3(_cartridgeCurrentX, _cartridgeCurrentY, _cartridgeCurrentZ);
                GameObject addedCartridgePosition = Instantiate(_m.FirstCartridgeToGenerateFrom, nextCartridgePosition, nextCartridgeRotation, _m.Magazine.Viz);

                addedCartridgePosition.name = _m.FirstCartridgeToGenerateFrom.name + " (" + i.ToString() + ")";

                Vector3 localPos = addedCartridgePosition.transform.localPosition;
                localPos.x *= sign;
                addedCartridgePosition.transform.localPosition = localPos;
                //if (_m.MirrorX && i % 2 == 1)
                //{
                //    Vector3 localPos = addedCartridgePosition.transform.localPosition;
                //    localPos.x = -localPos.x;
                //    addedCartridgePosition.transform.localPosition = localPos;
                //}

                _cartridgeObjectList.Add(addedCartridgePosition);
                _cartridgeMeshFilterList.Add(addedCartridgePosition.GetComponent<MeshFilter>());
                _cartridgeMeshRendererList.Add(addedCartridgePosition.GetComponent<MeshRenderer>());

                _m.AddedCartridgePositions.Add(addedCartridgePosition);
            }

            _m.Magazine.DisplayBullets = _cartridgeObjectList.ToArray();
            _m.Magazine.DisplayMeshFilters = _cartridgeMeshFilterList.ToArray();
            _m.Magazine.DisplayRenderers = _cartridgeMeshRendererList.ToArray();
        }

        private void ClearCartridges(bool destroyAll = false)
        {
            if (_cartridgeRoot != null)
            {
                int children = _cartridgeRoot.transform.childCount;
                for (int i = children - 1; i >= 0; i--)
                {
                    DestroyImmediate(_cartridgeRoot.transform.GetChild(i).gameObject);
                }
                if (destroyAll)
                {
                    DestroyImmediate(_cartridgeRoot);
                }
            }

            for (int i = 0; i < _m.AddedCartridgePositions.Count; i++)
            {
                DestroyImmediate(_m.AddedCartridgePositions[i]);
            }
            _m.AddedCartridgePositions.Clear();
        }

        private void AddFollowerPoints()
        {
            ClearFollowerPoints();
            _followerObjectList.Clear();

            if (_m.EmptyMagazineFollowerPosition != null)
            {
                _followerObjectList.Add(_m.EmptyMagazineFollowerPosition);
            }
            //if (FirstRoundFollower != null)
            //{
            //    _followerObjectList.Add(FirstRoundFollower);
            //}
            //if (SecondRoundFollower != null)
            //{
            //    _followerObjectList.Add(SecondRoundFollower);
            //}

            _followerObjectList.AddRange(_m.ManuallyAddedFollowerPositions);

            _followerObjectList.Add(_m.FirstFollowerPositionToGenerateFrom);

            _followerCurrentX = _m.FirstFollowerPositionToGenerateFrom.transform.position.x;
            _followerCurrentY = _m.FirstFollowerPositionToGenerateFrom.transform.position.y;
            _followerCurrentZ = _m.FirstFollowerPositionToGenerateFrom.transform.position.z;

            //if (_followerRoot == null)
            //{
            //    _followerRoot = new GameObject();
            //    _followerRoot.name = "FirstFollowerPositionToGenerateFrom Root";
            //    _followerRoot.transform.parent = Magazine.Viz;
            //    _followerRoot.transform.localPosition = new Vector3(0, 0, 0);
            //    _followerRoot.transform.localEulerAngles = new Vector3(0, 0, 0);
            //    _followerRoot.transform.localScale = new Vector3(1, 1, 1);
            //}

            //for (int i = 2; i <= NumberOfCartridges; i++)
            //{
            //    if (IsCurved)
            //    {
            //        Vector3 curvedPos = CalculateNextFollowerPos(CartridgeAngleOffsetX * (i - 1));

            //        _followerCurrentY = curvedPos.y;
            //        _followerCurrentZ = curvedPos.z;
            //    }
            //    else
            //    {
            //        _followerCurrentY += FollowerOffsetY;
            //        _followerCurrentZ += FollowerOffsetZ;
            //    }

            //    Vector3 nextFollowerPositions = new Vector3(_followerCurrentX, _followerCurrentY, _followerCurrentZ);
            //    Quaternion nextFollowerRotations = FirstFollowerPositionToGenerateFrom.transform.rotation;
            //    Vector3 eulerAngles;
            //    if (IsCurved)
            //    {
            //        eulerAngles = new Vector3(FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.x - CartridgeAngleOffsetX * (i - 1), FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.y, FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.z);
            //    }
            //    else
            //    {
            //        eulerAngles = new Vector3(FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.x, FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.y, FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.z);
            //    }

            //    nextFollowerRotations = Quaternion.Euler(eulerAngles);
            //    GameObject addedFollowerPosition = Instantiate(FirstFollowerPositionToGenerateFrom, nextFollowerPositions, nextFollowerRotations, Magazine.Viz);

            //    addedFollowerPosition.transform.localScale = FirstFollowerPositionToGenerateFrom.transform.lossyScale;
            //    addedFollowerPosition.name = FirstFollowerPositionToGenerateFrom.name + " (" + i.ToString() + ")";

            //    _followerObjectList.Add(addedFollowerPosition);

            //    AddedFollowerPositions.Add(addedFollowerPosition);
            //}

            float yOffset = _m.FirstFollowerPositionToGenerateFrom.transform.localPosition.y - _m.FirstCartridgeToGenerateFrom.transform.localPosition.y;
            float zOffset = _m.FirstFollowerPositionToGenerateFrom.transform.localPosition.z - _m.FirstCartridgeToGenerateFrom.transform.localPosition.z;
            for (int i = 1 + _m.ManuallyAddedCartridgePositions.Count; i < _cartridgeObjectList.Count; i++)
            {
                Vector3 nextFollowerPositions = _cartridgeObjectList[i].transform.TransformPoint(0f, yOffset, zOffset);
                nextFollowerPositions.x = _m.FirstFollowerPositionToGenerateFrom.transform.position.x;
                //nextFollowerPositions.y += yOffset;
                //nextFollowerPositions.z += zOffset;
                Quaternion nextFollowerRotations = _m.FirstFollowerPositionToGenerateFrom.transform.rotation;
                Vector3 eulerAngles;
                if (_m.IsCurved)
                {
                    eulerAngles = new Vector3(_m.FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.x - _m.CartridgeAngleOffsetX * (i - 1), _m.FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.y, _m.FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.z);
                }
                else
                {
                    eulerAngles = new Vector3(_m.FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.x, _m.FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.y, _m.FirstFollowerPositionToGenerateFrom.transform.rotation.eulerAngles.z);
                }

                nextFollowerRotations = Quaternion.Euler(eulerAngles);
                GameObject addedFollowerPosition = Instantiate(_m.FirstFollowerPositionToGenerateFrom, nextFollowerPositions, nextFollowerRotations, _m.Magazine.Viz);

                addedFollowerPosition.transform.localScale = _m.FirstFollowerPositionToGenerateFrom.transform.localScale;
                addedFollowerPosition.name = _m.FirstFollowerPositionToGenerateFrom.name + " (" + i.ToString() + ")";

                _followerObjectList.Add(addedFollowerPosition);

                _m.AddedFollowerPositions.Add(addedFollowerPosition);
            }

            _m.Magazine.FollowerPositions = _followerObjectList.Select(f => _m.Magazine.Viz.InverseTransformPoint(f.transform.position)).ToArray();
            _m.Magazine.FollowerEulers = _followerObjectList.Select(f => _m.Magazine.Viz.InverseTransformRotation(f.transform.rotation).eulerAngles).ToArray();
        }

        private void ClearFollowerPoints(bool destroyAll = false)
        {
            if (_followerRoot != null)
            {
                int children = _followerRoot.transform.childCount;
                for (int i = children - 1; i >= 0; i--)
                {
                    DestroyImmediate(_followerRoot.transform.GetChild(i).gameObject);
                }
                if (destroyAll) DestroyImmediate(_followerRoot);
            }

            for (int i = 0; i < _m.AddedFollowerPositions.Count; i++)
            {
                DestroyImmediate(_m.AddedFollowerPositions[i]);
            }
            _m.AddedFollowerPositions.Clear();
        }

        private void RemoveFollowerPointVisuals()
        {
            //if (_followerRoot == null) return;
            foreach (var follower in _m.AddedFollowerPositions)
            {
                MeshRenderer renderer = follower.GetComponent<MeshRenderer>() ?? follower.GetComponentInChildren<MeshRenderer>();
                DestroyImmediate(renderer);
                MeshFilter filter = follower.GetComponent<MeshFilter>() ?? follower.GetComponentInChildren<MeshFilter>();
                DestroyImmediate(filter);
                DestroyImmediate(follower);
            }
            _m.AddedFollowerPositions.Clear();
        }

        private void AddSpringScales()
        {
            _springScales.Clear();
            _springScales.Add(_m.EmptyMagazineSpringScale);
            _springScales.AddRange(_m.ManuallyAddedSpringScales);
            _springScales.Add(_m.FirstRoundSpringScaleToGenerateFrom);

            float deltaSpringScale = _m.FullMagazineSpringScale - _m.FirstRoundSpringScaleToGenerateFrom;
            deltaSpringScale /= _m.NumberOfCartridgesToGenerate;
            float startValue = _m.FirstRoundSpringScaleToGenerateFrom;

            for (int i = 1; i < _m.NumberOfCartridgesToGenerate; i++)
            {
                _springScales.Add(startValue + deltaSpringScale * i);
            }

            _m.Magazine.SpringScales = _springScales.ToArray();
        }

        private void ClearSpringScales()
        {
            _springScales.Clear();
            _m.Magazine.SpringScales = _springScales.ToArray();
        }

        private void ShowGizmo(bool on)
        {
            if (on)
            {
                if (_gizmoObject == null) _gizmoObject = new GameObject("MagazineGizmo");
                _gizmoObject.transform.position = _m.MagazineRadiusCenter.position;
                if (_gizmo == null) _gizmo = _gizmoObject.AddComponent<MagazineHelperGizmo>();
                _gizmo.GizmoSize = _m.GizmoSize;
            }
            else
            {
                if (_gizmoObject != null) DestroyImmediate(_gizmoObject);
            }
        }

        private Vector3 CalculateNextCartridgePos(float deltaA)
        {
            deltaA *= Mathf.Deg2Rad;
            Vector3 delta = _m.FirstCartridgeToGenerateFrom.transform.position - _m.MagazineRadiusCenter.position;
            float radius = Mathf.Sqrt(Mathf.Pow(delta.y, 2) + Mathf.Pow(delta.z, 2));
            float angle = Mathf.Atan2(delta.y, delta.z);

            //ClearConsole();

            //Debug.Log("Radius: " + radius);
            //Debug.Log("Angle: " + Mathf.Rad2Deg*angle);

            float y = Mathf.Sin(angle + deltaA) * radius + _m.MagazineRadiusCenter.transform.position.y;
            float z = Mathf.Cos(angle + deltaA) * radius + _m.MagazineRadiusCenter.transform.position.z;

            Vector3 pos = new Vector3(_m.FirstCartridgeToGenerateFrom.transform.position.x, y, z);

            //Debug.Log("Before:");

            //Debug.Log(pos.y);
            //Debug.Log(pos.z);

            //GameObject test1 = GameObject.Instantiate(firstCartridge, pos, new Quaternion(), cartridge_root.transform);

            return pos;
        }

        private Vector3 CalculateNextFollowerPos(float deltaA)
        {
            deltaA *= Mathf.Deg2Rad;
            Vector3 delta = _m.FirstFollowerPositionToGenerateFrom.transform.position - _m.MagazineRadiusCenter.position;
            float radius = Mathf.Sqrt(Mathf.Pow(delta.y, 2) + Mathf.Pow(delta.z, 2));
            float angle = Mathf.Atan2(delta.y, delta.z);

            //ClearConsole();

            //Debug.Log("Radius: " + radius);
            //Debug.Log("Angle: " + Mathf.Rad2Deg*angle);

            float y = Mathf.Sin(angle + deltaA) * radius + _m.MagazineRadiusCenter.transform.position.y;
            float z = Mathf.Cos(angle + deltaA) * radius + _m.MagazineRadiusCenter.transform.position.z;

            Vector3 pos = new Vector3(_m.FirstFollowerPositionToGenerateFrom.transform.position.x, y, z);

            //Debug.Log("Before:");

            //Debug.Log(pos.y);
            //Debug.Log(pos.z);

            //GameObject test1 = GameObject.Instantiate(firstCartridge, pos, new Quaternion(), cartridge_root.transform);

            return pos;
        }      
    }
#endif
}