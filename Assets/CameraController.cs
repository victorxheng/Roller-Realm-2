using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;

public class CameraController : MonoBehaviour
{

    [SerializeField, Range(1f, 15.0f)] public float PitchSensitivity;
    [SerializeField, Range(1f, 15.0f)] public float YawSensitivity;
    [SerializeField, Range(0.1f, 5f)] public float ZoomSensitivity;
    [SerializeField] public float PitchZoomLimit;
    [SerializeField] public float PitchLowerLimit;
    [SerializeField] public float PitchUpperLimit;
    [SerializeField] public float ZoomLowerLimit;
    [SerializeField] public float ZoomUpperLimit;
    [SerializeField, Range(0.005f, 2.0f)] public float OrbitalAcceleration;
    [SerializeField, Range(0.005f, 2.0f)] public float ZoomAcceleration;
    [SerializeField] public Transform FollowTransform;

    private float _targetPitch = 20.0f;
    private float _targetYaw = 0.0f;
    private float _actualZoom = 12.0f;
    private float _actualPitch = 0.0f;
    private float _actualYaw = 0.0f;
    private bool _useOrbit = false;
    private Vector3 offset = new Vector3(0, 1, 0);

    private void Start()
    { 
        
    }
    public void Update()
    {
        float p = 0.0f;
        float y = 0.0f;

        bool isOverUI = EventSystem.current.IsPointerOverGameObject();
        bool enableOrbit = !isOverUI;

        if (!Input.GetKey(KeyCode.Mouse0))
        {
            _useOrbit = enableOrbit;
        }
        else
        {
            if (_useOrbit)
            {
                p = -PitchSensitivity * Input.GetAxis("Mouse Y");
                y = YawSensitivity * Input.GetAxis("Mouse X");
            }
        }
        if (_targetPitch + p < PitchZoomLimit)
        {
            //_targetZoom = Mathf.Clamp(originalZoom - (PitchZoomLimit - (_targetPitch + p))/PitchLowerLimit, ZoomLowerLimit, ZoomUpperLimit);

            float zoomLerpFactor = Mathf.Clamp((ZoomAcceleration * Time.deltaTime) / 0.018f, 0.01f, 1.0f);
            _actualZoom = Mathf.Lerp(_actualZoom, ZoomLowerLimit + (ZoomUpperLimit - ZoomLowerLimit) * Mathf.Pow((_targetPitch - PitchLowerLimit) / (PitchZoomLimit - PitchLowerLimit),2.4f), zoomLerpFactor);
        }
        else
        {
            float zoomLerpFactor = Mathf.Clamp((ZoomAcceleration * Time.deltaTime) / 0.018f, 0.01f, 1.0f);
            _actualZoom = Mathf.Lerp(_actualZoom, ZoomUpperLimit, zoomLerpFactor);
        }
        
        _targetPitch = Mathf.Clamp(_targetPitch + p, PitchLowerLimit, PitchUpperLimit);
        _targetYaw += y;

        float orbitLerpFactor = Mathf.Clamp((OrbitalAcceleration * Time.deltaTime) / 0.018f, 0.01f, 1.0f);
        _actualPitch = Mathf.Lerp(_actualPitch, _targetPitch, orbitLerpFactor);
        _actualYaw = Mathf.Lerp(_actualYaw, _targetYaw, orbitLerpFactor);
    }

    public void LateUpdate()
    {
        // Construct orientation of the camera
        var t = transform;
        t.localPosition = FollowTransform == null ? Vector3.zero : FollowTransform.position + offset;
        t.localRotation = Quaternion.identity;

        var up = t.up;
        t.localRotation = Quaternion.Euler(_actualPitch, 0.0f, 0.0f);
        t.RotateAround(FollowTransform == null ? Vector3.zero : FollowTransform.position + offset, up, _actualYaw);
        t.localPosition = (up * 0.5f + t.forward * -_actualZoom) + t.localPosition;
    }


}
