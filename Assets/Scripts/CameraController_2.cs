using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO.Pipes;
using Unity.VisualScripting;
using System.Threading;

public class CameraController_2 : MonoBehaviour
{
    public static CameraController_2 Instance;

    #region General

    // General setup
    [Header("General")]
    [Tooltip("Center of the camera rotate sphere.")]
    public Transform target;

    [HideInInspector]
    public Vector3 gripPos = Vector3.zero;

    [Tooltip("Main Camera.")]
    private Camera _sceneCamera;
    private Transform _sceneCameraTransform;

    [Tooltip("Reset camera button")]
    public GameObject ResetCameraZoomButton;

    [Tooltip("The drag movement speed by hand. Default 10000.")]
    public float dragSpeed = 10000;
   



    [Tooltip("How long the smoothDamp of the mouse scroll takes.")]
    public float scrollSmoothTime = 0.12f;

    [Tooltip("How fast the camera FOV changes.")]
    public float editorFOVSensitivity = 0.2f;

    [Tooltip("Camera interact layer.")]
    public LayerMask cameraLayerMask;


    private Vector3 dragVelocity = Vector3.zero;

    private bool isZooming = false;
    private bool zoomIn = false;
    private bool zoomOut = false;

    private bool canZoomIn = true;
    private bool canZoomOut = true;
    private bool isMovingManually = false;


    public bool enteredZooming = false;

    private bool canRotate = true;

    private bool isInFocusMode = false; 
    public bool isCameraRotatingOrMoving = false;
    private bool isRotating = false;


    private float _lastIdleTime;

    private float cameraFieldOfView;
    private float initialHandDistance = 0;
    private float zoomFactor = 0;
    private float cameraFOVDamp;
    private float fovChangeVelocity = 0;
  

    private Vector3 initialHandPos;
    private bool isSliding = false;

    private float distanceBetweenCameraAndTarget;




    [SerializeField]
    private float minCameraFieldOfView = 6;
    [SerializeField]
    private float maxCameraFieldOfView = 78;
    #endregion

    #region Leap Motion
    [Header("Leap Motion")]
    public LeapProvider leapProvider;
    private Hand leftHand;
    private Hand rightHand;
    #endregion

