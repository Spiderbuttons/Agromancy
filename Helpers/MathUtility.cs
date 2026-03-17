using System;
using Microsoft.Xna.Framework;

namespace Agromancy.Helpers;

public class MathUtility
{
    public static float MultiLerp(float[] values, float progress)
    {
        switch (values.Length)
        {
            case 0:
                throw new ArgumentException("At least one value must be provided.", nameof(values));
            case 1:
                return values[0];
        }

        float delta = 1f / (values.Length - 1);
        int startIndex = (int)(progress / delta);

        if (startIndex == values.Length - 1) {
            return values[^1];
        }

        float localT = (progress % delta) / delta;

        return values[startIndex] * (1f - localT) + values[startIndex + 1] * localT;
    }
}