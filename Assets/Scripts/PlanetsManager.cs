using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static KinectGestures;

public class PlanetsManager : MonoBehaviour, InteractionListenerInterface, KinectGestures.GestureListenerInterface
{
    #region Kinect
    [Header("Kinect")]
    [Tooltip("Index of the player, tracked by the respective InteractionManager. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
    public int playerIndex = 0;

    [Tooltip("Whether the left hand interaction is allowed by the respective InteractionManager.")]
    public bool leftHandInteraction = true;

    [Tooltip("Whether the right hand interaction is allowed by the respective InteractionManager.")]
    public bool rightHandInteraction = true;

    [Tooltip("Smooth factor used for object rotation.")]
    public float smoothFactor = 3.0f;

    [Tooltip("Progress bar GameObject that needs to toggle on and off.")]
    public Image cursorProgressIMG;

    public KinectInterop.JointType headJoint = KinectInterop.JointType.Head;
    public KinectInterop.JointType leftHandJoint = KinectInterop.JointType.HandLeft;
    public KinectInterop.JointType rightHandJoint = KinectInterop.JointType.HandRight;

    private Vector3 headPos = Vector3.zero;
    private Vector3 rightHandPos = Vector3.zero;
    private Vector3 leftHandPos = Vector3.zero;
    private float leftHandHeadDistance = -1;
    private float rightHandHeadDistance = -1;

    //private bool isLeftHandDrag = false;
    //private InteractionManager.HandEventType lastHandEvent = InteractionManager.HandEventType.None;
    private Vector3 screenPixelPos = Vector3.zero;
    private Vector3 screenNormalPos = Vector3.zero;

    private InteractionManager interactionManager;

    //private bool isLeftHandDrag = false;
    private InteractionManager.HandEventType lastHandEvent = InteractionManager.HandEventType.None;

    public bool isUsingRightHandGrip;
    public bool isUsingLeftHandGrip;
    #endregion

    #region Camera
    [Header("Camera")]
    public Transform target;
    public Camera sceneCamera;

    [Tooltip("Box collider of the camera manager. Toggle on and off while camera focused or unfocused.")]
    public BoxCollider camManagerBoxCollider;

    //public float camSmoothFactor = 3f;

    [Tooltip("The rotate movement speed by right hand. Default 60.")]
    public float camRotSpeed = 60;

/*    [Tooltip("Smaller positive value means smoother rotation, 1 means no smooth apply.")]
    public float slerpSmoothValue = 0.3f;
    [Tooltip("How long the smoothDamp of the mouse scroll takes.")]
    public float scrollSmoothTime = 0.12f;*/

    public float editorFOVSensitivity = 5f;
/*
    [Range(1f, 15f)]
    [Tooltip("How sensitive the mouse drag to camera rotation.")]
    public float mouseRotateSpeed = 1f;
*/
    public CameraController cameraCon;

    public GameObject ResetCameraZoomButton;

    public bool isPortrait = false;

    public Vector3 portraitLowerPos = Vector3.zero;
    public Vector3 portraitMaxLowerPos = Vector3.zero;
    public Vector3 portraitMinPos = Vector3.zero;
    private float portraitLowerPosChangeVelocity = 0;

    private GameObject portraitCameraParent;

    private Vector3 camInitLocation;
    private Quaternion camInitRotation;
    private Vector3 _camLocation;
    private Quaternion _camRotation;

    private Vector3 offset;

    private Quaternion tempQ;
    private bool grabbedObject = false;

    //Mouse rotation related
    private float rotX; // around x
    private float rotY; // around y
    private float rotMultiplier = 1;

    //Clamp Value
    private float minXRotAngle = -0.65f; //min angle around x axis
    private float maxXRotAngle = 0.65f; // max angle around x axis

    private Vector3 direction;
    private Vector3 smoothVelocity = Vector3.zero;

    private Vector3 previousHandPos = Vector3.zero;

    public float distanceBetweenCameraAndTarget;
    public float initDistanceBetweenCameraAndTarget;

    #endregion

    #region General
    [Header("General")]
    [HideInInspector]
    public GameObject selectedObject;
    [HideInInspector]
    public GameObject zoomedObject;

    [Tooltip("Layer mask for interactable planet.")]
    public LayerMask planetLayerMask;

    [HideInInspector]
    public bool isZoomed = false;
    [HideInInspector]
    public bool isZooming = false;

    public int idleFocusOffTime = 5;

    public GameObject planetUI;
    public GameObject crossHair;

    public GameObject userStatus;

    public TextMeshProUGUI guidanceText;
    public string mainMenuGuide;
    public string zoomMenuGuide;

    private Animator planetUIAnim;

    private bool progressBarOn = false;

    public bool canClick = true;
    #endregion

    #region Reference Script
    [Header("Scripts")]
    [SerializeField]
    private GetDistanceBetweenJoint handDistanceManager;
    // reference to the gesture listener
    private ModelGestureListener gestureListener;
    #endregion

    #region Coroutine
    private IEnumerator focusCRT;
    private IEnumerator resetCRT;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        //hide mouser
        Cursor.visible = false;
        cursorProgressIMG.enabled = false;

        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
        }

        if (interactionManager == null)
        {
            //interactionManager = InteractionManager.Instance;
            interactionManager = GetInteractionManager();
        }

        SetCamInitialRotationLocation();

        planetUIAnim = planetUI.GetComponent<Animator>();

        planetUI.SetActive(false);

        guidanceText.text = mainMenuGuide;

        AudioManager.instance.Play("BGM");

        distanceBetweenCameraAndTarget = Vector3.Distance(sceneCamera.transform.position, target.position);
        initDistanceBetweenCameraAndTarget = distanceBetweenCameraAndTarget;

        // get the gestures listener
        gestureListener = ModelGestureListener.Instance;
    }

