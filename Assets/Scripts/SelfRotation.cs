using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfRotation : MonoBehaviour
{
    public GameObject parentObject;
    public float rotSpeed = 30f;
    public bool isRotateClockwise;

    [SerializeField]
    Vector3 initialRotation = Vector3.zero;

    private Quaternion currentRot;
    private IEnumerator rotResetCRT;

    private bool isResetting;

    private float storedRotSpeed;

    private void Start()
    {
        transform.localRotation = Quaternion.Euler(initialRotation);

        parentObject = gameObject.transform.parent.gameObject;

        if (isRotateClockwise)
        {
            rotSpeed *= -1;
        }

        storedRotSpeed = rotSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        transform.localRotation *= Quaternion.AngleAxis(-rotSpeed * Time.deltaTime, Vector3.up);
    }

    private void FixedUpdate()
    {
        if (Quaternion.Angle(parentObject.transform.localRotation, Quaternion.Euler(0,0,0)) >= 0.5 && !isResetting)
        {
            currentRot = parentObject.transform.localRotation;
            isResetting = true;
            StartCoroutine(ResetRotation(5));
        }
    }

    public void StopRotation()
    {
        rotSpeed = 0;
    }

    public void RestartRotation()
    {
        rotSpeed = storedRotSpeed;
    }

    IEnumerator ResetRotation(float resetCoolDown)
    {

        float time = 0;
        while (time < resetCoolDown)
        {
            time += Time.deltaTime;
            yield return null;
        }

        if (currentRot == parentObject.transform.localRotation)
        {
            ResetTranslation();
        }
        else
        {
            isResetting = false;
        }
    }

    public void ResetTranslation()
    {
        if (rotResetCRT != null)
        {
            StopCoroutine(rotResetCRT);
            isResetting = false;
        }

        rotResetCRT = LerpRotation(Quaternion.Euler(0,0,0), 4);
        StartCoroutine(rotResetCRT);
    }

    IEnumerator LerpRotation(Quaternion endValue, float duration)
    {
        isResetting = true;
        float time = 0;
        Quaternion startValue = parentObject.transform.localRotation;
        while (time < duration)
        {
            parentObject.transform.localRotation = Quaternion.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        isResetting = false;
        parentObject.transform.localRotation = endValue;
    }
}
