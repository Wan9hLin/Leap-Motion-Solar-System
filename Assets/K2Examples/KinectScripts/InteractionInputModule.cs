using Leap;
using System;
using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using UnityEngine.UI;

/// <summary>
/// Interaction IM is the input module that may be used as component of the Unity-UI EventSystem.
/// </summary>
public class InteractionInputModule : PointerInputModule
{
    [Tooltip("Whether to process the hand cursor movements (i.e for hovering ui-elements), or not.")]
    public bool processCursorMovement = true;

    [Tooltip("Reference to the PlanetsManager script.")]
    public PlanetsManager_2 planetsManager;

    private Vector3 handCursorPos = Vector3.zero;
    private Vector2 lastCursorPos = Vector2.zero;

    private PointerEventData.FramePressState framePressState = PointerEventData.FramePressState.NotChanged;
    private readonly MouseState mouseState = new MouseState();

    private bool progressBarOn = false;

    // Leap Motion
    [Header("Leap Motion")]
    public LeapProvider leapProvider;
    private Hand leftHand;
    private Hand rightHand;

    private static InteractionInputModule instance;

    public static InteractionInputModule Instance
    {
        get
        {
            return instance;
        }
    }

    protected InteractionInputModule()
    {
        instance = this;
    }

    public override bool IsModuleSupported()
    {
        return leapProvider != null;
    }

    public override bool ShouldActivateModule()
    {
        if (!base.ShouldActivateModule())
            return false;

        if (leapProvider == null)
        {
            Debug.LogError("LeapProvider is not set!");
            return false;
        }

        // Activate if there is a hand detected or if the state has changed
        Frame frame = leapProvider.CurrentFrame;
        if (frame != null && (frame.Hands.Count > 0 || framePressState != PointerEventData.FramePressState.NotChanged))
        {
            return true;
        }

        return false;
    }

    public override void Process()
    {
        GetHandGestureFromLeap();
        ProcessInteractionEvent();
    }

    private void GetHandGestureFromLeap()
    {
        Frame frame = leapProvider.CurrentFrame;
        if (frame != null)
        {
            leftHand = frame.GetHand(Chirality.Left);
            rightHand = frame.GetHand(Chirality.Right);

            Hand activeHand = leftHand ?? rightHand;

            if (activeHand != null)
            {
                // 获取手的世界坐标
                Vector3 leapHandPos = new Vector3(activeHand.PalmPosition.x, activeHand.PalmPosition.y, activeHand.PalmPosition.z);

                // 使用 Camera.main 将手的世界坐标转换为屏幕坐标
                handCursorPos = Camera.main.WorldToScreenPoint(leapHandPos);

                // Debugging: 打印手部坐标以确保转换正确
                Debug.Log($"Hand Screen Position: {handCursorPos}");

                // Check for pinch to trigger click events
                if (activeHand.PinchStrength > 0.9f)
                {
                    HandleClick();
                }

                // Update UI hover or other cursor movement if enabled
                if (processCursorMovement)
                {
                    CheckCursorPositionChange();
                }
            }
        }
    }


    private void CheckCursorPositionChange()
    {
        Vector2 screenHandPos = new Vector2(handCursorPos.x * Screen.width, handCursorPos.y * Screen.height);

        if (screenHandPos != lastCursorPos)
        {
            lastCursorPos = screenHandPos;
            framePressState = PointerEventData.FramePressState.Pressed;
        }
    }

    private void HandleClick()
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = new Vector2(handCursorPos.x * Screen.width, handCursorPos.y * Screen.height);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);

        if (results.Count > 0)
        {
            GameObject clickedObject = results[0].gameObject;
            ExecuteEvents.Execute(clickedObject, pointerEventData, ExecuteEvents.pointerClickHandler);
        }
    }

    protected void ProcessInteractionEvent()
    {
        // Emulate mouse data
        var mouseData = GetMousePointerEventData(0);
        var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

        // Process the interaction data
        ProcessHandPressRelease(leftButtonData);
        ProcessMove(leftButtonData.buttonData);
        ProcessDrag(leftButtonData.buttonData);
    }

    protected override MouseState GetMousePointerEventData(int id)
    {
        // Populate the left button...
        PointerEventData leftData;
        var created = GetPointerData(kMouseLeftId, out leftData, true);

        leftData.Reset();

        Vector2 handPos = new Vector2(handCursorPos.x * Screen.width, handCursorPos.y * Screen.height);

        if (created)
        {
            leftData.position = handPos;
        }

        leftData.delta = handPos - leftData.position;
        leftData.position = handPos;
        leftData.button = PointerEventData.InputButton.Left;

        eventSystem.RaycastAll(leftData, m_RaycastResultCache);
        var raycast = FindFirstRaycast(m_RaycastResultCache);

        if (m_RaycastResultCache.Count > 0)
        {
            foreach (RaycastResult result in m_RaycastResultCache)
            {
                if (result.gameObject.CompareTag("Button"))
                {
                    progressBarOn = true;
                    planetsManager.SetProgressBar(true);
                    planetsManager.canClick = false;
                }
                else
                {
                    planetsManager.canClick = true;
                }
            }
        }
        else
        {
            if (progressBarOn)
            {
                progressBarOn = false;
                planetsManager.SetProgressBar(false);
            }
        }

        leftData.pointerCurrentRaycast = raycast;
        m_RaycastResultCache.Clear();

        mouseState.SetButtonState(PointerEventData.InputButton.Left, framePressState, leftData);
        framePressState = PointerEventData.FramePressState.NotChanged;

        return mouseState;
    }

    protected void ProcessHandPressRelease(MouseButtonEventData data)
    {
        var pointerEvent = data.buttonData;
        var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

        if (data.PressedThisFrame())
        {
            pointerEvent.eligibleForClick = true;
            pointerEvent.delta = Vector2.zero;
            pointerEvent.dragging = false;
            pointerEvent.useDragThreshold = true;
            pointerEvent.pressPosition = pointerEvent.position;
            pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

            DeselectIfSelectionChanged(currentOverGo, pointerEvent);

            var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            float time = Time.unscaledTime;

            if (newPressed == pointerEvent.lastPress)
            {
                var diffTime = time - pointerEvent.clickTime;
                if (diffTime < 0.3f)
                    ++pointerEvent.clickCount;
                else
                    pointerEvent.clickCount = 1;

                pointerEvent.clickTime = time;
            }
            else
            {
                pointerEvent.clickCount = 1;
            }

            pointerEvent.pointerPress = newPressed;
            pointerEvent.rawPointerPress = currentOverGo;

            pointerEvent.clickTime = time;

            pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

            if (pointerEvent.pointerDrag != null)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
        }

        if (data.ReleasedThisFrame())
        {
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            if (pointerEvent.pointerPress != null && pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
            }
            else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
            {
                ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
            }

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;

            if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

            pointerEvent.dragging = false;
            pointerEvent.pointerDrag = null;

            if (currentOverGo != pointerEvent.pointerEnter)
            {
                HandlePointerExitAndEnter(pointerEvent, null);
                HandlePointerExitAndEnter(pointerEvent, currentOverGo);
            }
        }
    }
}



