using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO.Pipes;
using Unity.VisualScripting;
using System.Threading;

public class CameraController : MonoBehaviour, InteractionListenerInterface, KinectGestures.GestureListenerInterface
{
    public static CameraController Instance;

    #region General
    //General
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

    [Tooltip("The drag movement speed by left hand. Default 10000.")]
    public float dragSpeed = 10000;
    private float _dragSpeedChangeVelocity = 0;

    [Tooltip("The rotate movement speed by right hand. Default 60.")]
    public float camRotSpeed = 60;

    [Tooltip("How long the smoothDamp of the mouse scroll takes.")]
    public float scrollSmoothTime = 0.12f;

    [Tooltip("How fast the camera FOV changes.")] //before fixing zoom is 0.2f;
    public float editorFOVSensitivity = 0.2f;

    [Tooltip("Camera interact layer.")]
    public LayerMask cameraLayerMask;

    //cursor move direction over time.
    private Vector3 direction;
    private Vector3 smoothVelocity = Vector3.zero;

    private Vector3 dragVelocity = Vector3.zero;

    private bool isZooming = false;
    private bool zoomIn = false;
    private bool zoomOut = false;

    private bool canZoomIn = true;
    private bool canZoomOut = true;

    public bool enteredZooming = false;

    //Can we rotate camera, which means we are not blocking the view
    private bool canRotate = true;

    //Mouse rotation related
    private float rotX; // around x
    private float rotY; // around y
    private float _rotMultiplier = 1;

    //idle time
    public int IdleTimeSetting = 5;
    public int IdleFocusOffTimeSetting = 5;
    private float _lastIdleTime;

    private bool _isIdled = false;

    private bool changedCameraTrasnform = false;

    //Mouse Scroll
    private float cameraFieldOfView;
    private float initialHandDistance = 0;
    private float zoomFactor = 0;
    private float cameraFOVDamp; //Damped value
    private float fovChangeVelocity = 0;

    private float distanceBetweenCameraAndTarget;

    private Vector3 previousHandPos = Vector3.zero;

    private GameObject selectedObject;

    //Clamp Value
    private float minXRotAngle = -0.65f; //min angle around x axis
    private float maxXRotAngle = 0.65f; // max angle around x axis

    [SerializeField]
    private float minCameraFieldOfView = 6;
    [SerializeField]
    private float maxCameraFieldOfView = 78;
    #endregion

