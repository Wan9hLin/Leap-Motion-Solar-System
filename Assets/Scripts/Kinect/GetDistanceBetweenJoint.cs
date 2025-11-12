using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GetDistanceBetweenJoint : MonoBehaviour
{
    [Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
    public int playerIndex = 0;

    [Tooltip("Right hand joint.")]
    public KinectInterop.JointType rightHand = KinectInterop.JointType.HandRight;

    [Tooltip("Left hand joint.")]
    public KinectInterop.JointType leftHand = KinectInterop.JointType.HandLeft;


    [Tooltip("RightHand position in Kinect coordinates (meters).")]
    public Vector3 rightHandPosition;

    [Tooltip("LeftHand position in Kinect coordinates (meters).")]
    public Vector3 leftHandPosition;

    [Tooltip("Whether we save the joint data to a CSV file or not.")]
    public bool isSaving = false;

    [Tooltip("Path to the CSV file, we want to save the joint data to.")]
    public string saveFilePath = "joint_pos.csv";

    [Tooltip("How many seconds to save data to the CSV file, or 0 to save non-stop.")]
    public float secondsToSave = 0f;

    public float leftRightHandDistance = -1;


    // start time of data saving to csv file
    private float saveStartTime = -1f;

    void Start()
    {
        if (isSaving && File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
    }


    void Update()
    {
        if (isSaving)
        {
            // create the file, if needed
            if (!File.Exists(saveFilePath))
            {
                using (StreamWriter writer = File.CreateText(saveFilePath))
                {
                    // csv file header
                    string sLine = "time,joint,pos_x,pos_y,poz_z";
                    writer.WriteLine(sLine);
                }
            }

            // check the start time
            if (saveStartTime < 0f)
            {
                saveStartTime = Time.time;
            }
        }

        // get the joint position
        KinectManager manager = KinectManager.Instance;

        if (manager && manager.IsInitialized())
        {
            if (manager.IsUserDetected(playerIndex))
            {
                long userId = manager.GetUserIdByIndex(playerIndex);

                if (manager.IsJointTracked(userId, (int)rightHand))
                {
                    // output the joint position for easy tracking
                    Vector3 jointPos = manager.GetJointPosition(userId, (int)rightHand);
                    rightHandPosition = jointPos;

                    if (isSaving)
                    {
                        if ((secondsToSave == 0f) || ((Time.time - saveStartTime) <= secondsToSave))
                        {
#if !UNITY_WSA
                            using (StreamWriter writer = File.AppendText(saveFilePath))
                            {
                                string sLine = string.Format("{0:F3},{1},{2:F3},{3:F3},{4:F3}", Time.time, ((KinectInterop.JointType)rightHand).ToString(), jointPos.x, jointPos.y, jointPos.z);
                                writer.WriteLine(sLine);
                            }
#else
							string sLine = string.Format("{0:F3},{1},{2:F3},{3:F3},{4:F3}", Time.time, ((KinectInterop.JointType)joint).ToString(), jointPos.x, jointPos.y, jointPos.z);
							Debug.Log(sLine);
#endif
                        }
                    }
                }

                if (manager.IsJointTracked(userId, (int)leftHand))
                {
                    // output the joint position for easy tracking
                    Vector3 jointPos = manager.GetJointPosition(userId, (int)leftHand);
                    leftHandPosition = jointPos;

                    if (isSaving)
                    {
                        if ((secondsToSave == 0f) || ((Time.time - saveStartTime) <= secondsToSave))
                        {
#if !UNITY_WSA
                            using (StreamWriter writer = File.AppendText(saveFilePath))
                            {
                                string sLine = string.Format("{0:F3},{1},{2:F3},{3:F3},{4:F3}", Time.time, ((KinectInterop.JointType)leftHand).ToString(), jointPos.x, jointPos.y, jointPos.z);
                                writer.WriteLine(sLine);
                            }
#else
							string sLine = string.Format("{0:F3},{1},{2:F3},{3:F3},{4:F3}", Time.time, ((KinectInterop.JointType)joint).ToString(), jointPos.x, jointPos.y, jointPos.z);
							Debug.Log(sLine);
#endif
                        }
                    }
                }

                if (manager.IsJointTracked(userId, (int)leftHand) && manager.IsJointTracked(userId, (int)rightHand))
                {
                    leftRightHandDistance = Vector3.Distance(leftHandPosition, rightHandPosition);
                    //Debug.Log("Hand distance: " + leftRightHandDistance);
                }
                else
                {
                    leftRightHandDistance = -1;
                }    
            }
        }

    }

}
