using System.Collections;
using System.Collections.Generic;
using TMPro;
using Leap;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlanetsManager_2 : MonoBehaviour
{
    #region Leap Motion
    [Header("Leap Motion")]
    public LeapProvider leapProvider;
    private Hand leftHand;
    private Hand rightHand;

    [Tooltip("Smooth factor used for object rotation.")]
    public float smoothFactor = 3.0f;

    [Tooltip("Progress bar GameObject that needs to toggle on and off.")]
    public Image cursorProgressIMG;
    #endregion

    #region Camera
    [Header("Camera")]
    public Transform target;
    public Camera sceneCamera;

    [Tooltip("Box collider of the camera manager. Toggle on and off while camera focused or unfocused.")]
    public BoxCollider camManagerBoxCollider;

    [Tooltip("The rotate movement speed by hand. Default 60.")]
    public float camRotSpeed = 60;

    public float editorFOVSensitivity = 5f;

    public CameraController_2 cameraCon;

    public GameObject ResetCameraZoomButton;

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
    private CameraController_2 cameraController;
    #endregion

    #region Coroutine
    private IEnumerator focusCRT;
    private IEnumerator resetCRT;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        // Hide cursor
        Cursor.visible = false;
        cursorProgressIMG.enabled = false;

        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
        }

        SetCamInitialRotationLocation();

        planetUIAnim = planetUI.GetComponent<Animator>();
        planetUI.SetActive(false);

        guidanceText.text = mainMenuGuide;

        AudioManager.instance.Play("BGM");

        distanceBetweenCameraAndTarget = Vector3.Distance(sceneCamera.transform.position, target.position);
        initDistanceBetweenCameraAndTarget = distanceBetweenCameraAndTarget;
    }

    void Update()
    {
        GetLeapData();

        if (isZoomed && !cameraCon.enteredZooming)
        {
            ManageObjectInteraction();
        }
    }

    private void GetLeapData()
    {
        Frame frame = leapProvider.CurrentFrame;
        if (frame != null)
        {
            leftHand = frame.GetHand(Chirality.Left);
            rightHand = frame.GetHand(Chirality.Right);

            if (leftHand != null && rightHand != null)
            {
                if (leftHand.GrabStrength > 0.9f && rightHand.GrabStrength <= 0.9f)
                {
                    RotatePlanet(leftHand);
                }
                else if (rightHand.GrabStrength > 0.9f && leftHand.GrabStrength <= 0.9f)
                {
                    ZoomPlanet(rightHand);
                }
            }
        }
    }

    private void RotatePlanet(Hand hand)
    {
        if (zoomedObject == null) return;

        Vector3 handDelta = hand.PalmPosition - previousHandPos;

        float angleArounfY = handDelta.x * 360f;
        float angleArounfX = handDelta.y * 360f;

        Vector3 vObjectRotation = new Vector3(-angleArounfX, -angleArounfY, 0);

        Quaternion qObjectRotation = sceneCamera.transform.rotation * Quaternion.Euler(vObjectRotation);
        zoomedObject.transform.rotation = Quaternion.Slerp(zoomedObject.transform.rotation, qObjectRotation, smoothFactor * Time.deltaTime);

        previousHandPos = hand.PalmPosition;
    }

    private void ZoomPlanet(Hand hand)
    {
        if (zoomedObject == null) return;

        float zoomFactor = hand.PalmPosition.z * editorFOVSensitivity;
        zoomedObject.transform.localScale += Vector3.one * zoomFactor * Time.deltaTime;
    }

    private void ManageObjectInteraction()
    {
        Vector3 screenPixelPos = Vector3.zero;
        if (selectedObject == null)
        {
            RaycastHit hit;
            Ray ray = sceneCamera.ScreenPointToRay(screenPixelPos);
            if (Physics.Raycast(ray, out hit, 10000, planetLayerMask))
            {
                selectedObject = hit.collider.gameObject;
                zoomedObject = selectedObject;

                ResetCameraZoomButton.SetActive(false);
                SetPlanetUI(zoomedObject);
                StartCoroutine(planetUITimer(true));
            }
        }
    }
    public void SetProgressBar(bool isEnabled)
    {
        if (cursorProgressIMG != null)
        {
            cursorProgressIMG.enabled = isEnabled;
        }
    }

    public void SetPlanetUI(GameObject planet)
    {
        planet.GetComponent<PlanetInfoManager>().SetPlanetInfo();
    }

    private void SetCamInitialRotationLocation()
    {
        distanceBetweenCameraAndTarget = Vector3.Distance(sceneCamera.transform.position, target.position);
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
        while (time < 1.1) // animation length
        {
            time += Time.deltaTime;
            yield return null;
        }

        if (!bOn)
        {
            planetUI.SetActive(false);
        }
    }
}