    #region Kinect
    [Header("Kinect")]
    [Tooltip("Index of the player, tracked by the respective InteractionManager. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
    public int playerIndex = 0;

    [Tooltip("Whether the left hand interaction is allowed by the respective InteractionManager.")]
    public bool leftHandInteraction = true;

    [Tooltip("Whether the right hand interaction is allowed by the respective InteractionManager.")]
    public bool rightHandInteraction = true;

    //private bool isLeftHandDrag = false;
    private InteractionManager.HandEventType lastHandEvent = InteractionManager.HandEventType.None;
    private Vector3 screenNormalPos = Vector3.zero;

    public KinectInterop.HandState leftHandState;
    public KinectInterop.HandState rightHandState;
    #endregion

    #region Reference Script
    //General
    [Header("Scripts")]
    [SerializeField]
    private PlanetsManager planetManager;
    [SerializeField]
    private GetDistanceBetweenJoint handDistanceManager;

    //Kinect
    [Tooltip("Interaction manager instance, used to detect hand interactions. If left empty, the component will try to find a proper interaction manager in the scene.")]
    private InteractionManager interactionManager;
    private ModelGestureListener gestureListener;
    //private KinectManager manager;
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

    // Start is called before the first frame update
    void Start()
    {
        GetCameraReference();
        GetInteractionManager();
        GetGestureListener();

        distanceBetweenCameraAndTarget = Vector3.Distance(_sceneCamera.transform.position, target.position);

        Vector3 dir = new Vector3(0, 30, -distanceBetweenCameraAndTarget);  //Assign value to the distance between the maincamera and the target
        _sceneCamera.transform.position = target.position + dir;             //Initialize camera position

        cameraFOVDamp = _sceneCamera.fieldOfView;
        cameraFieldOfView = _sceneCamera.fieldOfView;

        RotateCamera();
    }

    private void GetGestureListener()
    {
        if (gestureListener == null)
        {
            gestureListener = ModelGestureListener.Instance;
        }
    }

    private void GetInteractionManager()
    {
        if (interactionManager == null)
        {
            interactionManager = InteractionManager.Instance;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!canRotate)
        {
            return;
        }

        GetDataFromKinect();
        ZoomInOut();
        GrabOrReleaseObject();
    }

    private void LateUpdate()
    {
        //Check if the timer for idle is true and if the planet is not in the focus mode
        if (IdleCheckFocusOff() && !planetManager.isZoomed)
        {
            if (!_isIdled && changedCameraTrasnform)
            {
                SetIdle(true);
                planetManager.FocusOff();
                changedCameraTrasnform = false;
            }
        }

        if (IdleCheck() && !planetManager.isZoomed)
        {
            if (!planetManager.isZooming)
            {
                IdleRotateCamera();
            }
        }

        if (selectedObject != null && !isZooming && !enteredZooming && !planetManager.isZoomed && lastHandEvent == InteractionManager.HandEventType.Grip)
        {
            if (interactionManager.IsRightHandPrimary())
            {
                RotateCamera();
            }
            else if (interactionManager.IsLeftHandPrimary())
            {
                MoveCamera();
            }
        }

        SetCameraFOV();

        if (isZooming && !planetManager.isZoomed && !enteredZooming)
        {
            if (!ResetCameraZoomButton.activeSelf && !planetManager.isZooming)
            {
                ResetCameraZoomButton.SetActive(true);
            }

            target.transform.position = Vector3.Slerp(target.transform.position, gripPos, Time.deltaTime * 5);
            RotateCamera();
        }
    }

    private void GrabOrReleaseObject()
    {
        if (interactionManager != null && interactionManager.IsInteractionInited() && !isZooming && !enteredZooming)
        {
            Vector3 screenPixelPos = Vector3.zero;
            if (selectedObject == null)
            {
                // no object is currently selected or dragged.
                bool bHandIntAllowed = (leftHandInteraction && interactionManager.IsLeftHandPrimary()) || (rightHandInteraction && interactionManager.IsRightHandPrimary());

                // check if there is an underlying object to be selected
                if (lastHandEvent == InteractionManager.HandEventType.Grip && bHandIntAllowed)
                {
                    SetNowAsLastIdleTime();
                    SetIdle(false);

                    changedCameraTrasnform = true;

                    // convert the normalized screen pos to pixel pos
                    screenNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

                    screenPixelPos.x = (int)(screenNormalPos.x * (_sceneCamera ? _sceneCamera.pixelWidth : Screen.width));
                    screenPixelPos.y = (int)(screenNormalPos.y * (_sceneCamera ? _sceneCamera.pixelHeight : Screen.height));

                    Ray ray = _sceneCamera ? _sceneCamera.ScreenPointToRay(screenPixelPos) : new Ray();

                    // check for underlying objects
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, 10000, cameraLayerMask))
                    {
                        if (hit.collider.gameObject == gameObject)
                        {
                            selectedObject = gameObject;

                            SetGripPosition(hit.point);

                            previousHandPos = screenNormalPos;
                        }
                    }

                    //Show the return button while not in focus mode, or not while focusing the planet
                    if (!ResetCameraZoomButton.activeSelf && !planetManager.isZoomed && !planetManager.isZooming)
                    {
                        ResetCameraZoomButton.SetActive(true);
                    }
                }
            }
            else
            {
                bool bHandIntAllowed = (leftHandInteraction && interactionManager.IsLeftHandPrimary()) || (rightHandInteraction && interactionManager.IsRightHandPrimary());

                if (bHandIntAllowed)
                {
                    SetNowAsLastIdleTime();

                    // continue dragging the object
                    screenNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

                    Vector3 currentHandPos = previousHandPos - screenNormalPos;

                    direction = Vector3.SmoothDamp(direction, currentHandPos, ref smoothVelocity, 10 * Time.deltaTime);

                    rotY = direction.x * camRotSpeed;  // horizontal rotation
                    rotX = direction.y * camRotSpeed;  // vertical rotation

                    // check if the object (hand grip) was released
                    bool isReleased = lastHandEvent == InteractionManager.HandEventType.Release;

                    if (isReleased)
                    {
                        selectedObject = null;
                    }
                }
            }
        }
    }

    private void SetGripPosition(Vector3 pos)
    {
        gripPos = pos;
    }

    #region Camera Methods
    public void GetCameraReference()
    {
        if (_sceneCamera == null)
        {
            _sceneCamera = Camera.main;
        }
    }

    private void CacheSceneCameraTransform()
    {
        _sceneCameraTransform = _sceneCamera.transform;
    }

