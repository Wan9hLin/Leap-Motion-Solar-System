using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;


public class LeapRotationControl : MonoBehaviour
{

    public Transform solarSystem;  // 太阳系物体
    public float rotationSpeed = 50.0f;  // 旋转速度
    private Hand hand;
    private float previousHandX;  // 记录手的上一帧的X位置
    private bool isFirstFrame = true;  // 用于初始化上一帧的手位置

    void Update()
    {
        var provider = FindObjectOfType<LeapServiceProvider>();
        if (provider.CurrentFrame.Hands.Count > 0)
        {
            hand = provider.CurrentFrame.Hands[0]; // 使用第一只手

            float currentHandX = hand.PalmPosition.x;  // 获取当前手掌的X轴位置

            if (!isFirstFrame)  // 确保不是第一帧
            {
                float deltaX = currentHandX - previousHandX;  // 计算X轴的移动量

                // 根据手的移动量旋转太阳系，deltaX决定旋转的方向
                solarSystem.transform.Rotate(Vector3.up, -deltaX * rotationSpeed * Time.deltaTime);
            }

            // 更新上一帧的X轴位置
            previousHandX = currentHandX;
            isFirstFrame = false;
        }
        else
        {
            isFirstFrame = true;  // 如果没有手的检测，重置标志位
        }
    }

}
