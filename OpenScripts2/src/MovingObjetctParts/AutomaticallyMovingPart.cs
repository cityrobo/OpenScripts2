using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class AutomaticallyMovingPart : OpenScripts2_BasePlugin
    {
        [Header("AutomaticallyMovingPart Config")]       
        public TransformType MovementType;
        public Axis MovementAxis;
        public float MaxSpeed;
        public bool DoesUseAcceleration;
        public float Acceleration;

        public float LowerLimit;
        public float UpperLimit;

        [Tooltip("Use this if you have a part that's only supposed to rotate in one direction at a constant speed.")]
        public bool IsConstantRotation;
        public bool Reversed;

        private Vector3 _targetPos;

        private float _currentRot;
        private bool _isRotating = false;

        private float _currentSpeed = 0;
      
        public void Awake()
        {
            _currentRot = transform.localEulerAngles.GetAxisValue(MovementAxis);
        }

        public void Update()
        {
            if (DoesUseAcceleration)
            {
                _currentSpeed += Acceleration * Time.deltaTime;
                _currentSpeed = Mathf.Clamp(_currentSpeed, 0f, MaxSpeed);
            }
            else _currentSpeed = MaxSpeed;

            switch (MovementType)
            {
                case TransformType.Movement:
                    Movement();
                    break;
                case TransformType.Rotation:
                    Rotation();
                    break;
                case TransformType.Scale:
                    Scale();
                    break;
            }
        }

        private void Movement()
        {
            _targetPos = transform.localPosition;
            if (Reversed) _targetPos = _targetPos.ModifyAxisValue(MovementAxis, LowerLimit);
            else _targetPos = _targetPos.ModifyAxisValue(MovementAxis, UpperLimit);

            if (transform.localPosition != _targetPos)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, _targetPos, _currentSpeed * Time.deltaTime);
            }
            else
            {
                Reversed = !Reversed;
                _currentSpeed = 0;
            }
        }

        private void Rotation()
        {
            if (IsConstantRotation)
            {
                if (!Reversed) transform.Rotate(GetVectorFromAxis(MovementAxis), _currentSpeed * Time.deltaTime);
                else transform.Rotate(GetVectorFromAxis(MovementAxis), -_currentSpeed * Time.deltaTime);
            }
            else
            {
                if (!_isRotating && !Reversed)
                {
                    _currentSpeed = 0f;
                    _isRotating = true;
                    StartCoroutine(Rotate(UpperLimit));
                }
                else if (!_isRotating && Reversed)
                {
                    _currentSpeed = 0f;
                    _isRotating = true;
                    StartCoroutine(Rotate(LowerLimit));
                }
            }
        }

        private void Scale()
        {
            _targetPos = transform.localScale;
            if (Reversed) _targetPos.ModifyAxisValue(MovementAxis, LowerLimit);
            else _targetPos.ModifyAxisValue(MovementAxis, UpperLimit);

            if (transform.localScale != _targetPos)
            {
                transform.localScale = Vector3.MoveTowards(transform.localScale, _targetPos, _currentSpeed * Time.deltaTime);
            }
            else
            {
                Reversed = !Reversed;
                _currentSpeed = 0;
            }
        }

        private IEnumerator Rotate(float angleToEndAt)
        {
            float startAngle = _currentRot;
            float angle = angleToEndAt - _currentRot;

            if (angleToEndAt > _currentRot)
            {
                for (int i = 1; i <= (int)Mathf.Abs(angle / 179f); i++)
                {
                    Quaternion iterrot = GetTargetQuaternionFromAxis(startAngle + 179f * i,MovementAxis);
                    while (transform.localRotation != iterrot)
                    {
                        //Quaternion deltaRot = Quaternion.RotateTowards(transform.localRotation, iterrot, Speed * Time.deltaTime) * Quaternion.Inverse(transform.localRotation);

                        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, iterrot, _currentSpeed * Time.deltaTime);
                        _currentRot += _currentSpeed * Time.deltaTime;
                        yield return null;
                    }
                }
                Quaternion rot = GetTargetQuaternionFromAxis(angleToEndAt,MovementAxis);
                while (transform.localRotation != rot)
                {
                    //Quaternion deltaRot = Quaternion.RotateTowards(transform.localRotation, rot, Speed * Time.deltaTime) * Quaternion.Inverse(transform.localRotation);

                    transform.localRotation = Quaternion.RotateTowards(transform.localRotation, rot, _currentSpeed * Time.deltaTime);
                    _currentRot += _currentSpeed * Time.deltaTime;
                    yield return null;
                }
                _currentRot = angleToEndAt;
            }
            else if (angleToEndAt < _currentRot)
            {
                for (int i = 1; i <= (int)Mathf.Abs(angle / 179f); i++)
                {
                    Quaternion iterrot = GetTargetQuaternionFromAxis(startAngle - 179f * i,MovementAxis);
                    while (transform.localRotation != iterrot)
                    {
                        //Quaternion deltaRot = Quaternion.RotateTowards(transform.localRotation, iterrot, Speed * Time.deltaTime) * Quaternion.Inverse(transform.localRotation);

                        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, iterrot, _currentSpeed * Time.deltaTime);
                        _currentRot -= _currentSpeed * Time.deltaTime;
                        yield return null;
                    }
                }
                Quaternion rot = GetTargetQuaternionFromAxis(angleToEndAt,MovementAxis);
                while (transform.localRotation != rot)
                {
                    //Quaternion deltaRot = Quaternion.RotateTowards(transform.localRotation, rot, Speed * Time.deltaTime) * Quaternion.Inverse(transform.localRotation);

                    transform.localRotation = Quaternion.RotateTowards(transform.localRotation, rot, _currentSpeed * Time.deltaTime);
                    _currentRot -= _currentSpeed * Time.deltaTime;
                    yield return null;
                }
                _currentRot = angleToEndAt;
            }
            _isRotating = false;
            Reversed = !Reversed;
        }
    }
}