    private void RotateCamera()
    {
        CacheSceneCameraTransform();
        //Calculate here is because the changes is more smooth, even though its for Moving Camera
        CalculateCameraDragSpeed();

        //Multiplier is used below to control how much rotate
        SetAndLimitRotationMultiplier();

        //Rotate the camera
        if (!isZooming && !enteredZooming)
        {
            if (_sceneCameraTransform.rotation.x > maxXRotAngle || _sceneCameraTransform.rotation.x < minXRotAngle)
            {
                rotX = 0;
            }
            else
            {
                _sceneCameraTransform.Rotate(new Vector3(1, 0, 0), rotX * Mathf.Abs(_rotMultiplier));
                _sceneCameraTransform.Rotate(new Vector3(0, -1, 0), rotY, Space.World);
            }
        }

        //Rotate the camera back, its not cool and beutiful
        RotateBackBeforeMaxAngle();

        SetDistanceBetweenCameraAndTarget();
        SetSceneCameraTransformFromCache();

        previousHandPos = screenNormalPos;
    }

    private void RotateBackBeforeMaxAngle()
    {
        if (_sceneCameraTransform.rotation.x > maxXRotAngle)
        {
            _sceneCameraTransform.Rotate(new Vector3(-1f, 0, 0), 0.22f);
        }
        else if (_sceneCameraTransform.rotation.x < minXRotAngle)
        {
            _sceneCameraTransform.Rotate(new Vector3(1f, 0, 0), 0.22f);
        }
    }

    private void SetAndLimitRotationMultiplier()
    {
        if (_sceneCameraTransform.rotation.x > maxXRotAngle - 0.02)
        {
            _rotMultiplier = ExtensionMethods.FloatClampedRemap(maxXRotAngle - 0.2f, maxXRotAngle, -1, -0.5f, Mathf.Abs(_sceneCameraTransform.rotation.x));
        }
        else if (_sceneCameraTransform.rotation.x < minXRotAngle + 0.02)
        {
            _rotMultiplier = ExtensionMethods.FloatClampedRemap(minXRotAngle + 0.2f, minXRotAngle, -1, -0.5f, Mathf.Abs(_sceneCameraTransform.rotation.x));
        }
        else
        {
            _rotMultiplier = 1;
        }
    }

    private void MoveCamera()
    {
        CacheSceneCameraTransform();

        Vector3 pos = _sceneCamera.ScreenToViewportPoint(screenNormalPos - previousHandPos);
        Vector3 move = new Vector3(pos.x * -dragSpeed, 0, pos.y * -dragSpeed);

        //Move the camera
        Vector3 outputMove = Vector3.up * move.z + _sceneCameraTransform.right * move.x;
        _sceneCameraTransform.Translate(outputMove, Space.World);

        RaycastAndMoveCameraTarget();
        SetDistanceBetweenCameraAndTarget();
        SetSceneCameraTransformFromCache();

        previousHandPos = screenNormalPos;
    }

