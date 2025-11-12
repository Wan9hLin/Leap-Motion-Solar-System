using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlanetsManager_leap : MonoBehaviour
{
    [Header("Camera")]
    public Transform cameraTransform;
    public Camera sceneCamera;

    [Header("Planet Selection")]
    public LayerMask planetLayerMask;  
    public GameObject selectedObject;  
    public GameObject planetInfoUI;    
    public Image cursorProgressIMG;


    // Planet-related variables
    private bool isZoomed = false;
    private bool isZooming = false;
    private bool isFollowingPlanet = false;  
    public bool progressBarOn = false;

    private Vector3 originalPosition;  

    private float focusDistance = 15f;  

    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;

    public InteractionInputModule_leap interactionInputModule;
    private SelfRotation selfRotationScript;

    private bool isRotatingPlanet = false;
    private Vector3 initialHandPos;


    void Start()
    {
        initialCameraPosition = cameraTransform.position;
        initialCameraRotation = cameraTransform.rotation;

        planetInfoUI.SetActive(false);
        cursorProgressIMG.enabled = false;
        originalPosition = cameraTransform.position; 

        AudioManager.instance.Play("BGM");
    }

    void Update()
    {
        // In focus mode and following a planet, update the camera position and orientation
        if (isFollowingPlanet && selectedObject != null)
        {
            FollowPlanet();
        }

        // Detect left hand grip to rotate the planet in focus mode
        if (isZoomed && selectedObject != null)
        {
            RotateSelectedPlanet();
        }
    }

    public void FocusOnSelectedPlanet()
    {
        // Disable cursor updates before entering focus mode to prevent jitter
        interactionInputModule.cursorDisabledForFocus = true;

        originalPosition = cameraTransform.position;

        if (selectedObject != null)
        {
            StartCoroutine(SmoothFocusOnPlanet(selectedObject.transform.position));

            selfRotationScript = selectedObject.GetComponentInChildren<SelfRotation>();
          
            // Update camera target
            CameraController_2.Instance.target.position = selectedObject.transform.position;

            PlanetInfoManager planetInfoManager = selectedObject.GetComponent<PlanetInfoManager>();
            if (planetInfoManager != null)
            {
                planetInfoManager.SetPlanetInfo();
                planetInfoUI.SetActive(true); 
            }
            else
            {
                Debug.LogWarning("Selected object does not have a PlanetInfoManager component.");
            }

            AudioManager.instance.Play("Woosh");
        }
    }


    private IEnumerator SmoothFocusOnPlanet(Vector3 targetPosition)
    {
        isZooming = true;

        float elapsedTime = 0f;
        float focusDuration = 1.5f;  // focus time
        Vector3 initialPosition = cameraTransform.position;

        // offset based on planet radius
        float planetRadius = 1.0f;  
        Collider planetCollider = selectedObject.GetComponent<Collider>();

        if (planetCollider != null)
        {
            planetRadius = planetCollider.bounds.extents.magnitude; 
        }

        Vector3 directionToPlanet = (targetPosition - initialPosition).normalized;
        Vector3 adjustedTargetPosition = targetPosition - directionToPlanet * (planetRadius + focusDistance);


        // Move the camera smoothly to the target position
        while (elapsedTime < focusDuration)
        {
            cameraTransform.position = Vector3.Lerp(initialPosition, adjustedTargetPosition, elapsedTime / focusDuration);
            cameraTransform.LookAt(targetPosition);

            // Disable and reset the cursor to the center of the screen
            StartCoroutine(interactionInputModule.DisableAndRecenterCursor());

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isZoomed = true;
        isZooming = false;
        isFollowingPlanet = true;

        // Enable cursor
        interactionInputModule.EnableCursorAfterFocus();
    }

    private void FollowPlanet()
    {
        if (selectedObject != null)
        {
            Vector3 targetPosition = selectedObject.transform.position;
            float planetRadius = 1.0f;  
            Collider planetCollider = selectedObject.GetComponent<Collider>();

            if (planetCollider != null)
            {
                planetRadius = planetCollider.bounds.extents.magnitude;
            }

            Vector3 directionToPlanet = (targetPosition - cameraTransform.position).normalized;
            Vector3 adjustedTargetPosition = targetPosition - directionToPlanet * (planetRadius + focusDistance);

            // Smoothly adjust camera to keep planet centered
            cameraTransform.position = adjustedTargetPosition;
            cameraTransform.LookAt(targetPosition);

            CameraController_2.Instance.target.position = targetPosition;
        }
    }

    public void FocusOff()
    {
        if (isZoomed)
        {
           
            isFollowingPlanet = false;
            isRotatingPlanet = false;

            // Stop planet's self-rotation when unfocusing
            if (selfRotationScript != null)
            {
                selfRotationScript.RestartRotation();
                selfRotationScript = null; 
            }

            originalPosition = cameraTransform.position;

            StartCoroutine(SmoothMoveBackToSuitableDistance());

            planetInfoUI.SetActive(false);
            AudioManager.instance.Play("Woosh");

        }

    }

    private IEnumerator SmoothMoveBackToSuitableDistance()
    {
        Vector3 initialPosition = cameraTransform.position; 
        float suitableDistance = 50f; // Suitable camera distance after exit
        float duration = 1.8f;  
        float elapsedTime = 0f;

        // Calculate the distance between the current camera and the planet
        float currentDistance = Vector3.Distance(cameraTransform.position, selectedObject.transform.position);
        Vector3 directionToPlanet = (cameraTransform.position - selectedObject.transform.position).normalized;

        // Determine the target position based on the suitable distance
        Vector3 targetPosition = selectedObject.transform.position + directionToPlanet * suitableDistance;

        // Smoothly move the camera back
        while (elapsedTime < duration)
        {
            cameraTransform.position = Vector3.Lerp(initialPosition, targetPosition, elapsedTime / duration);
            cameraTransform.LookAt(selectedObject.transform.position);  
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cameraTransform.position = targetPosition;
        isZoomed = false;
    }

    public void ShowPlanetInfo(GameObject planet)
    {
        planetInfoUI.SetActive(true);
    }

    public void HidePlanetInfo()
    {
        planetInfoUI.SetActive(false);
    }

    public IEnumerator ResetCameraToInitialPositionCoroutine()
    {
        yield return StartCoroutine(SmoothMoveToInitialPosition());
    }

    private IEnumerator SmoothMoveToInitialPosition()
    {
        float elapsedTime = 0f;
        float duration = 1.5f; // Transition duration

        Vector3 currentPos = cameraTransform.position;
        Quaternion currentRot = cameraTransform.rotation;

        while (elapsedTime < duration)
        {
            cameraTransform.position = Vector3.Lerp(currentPos, initialCameraPosition, elapsedTime / duration);
            cameraTransform.rotation = Quaternion.Slerp(currentRot, initialCameraRotation, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cameraTransform.position = initialCameraPosition;
        cameraTransform.rotation = initialCameraRotation;

        // Update camera target
        CameraController_2.Instance.RaycastAndUpdateCameraTarget();
       
    }

   


    private void RotateSelectedPlanet()
    {
        if (CameraController_2.Instance.IsZooming)
        {
            DisablePlanetRotation(); // Disable rotation if zooming
            return;
        }

        if (interactionInputModule.leftHand != null && interactionInputModule.leftHand.GrabStrength > 0.8f)
        {
            if (!isRotatingPlanet)
            {
                // Start rotating planet when hand is gripping
                initialHandPos = interactionInputModule.leftHand.PalmPosition;
                isRotatingPlanet = true;

                if (selfRotationScript != null)
                {
                    selfRotationScript.StopRotation();
                }
            }
            else
            {           
                Vector3 currentHandPos = interactionInputModule.leftHand.PalmPosition;
    
                Vector3 handDelta = currentHandPos - initialHandPos;
               
                float movementThreshold = 0.002f; // Sensitivity threshold

                if (handDelta.magnitude > movementThreshold)
                {
                    
                    Vector3 cameraRelativeHandDelta = cameraTransform.InverseTransformDirection(handDelta);

                    Rigidbody planetRigidbody = selectedObject.GetComponent<Rigidbody>();
                    if (planetRigidbody != null)
                    {
                       
                        float torqueMultiplier = 2800f; // Sensitivity of rotation

                        Vector3 worldUp = cameraTransform.right;      
                        Vector3 worldRight = cameraTransform.up;

                        // Add torque based on hand movement to rotate the planet
                        Vector3 torque = (worldUp * cameraRelativeHandDelta.y + -worldRight * cameraRelativeHandDelta.x) * torqueMultiplier;
                        planetRigidbody.AddTorque(Vector3.Lerp(Vector3.zero, torque, 0.5f), ForceMode.Acceleration);

                        initialHandPos = currentHandPos;
                    }
                }
            }
        }
        else
        {
            if (isRotatingPlanet)
            {
                isRotatingPlanet = false;

                if (selfRotationScript != null)
                {
                    selfRotationScript.RestartRotation();
                }
            }
        }
    }

    private void DisablePlanetRotation()
    {
        isRotatingPlanet = false; 

        if (selfRotationScript != null)
        {
            selfRotationScript.RestartRotation();
        }
    }


}

