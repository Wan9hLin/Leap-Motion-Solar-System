using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    public static float FloatClampedRemap(float input_min, float input_max, float output_min, float output_max, float value)
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
}