    private void CalculateCameraDragSpeed()
    {
        if (_sceneCameraTransform.rotation.x >= 0.2f || _sceneCameraTransform.rotation.x <= -0.2f)
        {
            //Drag speed is higher when the angle is more higher or lower on X axis
            float targetDragSpeed = ExtensionMethods.FloatClampedRemap(0.25f, 0.65f, 16000, 50000, Mathf.Abs(_sceneCameraTransform.transform.rotation.x));
            dragSpeed = Mathf.SmoothDamp(dragSpeed, targetDragSpeed, ref _dragSpeedChangeVelocity, scrollSmoothTime);
        }
        else
        {
            dragSpeed = Mathf.SmoothDamp(dragSpeed, 16000, ref _dragSpeedChangeVelocity, scrollSmoothTime);
        }
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

    private void SetSceneCameraTransformFromCache()
    {
        _sceneCamera.transform.position = _sceneCameraTransform.position;
        _sceneCamera.transform.rotation = _sceneCameraTransform.rotation;
    }

    private void RaycastAndMoveCameraTarget()
    {
        //Raycast from the center of camera to set the target point
        Ray centerRay = _sceneCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit centerHit;
        if (Physics.Raycast(centerRay, out centerHit, 10000, cameraLayerMask))
        {
            if (centerHit.collider.gameObject == gameObject)
            {
                target.transform.position = Vector3.SmoothDamp(target.transform.position, centerHit.point, ref dragVelocity, scrollSmoothTime * Time.smoothDeltaTime);
            }
        }
    }

    void SetCameraFOV()
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

    #region Kinect Function
    private void GetDataFromKinect()
    {
        KinectManager manager = KinectManager.Instance;

        if (manager && manager.IsInitialized())
        {
            if (manager.IsUserDetected(playerIndex))
            {
                long userId = manager.GetUserIdByIndex(playerIndex);
                GetBothHandsState(manager, userId);
            }
        }
    }

    private void GetBothHandsState(KinectManager manager, long userId)
    {
        leftHandState = GetHandLeftState(manager, userId);
        rightHandState = GetHandRightState(manager, userId);
    }

    private KinectInterop.HandState GetHandLeftState(KinectManager mng, long uid)
    {
        return mng.GetLeftHandState(uid);
    }

    private KinectInterop.HandState GetHandRightState(KinectManager mng, long uid)
    {
        return mng.GetRightHandState(uid);
    }
    #endregion

    #region Idle Function
    private void SetNowAsLastIdleTime()
    {
        _lastIdleTime = Time.time;
    }

    private void SetIdle(bool bIsIdleState)
    {
        _isIdled = bIsIdleState;
    }

    public bool IdleCheck()
    {
        return Time.time - _lastIdleTime > IdleTimeSetting;
    }

    public bool IdleCheckFocusOff()
    {
        return Time.time - _lastIdleTime > IdleFocusOffTimeSetting;
    }
    #endregion

    #region Zoom Function
    private void ZoomInOut()
    {
        if (gestureListener != null)
        {
            if ((leftHandState == KinectInterop.HandState.Closed && 
                rightHandState == KinectInterop.HandState.Closed) && 
                !planetManager.isZooming)
            {
                enteredZooming = true;
            }

            if (enteredZooming)
            {
                if (!isZooming)
                {
                    initialHandDistance = handDistanceManager.leftRightHandDistance;
                }

                isZooming = true;

                //zoom in
                if (handDistanceManager.leftRightHandDistance >= (initialHandDistance + 0.05) && canZoomIn)
                {
                    zoomIn = true;
                    zoomOut = false;
                    isZooming = false;
                    canZoomOut = false;
                }
                //zoom out
                else if (handDistanceManager.leftRightHandDistance <= (initialHandDistance - 0.05) && canZoomOut)
                {
                    zoomIn = false;
                    zoomOut = true;
                    isZooming = false;
                    canZoomIn = false;
                }
                else
                {
                    zoomIn = false;
                    zoomOut = false;
                }

                if (zoomIn && !zoomOut)
                {
                    zoomFactor = ExtensionMethods.FloatClampedRemap(0.2f, 1f, 0.05f, 2f, handDistanceManager.leftRightHandDistance);
                    cameraFieldOfView = (cameraFieldOfView - (zoomFactor * editorFOVSensitivity));
                }
                else if (zoomOut && !zoomIn)
                {
                    zoomFactor = ExtensionMethods.FloatClampedRemap(0.2f, 1f, -2f, -0.05f, handDistanceManager.leftRightHandDistance);
                    cameraFieldOfView = (cameraFieldOfView - (zoomFactor * editorFOVSensitivity));
                }

                return;
            }
            else
            {
                zoomIn = false;
                zoomOut = false;
            }
        }
    }
    #endregion

    public void HandGripDetected(long userId, int userIndex, bool isRightHand, bool isHandInteracting, Vector3 handScreenPos)
    {
        if (!isHandInteracting || !interactionManager)
            return;
        if (userId != interactionManager.GetUserID())
            return;


        lastHandEvent = InteractionManager.HandEventType.Grip;
        screenNormalPos = handScreenPos;
    }

    public void HandReleaseDetected(long userId, int userIndex, bool isRightHand, bool isHandInteracting, Vector3 handScreenPos)
    {
        if (!isHandInteracting || !interactionManager)
            return;
        if (userId != interactionManager.GetUserID())
            return;

        isZooming = false;
        enteredZooming = false;

        canZoomIn = true;
        canZoomOut = true;

        SetGripPosition(target.transform.position);

        lastHandEvent = InteractionManager.HandEventType.Release;
        screenNormalPos = handScreenPos;
    }

    public bool HandClickDetected(long userId, int userIndex, bool isRightHand, Vector3 handScreenPos)
    {
        return true;
    }

    public void UserDetected(long userId, int userIndex)
    {
        return;
    }

    public void UserLost(long userId, int userIndex)
    {
        return;
    }

    public void GestureInProgress(long userId, int userIndex, KinectGestures.Gestures gesture, float progress, KinectInterop.JointType joint, Vector3 screenPos)
    {
        return;
    }

    public bool GestureCompleted(long userId, int userIndex, KinectGestures.Gestures gesture, KinectInterop.JointType joint, Vector3 screenPos)
    {
        return true;
    }

    public bool GestureCancelled(long userId, int userIndex, KinectGestures.Gestures gesture, KinectInterop.JointType joint)
    {
        return true;
    }
}
