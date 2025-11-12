using Leap;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GetDistanceBetweenJoint_2 : MonoBehaviour
{

    [Tooltip("Leap Provider that provides the current frame data.")]
    public LeapProvider leapProvider;

    [Tooltip("Left hand position in Leap Motion coordinates (meters).")]
    public Vector3 leftHandPosition;

    [Tooltip("Right hand position in Leap Motion coordinates (meters).")]
    public Vector3 rightHandPosition;

    [Tooltip("Distance between left and right hands.")]
    public float leftRightHandDistance = -1;

    [Tooltip("Whether we save the hand distance data to a CSV file or not.")]
    public bool isSaving = false;

    [Tooltip("Path to the CSV file where we want to save the hand distance data.")]
    public string saveFilePath = "hand_distance.csv";

    [Tooltip("How many seconds to save data to the CSV file, or 0 to save non-stop.")]
    public float secondsToSave = 0f;

    // start time of data saving to the CSV file
    private float saveStartTime = -1f;

    private Hand leftHand;
    private Hand rightHand;

    void Start()
    {
        if (isSaving && File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
    }

    void Update()
    {
        // Get the hand data from Leap Motion
        GetHandDistanceFromLeap();

        // Save the data if required
        if (isSaving)
        {
            SaveHandDistanceToFile();
        }
    }

    private void GetHandDistanceFromLeap()
    {
        Frame frame = leapProvider.CurrentFrame;
        if (frame != null)
        {
            // Get the left and right hands from the current frame
            leftHand = frame.Hands.Find(h => h.IsLeft);
            rightHand = frame.Hands.Find(h => h.IsRight);

            if (leftHand != null && rightHand != null)
            {
                // Directly assign PalmPosition (which is already a Vector3)
                leftHandPosition = leftHand.PalmPosition;
                rightHandPosition = rightHand.PalmPosition;

                // Calculate the distance between the hands
                leftRightHandDistance = Vector3.Distance(leftHandPosition, rightHandPosition);
                // Debug.Log("Left and right hand distance: " + leftRightHandDistance);
            }
            else
            {
                // If either hand is missing, reset the distance
                leftRightHandDistance = -1;
            }
        }
    }

    private void SaveHandDistanceToFile()
    {
        if (!File.Exists(saveFilePath))
        {
            using (StreamWriter writer = File.CreateText(saveFilePath))
            {
                // Write CSV file header
                string header = "time,leftHandPos_x,leftHandPos_y,leftHandPos_z,rightHandPos_x,rightHandPos_y,rightHandPos_z,distance";
                writer.WriteLine(header);
            }
        }

        if (saveStartTime < 0f)
        {
            saveStartTime = Time.time;
        }

        if (secondsToSave == 0f || (Time.time - saveStartTime) <= secondsToSave)
        {
            using (StreamWriter writer = File.AppendText(saveFilePath))
            {
                string line = string.Format("{0:F3},{1:F3},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3}",
                    Time.time,
                    leftHandPosition.x, leftHandPosition.y, leftHandPosition.z,
                    rightHandPosition.x, rightHandPosition.y, rightHandPosition.z,
                    leftRightHandDistance);
                writer.WriteLine(line);
            }
        }
    }
}