    #region Reference Script
    [Header("Scripts")]
    [SerializeField]
    private PlanetsManager_leap planetManager;
    public InteractionInputModule_leap interactionInputModule;
    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        SetNowAsLastIdleTime();
    }

    void Start()
    {

        GetCameraReference();

        if (_sceneCamera == null)
        {
            Debug.LogError("Main Camera is not assigned or found!");
            return; 
        }

        if (target == null)
        {
            Debug.LogError("Target is not assigned!");
            return;
        }

        distanceBetweenCameraAndTarget = Vector3.Distance(_sceneCamera.transform.position, target.position);

        cameraFOVDamp = _sceneCamera.fieldOfView;
        cameraFieldOfView = _sceneCamera.fieldOfView;
    }

    void Update()
    {
        if (!canRotate)  // Disable Rotate in focus mode
            return;

        GetDataFromLeap();
        ZoomInOut();
    }

    private void LateUpdate()
    {
        if (isMovingManually || isCameraRotatingOrMoving)
        {
            // Disable Auto-Rotation
            SetNowAsLastIdleTime();

            isMovingManually = false; //Reset
        }
        else if (!isInFocusMode && !isCameraRotatingOrMoving && IdleCheck())
        {
            // Camera Auto-Rotation
            IdleRotateCamera();
        }

        // Set camera FOV
        SetCameraFOV();
    }


    private void GetDataFromLeap()
    {
        Frame frame = leapProvider.CurrentFrame;
        if (frame != null)
        {
            leftHand = frame.GetHand(Chirality.Left);
            rightHand = frame.GetHand(Chirality.Right);

            if (leftHand != null && rightHand != null)
            {
                //Camera Move
                if (leftHand.GrabStrength > 0.9f && rightHand.GrabStrength <= 0.9f && !isInFocusMode)
                {
                    MoveCamera();  
                }

                // Camera Rotate
                else if (rightHand.GrabStrength > 0.9f && leftHand.GrabStrength <= 0.9f)
                {
                    RotateCamera(); 
                }

                //Reset camera to origin position
                else
                {
                    ResetCameraMove();
                    ResetCameraRotate();
                }
            }
        }
        else
        {
            Debug.LogWarning("No hand data found in the current frame.");
        }
    }


    #region Camera Methods
    public void GetCameraReference()
    {
        if (_sceneCamera == null)
        {
            _sceneCamera = Camera.main;
        }
        if (_sceneCamera != null)
        {
            _sceneCameraTransform = _sceneCamera.transform;
        }
    }

    private void CacheSceneCameraTransform()
    {
        if (_sceneCamera == null || _sceneCameraTransform == null)
        {
            Debug.LogError("Scene Camera or its Transform is not assigned!");
            return;
        }
        _sceneCameraTransform = _sceneCamera.transform;
    }



    private void RotateCamera()
    {
        CacheSceneCameraTransform();

        if (_sceneCameraTransform == null || rightHand == null)
        {
            Debug.LogError("Scene camera transform or right hand data is null. Aborting RotateCamera.");
            return;
        }

        // Check if the right hand's grab is valid
        if (rightHand.GrabStrength <= 0.9f)
        {
            ResetCameraRotate();  // Reset camera rotation state when released
            return;
        }

        // Mark camera as rotating
        isCameraRotatingOrMoving = true;
        isMovingManually = true;
        SetNowAsLastIdleTime();

        Vector3 currentHandScreenPos = _sceneCamera.WorldToScreenPoint(
            new Vector3(rightHand.PalmPosition.x, rightHand.PalmPosition.y, rightHand.PalmPosition.z)
        );


        // record the initial screen position
        if (!isRotating)
        {
            initialHandPos = currentHandScreenPos;
            isRotating = true;
            StartCoroutine(interactionInputModule.DisableAndRecenterCursor());
        }

        Vector3 handDelta = currentHandScreenPos - initialHandPos;

        
        initialHandPos = currentHandScreenPos;

        // Adjust the offset multiplier to control sensitivity of rotation
        float offsetMultiplier = isInFocusMode ? 0.25f : 0.18f; 

        float deltaX = handDelta.x * offsetMultiplier;
        float deltaY = handDelta.y * offsetMultiplier;

        // Create rotation quaternions based on hand movement
        Quaternion horizontalRotation = Quaternion.AngleAxis(-deltaX, Vector3.up);
        Quaternion verticalRotation = Quaternion.AngleAxis(deltaY, _sceneCameraTransform.right);

        _sceneCameraTransform.position = horizontalRotation * verticalRotation * (_sceneCameraTransform.position - target.position) + target.position;
  
        _sceneCameraTransform.LookAt(target);

        SetDistanceBetweenCameraAndTarget();

    }

    // Reset rotation state
    private void ResetCameraRotate()
    {

        isRotating = false;
        isCameraRotatingOrMoving = false;

        // Re-enable the cursor
        interactionInputModule.EnableCursorAfterFocus();
    }

    public void EnterFocusMode()
    {
        isInFocusMode = true;  
        isCameraRotatingOrMoving = false; 
    }

    public void ExitFocusMode()
    {
        isInFocusMode = false;  
        isCameraRotatingOrMoving = false; 

        RaycastAndUpdateCameraTarget();
        SetNowAsLastIdleTime(); 

    }

    private void MoveCamera()
    {
        if (isInFocusMode)
            return;

        CacheSceneCameraTransform();

        if (_sceneCameraTransform == null || leftHand == null)
        {
            Debug.LogError("Scene camera transform or left hand data is null. Aborting MoveCamera.");
            return;
        }
     
        if (leftHand.GrabStrength <= 0.9f)
        {
            ResetCameraMove();
            return;
        }

        isCameraRotatingOrMoving = true;
        isMovingManually = true;
        SetNowAsLastIdleTime();

        Vector3 currentHandScreenPos = _sceneCamera.WorldToScreenPoint(
            new Vector3(leftHand.PalmPosition.x, leftHand.PalmPosition.y, leftHand.PalmPosition.z)
        );

        if (!isSliding)
        {
            initialHandPos = currentHandScreenPos;
            isSliding = true;
            StartCoroutine(interactionInputModule.DisableAndRecenterCursor());
        }

        
        Vector3 handDelta = currentHandScreenPos - initialHandPos;
     
        initialHandPos = currentHandScreenPos;
     
        float offsetMultiplier = 0.07f; 

        Vector3 localMovement = new Vector3(-handDelta.x * offsetMultiplier, 0, -handDelta.y * offsetMultiplier);
       
        Vector3 worldMovement = _sceneCameraTransform.TransformDirection(localMovement);
   
        worldMovement.y = 0;
   
        _sceneCameraTransform.position += worldMovement;

        RaycastAndUpdateCameraTarget();
    }









    public void RaycastAndUpdateCameraTarget()
    {
        // ray from the center of the camera
        Ray centerRay = _sceneCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit centerHit;

        // update target position
        if (Physics.Raycast(centerRay, out centerHit, 10000, cameraLayerMask))
        {
            if (centerHit.collider.gameObject == gameObject)  
            {
                target.position = Vector3.SmoothDamp(target.position, centerHit.point, ref dragVelocity, scrollSmoothTime * Time.smoothDeltaTime);
                Debug.Log("Camera target updated to position: " + centerHit.point);
            }
        }
    }
   
    private void ResetCameraMove()
    {
        isSliding = false;
        isCameraRotatingOrMoving = false; 
    }

    private void IdleRotateCamera()
    {
        _sceneCamera.transform.Rotate(new Vector3(0, -1, 0), 0.008f, Space.World);
        SetDistanceBetweenCameraAndTarget();
    }

    private void SetDistanceBetweenCameraAndTarget()
    {
        _sceneCameraTransform.position = target.position - (_sceneCameraTransform.forward * distanceBetweenCameraAndTarget);
        _sceneCameraTransform.LookAt(target.transform.position);
    }

    private void SetCameraFOV()
    {
        if (cameraFieldOfView <= minCameraFieldOfView)
        {
            cameraFieldOfView = minCameraFieldOfView;
        }
        else if (cameraFieldOfView >= maxCameraFieldOfView)
        {
            cameraFieldOfView = maxCameraFieldOfView;
        }

        cameraFOVDamp = Mathf.SmoothDamp(cameraFOVDamp, cameraFieldOfView, ref fovChangeVelocity, scrollSmoothTime);
        _sceneCamera.fieldOfView = cameraFOVDamp;
    }

    public void CallFOVReset(float FOV)
    {
        StartCoroutine(SetCameraFOVCoroutine(FOV));
    }

    private IEnumerator SetCameraFOVCoroutine(float cameraFoV)
    {
        float time = 0;
        while (time < 0.1f) //animation length
        {
            time += Time.deltaTime;
            cameraFieldOfView = Mathf.SmoothDamp(cameraFieldOfView, cameraFoV, ref fovChangeVelocity, scrollSmoothTime);
        }
        yield return true;
    }
    #endregion

    #region Zoom Function
    private void ZoomInOut()
    {
        if (leftHand != null && rightHand != null)
        {        
            if (leftHand.GrabStrength > 0.9f && rightHand.GrabStrength > 0.9f)
            {
                float handDistance = Vector3.Distance(leftHand.PalmPosition, rightHand.PalmPosition);

               
                if (!isZooming)
                {
                    initialHandDistance = handDistance;
                    isZooming = true;


                }

                if (isZooming)
                {
                    //Stop Auto Rotation
                    SetNowAsLastIdleTime();
                    StartCoroutine(interactionInputModule.DisableAndRecenterCursor());

                    // Zoom in
                    if (handDistance > initialHandDistance + 0.05f && canZoomIn)
                    {
                        zoomIn = true;
                        zoomOut = false;
                    }
                    // Zoom out
                    else if (handDistance < initialHandDistance - 0.05f && canZoomOut)
                    {
                        zoomIn = false;
                        zoomOut = true;
                    }

                    float zoomThreshold = 0.1f;
                    if (Mathf.Abs(handDistance - initialHandDistance) > zoomThreshold)
                    {             
                        if (zoomIn)
                        {
                            zoomFactor = (handDistance - initialHandDistance) * editorFOVSensitivity * 3f; 
                            cameraFieldOfView = Mathf.Clamp(cameraFieldOfView - zoomFactor, minCameraFieldOfView, maxCameraFieldOfView);
                        }
                        else if (zoomOut)
                        {
                            zoomFactor = (initialHandDistance - handDistance) * editorFOVSensitivity * 3f; 
                            cameraFieldOfView = Mathf.Clamp(cameraFieldOfView + zoomFactor, minCameraFieldOfView, maxCameraFieldOfView);
                        }

                        _sceneCamera.fieldOfView = cameraFieldOfView;
                     
                        if (Mathf.Abs(handDistance - initialHandDistance) < 0.02f)
                        {
                            StartCoroutine(ExitZoomingAfterDelay(0.5f));
                        }
                    }
                }
            }
            else
            {            
                if (isZooming)
                {
                    isZooming = false;
                    Debug.Log("Zooming Canceled due to hands not gripping.");


                }
            }
        }
    }

    private IEnumerator ExitZoomingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isZooming = false;  
        Debug.Log("Zooming Finished.");


    }
    #endregion

    public void SetNowAsLastIdleTime()
    {
        _lastIdleTime = Time.time;
    }

    public bool IdleCheck()
    {
        return Time.time - _lastIdleTime > 5;
    }

    public bool IsZooming
    {
        get { return isZooming; }
    }
}