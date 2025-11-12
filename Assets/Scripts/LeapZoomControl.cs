using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;

public class LeapZoomControl : MonoBehaviour
{
    public LeapProvider leapProvider;
    public Camera mainCamera;
    public float zoomSpeed = 0.01f;
    private float lastPinchDistance = 0f;

    private void OnEnable()
    {
        leapProvider.OnUpdateFrame += OnUpdateFrame;
    }

    private void OnDisable()
    {
        leapProvider.OnUpdateFrame -= OnUpdateFrame;
    }

    void OnUpdateFrame(Frame frame)
    {
        Hand leftHand = frame.GetHand(Chirality.Left);
        if (leftHand != null)
        {
            HandlePinch(leftHand);
        }
    }

    void HandlePinch(Hand hand)
    {
        float pinchDistance = hand.PinchDistance;
        if (pinchDistance != lastPinchDistance)
        {
            float pinchChange = pinchDistance - lastPinchDistance;
            ZoomCamera(pinchChange);
        }
        lastPinchDistance = pinchDistance;
    }

    void ZoomCamera(float pinchChange)
    {
        mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView - pinchChange * zoomSpeed, 20f, 180f);
    }
}
