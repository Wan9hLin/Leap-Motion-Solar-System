using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractionInputModule_leap : MonoBehaviour
{
    public LeapProvider leapProvider; // Leap Motion input
    public PlanetsManager_leap planetsManager; 

    public Hand leftHand;
    public Hand rightHand;

    public RectTransform cursor;
    public Button backButton; 

    private bool isFocusMode = false;  
    private Coroutine progressCoroutine;  // Coroutine to save the progress bar
    private float pinchThreshold = 0.73f; 
    private float grabThreshold = 0.5f;  
    public bool cursorDisabledForFocus = false;

    private Vector3 lastHandPos;  
    private bool rightHandDetected = false;  
    private float cursorSpeedMultiplier = 2.6f;  // Cursor movement magnification factor


    void Update()
    {
        GetHandData(); 

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (rightHand == null && !isFocusMode)
        {
            planetsManager.FocusOff(); 
            planetsManager.HidePlanetInfo();

            cursor.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);  

            return;
        }

        if (!cursorDisabledForFocus) 
        {
            UpdateCursorPosition();
        }

        if (rightHand != null && rightHand.PinchStrength > pinchThreshold && rightHand.GrabStrength < grabThreshold)
        {
            if (progressCoroutine == null)
            {
                HandleSelection(); 
            }
        }

        else
        {
            // If the gesture is lost, reset the progress bar
            if (progressCoroutine != null)
            {
                StopCoroutine(progressCoroutine);
                progressCoroutine = null;
                StartCoroutine(ResetProgressBarSmoothly());
            }

            CameraController_2.Instance.SetNowAsLastIdleTime();
        }

    }

    private void HandleSelection()
    {
        if (!isFocusMode)
        {
            Vector3 screenPos = cursor.position;
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, raycastResults);

            bool isBackButton = false;

            // Check if the back button is selected
            foreach (RaycastResult result in raycastResults)
            {
                if (result.gameObject == backButton.gameObject)
                {
                    isBackButton = true;
                    break;
                }
            }

            // Trigger the appropriate action based on the selected objec
            if (isBackButton)
            {
                progressCoroutine = StartCoroutine(StartBackButtonSelection()); 
            }
            else
            {
                progressCoroutine = StartCoroutine(StartPlanetSelection());
            }
        }
        else
        {
            progressCoroutine = StartCoroutine(StartBackButtonSelection());
        }
    }

    private void GetHandData()
    {
        // Get current hand data from Leap Motion
        Frame frame = leapProvider.CurrentFrame;
        if (frame != null)
        {
            leftHand = frame.GetHand(Chirality.Left);
            rightHand = frame.GetHand(Chirality.Right);
        }
    }

    private void UpdateCursorPosition()
    {
        // Fix cursor at screen center if right hand is not detected
        if (rightHand == null)
        {
            if (rightHandDetected)
            {
                cursor.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                rightHandDetected = false;
            }
            return;
        }

        rightHandDetected = true;

        Vector3 handWorldPos = rightHand.PalmPosition;

        Vector3 screenPos = planetsManager.sceneCamera.WorldToScreenPoint(handWorldPos);

        Vector3 handDelta = screenPos - lastHandPos;

        Vector3 smoothedHandPos = Vector3.Lerp(lastHandPos, screenPos, Time.deltaTime * 4f);

        // Amplify the hand movement increments by the magnification factor
        Vector3 cursorMovement = handDelta * cursorSpeedMultiplier;

        cursor.position = Vector3.Lerp(cursor.position, cursor.position + cursorMovement, Time.deltaTime * 10f);

        lastHandPos = smoothedHandPos;

        //Limit the cursor to the screen size
        cursor.position = new Vector3(
            Mathf.Clamp(cursor.position.x, 0, Screen.width),
            Mathf.Clamp(cursor.position.y, 0, Screen.height),
            cursor.position.z
        );
    }


    private IEnumerator StartPlanetSelection()
    {
        // Raycast to detect selected planet and focus on it
        Vector3 screenPos = cursor.position;
        Ray ray = planetsManager.sceneCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10000, planetsManager.planetLayerMask))
        {
            if (hit.collider.gameObject.CompareTag("Celestial"))
            {
                planetsManager.cursorProgressIMG.enabled = true;
                planetsManager.progressBarOn = true;

                float progress = 0f;
                while (progress < 1f)
                {
                    progress += Time.deltaTime / 0.1f; 
                    planetsManager.cursorProgressIMG.fillAmount = progress;

                    yield return null;
                }

                planetsManager.selectedObject = hit.collider.gameObject;
                planetsManager.FocusOnSelectedPlanet();
                isFocusMode = true; 

                CameraController_2.Instance.EnterFocusMode();
                planetsManager.cursorProgressIMG.enabled = false;

                progressCoroutine = null;
            }
        }
    }


    private IEnumerator StartBackButtonSelection()
    {
        // Handle back button selection and transition back to initial state
        Vector3 screenPos = cursor.position;
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject == backButton.gameObject)
            {             
                planetsManager.cursorProgressIMG.enabled = true;
                planetsManager.progressBarOn = true;

                float progress = 0f;
                float duration = isFocusMode ? 0.05f : 0.5f;  

                while (progress < 1f)
                {
                    progress += Time.deltaTime / duration;  // Smooth progress bar
                    planetsManager.cursorProgressIMG.fillAmount = progress;

                    yield return null;
                }

                // Reset camera position if not in focus mode
                if (!isFocusMode)
                {
                    yield return planetsManager.ResetCameraToInitialPositionCoroutine();
                }

                planetsManager.FocusOff();
                isFocusMode = false;  

                CameraController_2.Instance.ExitFocusMode();
                planetsManager.cursorProgressIMG.enabled = false;

                backButton.interactable = true;

                progressCoroutine = null;
            }
        }
    }

    private IEnumerator ResetProgressBarSmoothly()
    {
        // Smoothly reset the progress bar when the selection is canceled
        float progress = planetsManager.cursorProgressIMG.fillAmount;
        while (progress > 0)
        {
            progress -= Time.deltaTime / 2f; 
            planetsManager.cursorProgressIMG.fillAmount = progress;
            yield return null;
        }
        planetsManager.cursorProgressIMG.enabled = false;
    }

    public IEnumerator DisableAndRecenterCursor()
    {
        // Disable cursor and recenter it after focus mode
        cursor.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        cursorDisabledForFocus = true;
        yield return new WaitForSeconds(1.5f); 
    }

  
    public void EnableCursorAfterFocus()
    {
        cursorDisabledForFocus = false;
    }

    public void ActiveInteracBtn()
    {
        backButton.interactable = true;
    }


}






