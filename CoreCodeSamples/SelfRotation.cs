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

        // If rotating clockwise, negate the rotation speed
        if (isRotateClockwise)
        {
            rotSpeed *= -1;
        }

        storedRotSpeed = rotSpeed; // Store the initial rotation speed
    }
   
    private void Update()
    {
        // Continuously rotate the planet in the specified direction
        transform.localRotation *= Quaternion.AngleAxis(-rotSpeed * Time.deltaTime, Vector3.up);
    }

    private void FixedUpdate()
    {
        // If the parent object¡¯s rotation is significantly different from the default, start the reset process
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

    // Coroutine to reset rotation with a cooldown period
    IEnumerator ResetRotation(float resetCoolDown)
    {

        float time = 0;
        while (time < resetCoolDown)
        {
            time += Time.deltaTime;
            yield return null;
        }

        // If the rotation hasn't changed, proceed with translation reset
        if (currentRot == parentObject.transform.localRotation)
        {
            ResetTranslation();
        }
        else
        {
            isResetting = false;
        }
    }

    // Reset the translation (position and rotation) of the parent object
    public void ResetTranslation()
    {
        if (rotResetCRT != null)
        {
            StopCoroutine(rotResetCRT);
            isResetting = false;
        }

        // Start a smooth lerp transition to the initial rotation
        rotResetCRT = LerpRotation(Quaternion.Euler(0,0,0), 4);
        StartCoroutine(rotResetCRT);
    }

    // Smoothly interpolate (lerp) between the current rotation and the target rotation
    // over the specified duration
    IEnumerator LerpRotation(Quaternion endValue, float duration)
    {
        isResetting = true;
        float time = 0;
        Quaternion startValue = parentObject.transform.localRotation;

        // Perform the interpolation over time
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