    private InteractionManager GetInteractionManager()
    {
        // find the proper interaction manager
        MonoBehaviour[] monoScripts = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];

        foreach (MonoBehaviour monoScript in monoScripts)
        {
            if ((monoScript is InteractionManager) && monoScript.enabled)
            {
                InteractionManager manager = (InteractionManager)monoScript;

                if (manager.playerIndex == playerIndex && manager.leftHandInteraction == leftHandInteraction && manager.rightHandInteraction == rightHandInteraction)
                {
                    return manager;
                }
            }
        }

        // not found
        return null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            FocusOff();
        }

        KinectManager manager = KinectManager.Instance;

        if (manager && manager.IsInitialized())
        {
            if (manager.IsUserDetected(playerIndex))
            {
                long userId = manager.GetUserIdByIndex(playerIndex);

                if (manager.IsJointTracked(userId, (int)headJoint))
                {
                    Vector3 pos = manager.GetJointPosition(userId, (int)headJoint);
                    headPos = pos;
                }

                if (manager.IsJointTracked(userId, (int)leftHandJoint))
                {
                    Vector3 pos = manager.GetJointPosition(userId, (int)leftHandJoint);
                    leftHandPos = pos;
                }

                if (manager.IsJointTracked(userId, (int)rightHandJoint))
                {
                    Vector3 pos = manager.GetJointPosition(userId, (int)rightHandJoint);
                    rightHandPos = pos;
                }

                if (manager.IsJointTracked(userId, (int)headJoint) && (manager.IsJointTracked(userId, (int)leftHandJoint) || manager.IsJointTracked(userId, (int)rightHandJoint)))
                {
                    leftHandHeadDistance = leftHandPos.y - headPos.y;
                    rightHandHeadDistance = rightHandPos.y - headPos.y;
                    //Debug.Log("Left: " + leftHandHeadDistance + " Right: " + rightHandHeadDistance);
                }

                if (cursorProgressIMG != null)
                {
                    screenNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

                    screenPixelPos.x = (int)(screenNormalPos.x * (sceneCamera ? sceneCamera.pixelWidth : Screen.width));
                    screenPixelPos.y = (int)(screenNormalPos.y * (sceneCamera ? sceneCamera.pixelHeight : Screen.height));
                    Ray ray = sceneCamera ? sceneCamera.ScreenPointToRay(screenPixelPos) : new Ray();

                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 10000, planetLayerMask))
                    {
                        if (hit.collider.gameObject.CompareTag("Celestial"))
                        {
                            progressBarOn = true;
                            cursorProgressIMG.enabled = true;
                        }
                    }
                    else
                    {
                        if (progressBarOn)
                        {
                            progressBarOn = false;
                            cursorProgressIMG.enabled = false;
                        }
                    }
                }
            }
        }

        if (interactionManager != null && interactionManager.IsInteractionInited() && isZoomed && !cameraCon.enteredZooming)
        {
            Vector3 screenPixelPos = Vector3.zero;
            if (selectedObject == null)
            {
                // no object is currently selected or dragged.
                //(leftHandInteraction && interactionManager.IsLeftHandPrimary()) ||

                bool bRightHandIntAllowed = (rightHandInteraction && isUsingRightHandGrip && (interactionManager.IsRightHandPrimary() || interactionManager.IsLeftHandPrimary()));
                bool bLeftHandIntAllowed = (leftHandInteraction && isUsingLeftHandGrip && (interactionManager.IsRightHandPrimary() || interactionManager.IsLeftHandPrimary()));

                // check if there is an underlying object to be selected
                if (lastHandEvent == InteractionManager.HandEventType.Grip && bLeftHandIntAllowed && !bRightHandIntAllowed)
                {
                    selectedObject = zoomedObject;

                    if (zoomedObject != null && zoomedObject.name != "AsteroidCollider")
                    {
                        zoomedObject.transform.GetChild(0).GetComponent<SelfRotation>().StopRotation();
                    }
                }
                else if (lastHandEvent == InteractionManager.HandEventType.Grip && bRightHandIntAllowed && !bLeftHandIntAllowed)
                {
                    selectedObject = zoomedObject;

                    previousHandPos = screenNormalPos;
                }

                /*//might need to take away, will cause incidently to reset when zooming
                if (leftHandState == KinectInterop.HandState.Closed && rightHandState == KinectInterop.HandState.Closed && zoomedObject != null && handDistanceManager.leftRightHandDistance >= .8f)
                {
                    FocusOff();
                }*/

                if (gestureListener != null)
                {
                    if ((leftHandHeadDistance >= 0.1 && rightHandHeadDistance >= 0.1) && (cameraCon.leftHandState != KinectInterop.HandState.Closed && cameraCon.rightHandState != KinectInterop.HandState.Closed))
                    {
                        isZoomed = false;
                        FocusOff();
                    }

                    /*if ((gestureListener.IsRaiseHand() || gestureListener.IsSwipeUp()) && (cameraCon.leftHandState != KinectInterop.HandState.Closed && cameraCon.rightHandState != KinectInterop.HandState.Closed))
                    {
                        if (leftHandHeadDistance >= 0.1 && rightHandHeadDistance >= 0.1)
                        {
                            FocusOff();
                        }
                    }*/
                }
            }
            else
            {
                bool bRightHandIntAllowed = (rightHandInteraction && interactionManager.IsRightHandPrimary());
                bool bLeftHandIntAllowed = (leftHandInteraction && interactionManager.IsLeftHandPrimary());

                if (bLeftHandIntAllowed && !bRightHandIntAllowed && zoomedObject.name != "AsteroidCollider")
                {
                    // continue dragging the object
                    screenNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

                    float angleArounfY = screenNormalPos.x * 360f;  // horizontal rotation
                    float angleArounfX = screenNormalPos.y * 360f;  // vertical rotation

                    Vector3 vObjectRotation = new Vector3(-angleArounfX, -angleArounfY, 180);

                    Quaternion qObjectRotation = sceneCamera ? sceneCamera.transform.rotation * Quaternion.Euler(vObjectRotation) : Quaternion.Euler(vObjectRotation);
                    tempQ = qObjectRotation;

                    /*if (selectedObject.GetComponent<Rigidbody>() != null)
                    {
                        MoveRotationTorque(selectedObject.GetComponent<Rigidbody>(), qObjectRotation);
                    }
                    else
                    {
                        selectedObject.transform.rotation = Quaternion.Slerp(selectedObject.transform.rotation, qObjectRotation, smoothFactor * Time.deltaTime);
                    }*/

                    if (selectedObject.GetComponent<Rigidbody>() != null)
                    {
                        grabbedObject = true;
                    }
                    else
                    {
                        selectedObject.transform.rotation = Quaternion.Slerp(selectedObject.transform.rotation, qObjectRotation, smoothFactor * Time.deltaTime);
                    }

                    // check if the object (hand grip) was released
                    bool isReleased = lastHandEvent == InteractionManager.HandEventType.Release;

                    if (isReleased)
                    {
                        if (zoomedObject != null)
                        {
                            zoomedObject.transform.GetChild(0).GetComponent<SelfRotation>().RestartRotation();
                        }

                        grabbedObject = false;

                        selectedObject = null;
                    }
                }
                else if (bRightHandIntAllowed && !bLeftHandIntAllowed)
                {
                    screenNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

                    Vector3 currentHandPos = previousHandPos - screenNormalPos;

                    direction = Vector3.SmoothDamp(direction, currentHandPos, ref smoothVelocity, 10 * Time.deltaTime);

                    rotY = direction.x * camRotSpeed;  // horizontal rotation
                    rotX = direction.y * camRotSpeed;

                    /*rotX += Input.GetAxis("Mouse Y") * mouseRotateSpeed; // around X
                    rotY += Input.GetAxis("Mouse X") * mouseRotateSpeed;*/

                    //Vector3 vObjectRotation = new Vector3(-rotX, -rotY, 180);

                    //Quaternion qObjectRotation = sceneCamera ? sceneCamera.transform.rotation * Quaternion.Euler(vObjectRotation) : Quaternion.Euler(vObjectRotation);

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

    private void FixedUpdate()
    {
        if (grabbedObject && selectedObject != null)
        {
            MoveRotationTorque(selectedObject.GetComponent<Rigidbody>(), tempQ);
        }
    }

    private void LateUpdate()
    {
        if (zoomedObject != null)
        {
            if (isPortrait && portraitCameraParent != null)
            {
                portraitCameraParent.transform.position = zoomedObject.transform.position;
            }

            if (zoomedObject.name != "Sun")
            {
                if (!isPortrait)
                {
                    //sceneCamera.transform.position = Vector3.SmoothDamp(sceneCamera.transform.position, targetPosition, ref camMoveVelocity, camSmoothFactor);

                    // update rotation
                    sceneCamera.transform.LookAt(zoomedObject.transform);

                    //Vector3 targetPosition = zoomedObject.transform.position + offset;
                    if ((zoomedObject.transform.position - sceneCamera.transform.position).magnitude > offset.magnitude)
                    {
                        float chaseSpeed = (zoomedObject.transform.position - sceneCamera.transform.position).magnitude - offset.magnitude;
                        sceneCamera.transform.position += sceneCamera.transform.forward * (1 + chaseSpeed) * Time.deltaTime;
                    }
                }
            }

            target.transform.position = zoomedObject.transform.position;

            /*if (isPortrait)
            {
                Vector3 targetPOS = target.transform.position;
                targetPOS.y = portraitLowerPos.y;
                target.transform.position = targetPOS;

                sceneCamera.transform.LookAt(target.transform);
            }*/
        }

        if (isZoomed && isUsingRightHandGrip)
        {
            if (selectedObject != null)
            {
                RotateCamera();
            }
        }
    }

    public void SetProgressBar(bool bOn)
    {
        cursorProgressIMG.enabled = bOn;
    }

    private void RotateCamera()
    {
        if (sceneCamera.transform.rotation.x > maxXRotAngle - 0.02)
        {
            rotMultiplier = ClampedRemap(maxXRotAngle - 0.2f, maxXRotAngle, -1, -0.5f, Mathf.Abs(sceneCamera.transform.rotation.x));
        }
        else if (sceneCamera.transform.rotation.x < minXRotAngle + 0.02)
        {
            rotMultiplier = ClampedRemap(minXRotAngle + 0.2f, minXRotAngle, -1, -0.5f, Mathf.Abs(sceneCamera.transform.rotation.x));
        }
        else
        {
            rotMultiplier = 1;
        }

        /*if (sceneCamera.transform.rotation.x >= 0.1f || sceneCamera.transform.rotation.x <= -0.1f)
        {
            float targetPortraitPos = ClampedRemap(0.15f, 0.5f, portraitMinPos.y, portraitMaxLowerPos.y, Mathf.Abs(sceneCamera.transform.rotation.x));
            portraitLowerPos.y = Mathf.SmoothDamp(portraitLowerPos.y, targetPortraitPos, ref portraitLowerPosChangeVelocity, Time.deltaTime);
        }
        else
        {
            portraitLowerPos.y = Mathf.SmoothDamp(portraitLowerPos.y, portraitMinPos.y, ref portraitLowerPosChangeVelocity, Time.deltaTime);
        }*/

        if (isPortrait)
        {
            if (portraitCameraParent.transform.rotation.x > maxXRotAngle || portraitCameraParent.transform.rotation.x < minXRotAngle)
            {
                rotX = 0;
            }
            else 
            {
                if (portraitCameraParent != null)
                {
                    portraitCameraParent.transform.Rotate(new Vector3(1, 0, 0), rotX * Mathf.Abs(rotMultiplier));
                    portraitCameraParent.transform.Rotate(new Vector3(0, -1, 0), rotY, Space.World);
                }
            }

            if (portraitCameraParent.transform.rotation.x > maxXRotAngle)
            {
                portraitCameraParent.transform.Rotate(new Vector3(-1f, 0, 0), 0.22f);
            }
            else if (portraitCameraParent.transform.rotation.x < minXRotAngle)
            {
                portraitCameraParent.transform.Rotate(new Vector3(1f, 0, 0), 0.22f);
            }
        }
        else
        {
            if (sceneCamera.transform.rotation.x > maxXRotAngle || sceneCamera.transform.rotation.x < minXRotAngle)
            {
                rotX = 0;
            }
            else
            {
                sceneCamera.transform.Rotate(new Vector3(1, 0, 0), rotX * Mathf.Abs(rotMultiplier));
                sceneCamera.transform.Rotate(new Vector3(0, -1, 0), rotY, Space.World);
            }

            if (sceneCamera.transform.rotation.x > maxXRotAngle)
            {
                sceneCamera.transform.Rotate(new Vector3(-1f, 0, 0), 0.22f);
            }
            else if (sceneCamera.transform.rotation.x < minXRotAngle)
            {
                sceneCamera.transform.Rotate(new Vector3(1f, 0, 0), 0.22f);
            }

            sceneCamera.transform.position = target.position - (sceneCamera.transform.forward * distanceBetweenCameraAndTarget);

            sceneCamera.transform.LookAt(target.transform.position);
        }

        previousHandPos = screenNormalPos;
    }

    private IEnumerator FocusOnCamera(Camera cam, float duration)
    {
        isZooming = true;

        //cameraCon.CallFOVReset(40);

        AudioManager.instance.Play("Woosh");

        camManagerBoxCollider.enabled = false;

        crossHair.SetActive(false);

        if (zoomedObject.GetComponent<PlanetInfoManager>().planet.isSmaller)
        {
            cam.TrySetCameraDistance(0.5f);
        }
        else
        {
            cam.TrySetCameraDistance(1.25f);
        }

        float time = 0;
        while (time < 0.11f)
        {
            time += Time.deltaTime;
            yield return null;
        }

        time = 0;

        if (cam.TryGetFocusTransforms(zoomedObject, out var targetPosition, out var targetRotation))
        {
            _camLocation = targetPosition;
            _camRotation = targetRotation;
        }

        guidanceText.text = zoomMenuGuide;

        while (time < duration)
        {
            cam.transform.position = Vector3.Lerp(cam.transform.position, _camLocation, 5 * Time.deltaTime);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, _camRotation, 5 * Time.deltaTime);
            time += Time.deltaTime;
            yield return null;
        }
        offset = sceneCamera.transform.position - zoomedObject.transform.position;
        distanceBetweenCameraAndTarget = Vector3.Distance(sceneCamera.transform.position, target.position);
        
        isZoomed = true;

        isZooming = false;

        if (isPortrait)
        {
            portraitCameraParent = new GameObject("CameraParent");
            portraitCameraParent.transform.position = zoomedObject.transform.position;
            sceneCamera.transform.LookAt(portraitCameraParent.transform);
            portraitCameraParent.transform.LookAt(sceneCamera.transform);
            sceneCamera.transform.parent = portraitCameraParent.transform;

            sceneCamera.transform.Rotate(new Vector3(1, 0, 0), 6);
        }
    }

    public void FocusOff()
    {
        ResetCameraZoomButton.SetActive(false);

        cameraCon.CallFOVReset(40);

        guidanceText.text = mainMenuGuide;

        crossHair.SetActive(true);

        if (focusCRT != null)
        {
            StopCoroutine(focusCRT);
        }
        focusCRT = FocusOffCamera(sceneCamera, 2);
        StartCoroutine(focusCRT);

        if (zoomedObject != null && zoomedObject.name != "AsteroidCollider")
        {
            zoomedObject.transform.GetChild(0).GetComponent<SelfRotation>().RestartRotation();
        }

        selectedObject = null;
        zoomedObject = null;

        cameraCon.gripPos = Vector3.zero;
        target.transform.localPosition = Vector3.zero;

        StartCoroutine(planetUITimer(false));
    }

    private IEnumerator planetUITimer(bool bOn)
    {
        if (bOn)
        {
            planetUI.SetActive(true);
            planetUIAnim.Play("Base Layer.OnAnimation");
        }
        else
        {
            planetUIAnim.Play("Base Layer.OffAnimation");
        }

        float time = 0;
        while (time < 1.1) //animation length
        {
            time += Time.deltaTime;
            yield return null;
        }

        if (!bOn)
        {
            planetUI.SetActive(false);
        }
    }

    public void SetCamInitialRotationLocation()
    {
        camInitLocation = sceneCamera.transform.position;
        camInitRotation = sceneCamera.transform.rotation;
    }

    public void MoveRotationTorque(Rigidbody rigidbody, Quaternion targetRotation)
    {
        rigidbody.maxAngularVelocity = 1000;

        Quaternion rotation = targetRotation * Quaternion.Inverse(rigidbody.rotation);
        rigidbody.AddTorque(rotation.x / Time.fixedDeltaTime, rotation.y / Time.fixedDeltaTime, rotation.z / Time.fixedDeltaTime, ForceMode.VelocityChange);
        rigidbody.angularVelocity = Vector3.zero;
    }

    private IEnumerator FocusOffCamera(Camera cam, float duration)
    {
        isZooming = true;

        AudioManager.instance.Play("Woosh");
        cam.TrySetCameraDistance(1.25f);
        distanceBetweenCameraAndTarget = initDistanceBetweenCameraAndTarget;

        float time = 0;
        while (time < duration)
        {
            cam.transform.position = Vector3.Lerp(cam.transform.position, camInitLocation, 5 * Time.deltaTime);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, camInitRotation, 5 * Time.deltaTime);
            time += Time.deltaTime;
            yield return null;
        }

        isZoomed = false;

        camManagerBoxCollider.enabled = true;

        isZooming = false;

        sceneCamera.transform.parent = null;
        Destroy(portraitCameraParent, 1);
    }

    private IEnumerator ClickTimer()
    {
        canClick = false;
        float time = 0;
        while (time < 0.5f)
        {
            time += Time.deltaTime;
            yield return null;
        }
        canClick = true;
    }

    private IEnumerator ResetTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (isZoomed)
        {
            FocusOff();
        }
    }

    public void HandGripDetected(long userId, int userIndex, bool isRightHand, bool isHandInteracting, Vector3 handScreenPos)
    {
        if (!isHandInteracting || !interactionManager)
            return;
        if (userId != interactionManager.GetUserID())
            return;

        if (isRightHand)
        {
            if (isHandInteracting)
            {
                isUsingRightHandGrip = true;
            }
            else
            {
                isUsingLeftHandGrip = true;
            }
        }
        else
        {
            if (isHandInteracting)
            {
                isUsingLeftHandGrip = true;
            }
            else
            {
                isUsingRightHandGrip = true;
            }
        }

        lastHandEvent = InteractionManager.HandEventType.Grip;
        //isLeftHandDrag = !isRightHand;
        screenNormalPos = handScreenPos;
    }

    public void HandReleaseDetected(long userId, int userIndex, bool isRightHand, bool isHandInteracting, Vector3 handScreenPos)
    {
        if (!isHandInteracting || !interactionManager)
            return;
        if (userId != interactionManager.GetUserID())
            return;

        if (isRightHand)
        {
            if (isHandInteracting)
            {
                isUsingRightHandGrip = false;
            }
            else
            {
                isUsingLeftHandGrip = false;
            }
        }
        else
        {
            if (isHandInteracting)
            {
                isUsingLeftHandGrip = false;
            }
            else
            {
                isUsingRightHandGrip = false;
            }
        }

        lastHandEvent = InteractionManager.HandEventType.Release;
        //isLeftHandDrag = !isRightHand;
        screenNormalPos = handScreenPos;
    }

    public bool HandClickDetected(long userId, int userIndex, bool isRightHand, Vector3 handScreenPos)
    {
        if ((lastHandEvent != InteractionManager.HandEventType.Grip) && interactionManager != null && interactionManager.IsInteractionInited() && canClick)
        {
            //ray cast hand position
            screenNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

            screenPixelPos.x = (int)(screenNormalPos.x * (sceneCamera ? sceneCamera.pixelWidth : Screen.width));
            screenPixelPos.y = (int)(screenNormalPos.y * (sceneCamera ? sceneCamera.pixelHeight : Screen.height));
            Ray ray = sceneCamera ? sceneCamera.ScreenPointToRay(screenPixelPos) : new Ray();

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 10000, planetLayerMask))
            {
                if (hit.collider.gameObject.CompareTag("Celestial"))
                {
                    if (hit.collider.gameObject != zoomedObject)
                    {
                        selectedObject = hit.collider.gameObject;
                        zoomedObject = selectedObject;

                        ResetCameraZoomButton.SetActive(false);
                        SetPlanetUI(zoomedObject);

                        StartCoroutine(planetUITimer(true));

                        StartCoroutine(ClickTimer());
                    }
                }
            }

            if (selectedObject != null)
            {
                if (focusCRT != null)
                {
                    StopCoroutine(focusCRT);
                }

                selectedObject = null;

                focusCRT = FocusOnCamera(sceneCamera, 2);
                StartCoroutine(focusCRT);

                isZoomed = false;
            }
        }
        return true;
    }

    public void SetPlanetUI(GameObject planet)
    {
        planet.GetComponent<PlanetInfoManager>().SetPlanetInfo();
    }

    float ClampedRemap(float input_min, float input_max, float output_min, float output_max, float value)
    {
        if (value < input_min)
        {
            return output_min;
        }
        else if (value > input_max)
        {
            return output_max;
        }
        else
        {
            return (value - input_min) / (input_max - input_min) * (output_max - output_min) + output_min;
        }
    }

    public void UserDetected(long userId, int userIndex)
    {
        if (resetCRT != null)
        {
            StopCoroutine(resetCRT);
        }

        if (userStatus.activeSelf)
        {
            userStatus.SetActive(false);
        }

        return;
    }

    public void UserLost(long userId, int userIndex)
    {
        if (resetCRT != null)
        {
            StopCoroutine(resetCRT);
        }

        resetCRT = ResetTimer(idleFocusOffTime);
        StartCoroutine(resetCRT);

        if (!userStatus.activeSelf)
        {
            userStatus.SetActive(true);
        }

        return;
    }

    public void GestureInProgress(long userId, int userIndex, KinectGestures.Gestures gesture, float progress, KinectInterop.JointType joint, Vector3 screenPos)
    {
        return;
    }

    public bool GestureCompleted(long userId, int userIndex, KinectGestures.Gestures gesture, KinectInterop.JointType joint, Vector3 screenPos)
    {
        if (userIndex != playerIndex)
            return false;

        return true;
    }

    public bool GestureCancelled(long userId, int userIndex, KinectGestures.Gestures gesture, KinectInterop.JointType joint)
    {
        return true;
    }
}
