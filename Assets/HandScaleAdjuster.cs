using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandScaleAdjuster : MonoBehaviour
{
    public HandModelBase handModel;  // 这是Leap Motion手部模型的引用
    public float handScaleFactor = 0.5f;  // 缩小手的比例，默认值为0.5

    void Start()
    {
        // 设置手部模型的缩放比例
        if (handModel != null)
        {
            handModel.transform.localScale = new Vector3(handScaleFactor, handScaleFactor, handScaleFactor);
        }
    }
}
