using UnityEngine;

public static class CBUtils
{
    #region Remap methods
    private static float RemapFloat(float input, float startMin, float startMax, float endMin, float endMax, bool clampInput)
    {
        if (clampInput)
        {
            input = Mathf.Clamp(input, startMin, startMax);
        }
        return endMin + (input - startMin) * (endMax - endMin) / (startMax - startMin);
    }
    public static float Remap(float input, float startMin, float startMax, float endMin, float endMax, bool clampInput)
    {
        return RemapFloat(input, startMin, startMax, endMin, endMax, clampInput);
    }
    public static Vector2 Remap(Vector2 input, Vector2 startMin, Vector2 startMax, Vector2 endMin, Vector2 endMax, bool clampInput)
    {
        float x = RemapFloat(input.x, startMin.x, startMax.x, endMin.x, endMax.x, clampInput);
        float y = RemapFloat(input.y, startMin.y, startMax.y, endMin.y, endMax.y, clampInput);
        return new Vector2(x, y);
    }
    public static Vector2 Remap(Vector2 input, float startMin, float startMax, float endMin, float endMax, bool clampInput)
    {
        float x = RemapFloat(input.x, startMin, startMax, endMin, endMax, clampInput);
        float y = RemapFloat(input.y, startMin, startMax, endMin, endMax, clampInput);
        return new Vector2(x, y);
    }
    public static Vector3 Remap(Vector3 input, Vector3 startMin, Vector3 startMax, Vector3 endMin, Vector3 endMax, bool clampInput)
    {
        float x = RemapFloat(input.x, startMin.x, startMax.x, endMin.x, endMax.x, clampInput);
        float y = RemapFloat(input.y, startMin.y, startMax.y, endMin.y, endMax.y, clampInput);
        float z = RemapFloat(input.z, startMin.z, startMax.z, endMin.z, endMax.z, clampInput);
        return new Vector3(x, y, z);
    }
    public static Vector3 Remap(Vector3 input, float startMin, float startMax, float endMin, float endMax, bool clampInput)
    {
        float x = RemapFloat(input.x, startMin, startMax, endMin, endMax, clampInput);
        float y = RemapFloat(input.y, startMin, startMax, endMin, endMax, clampInput);
        float z = RemapFloat(input.z, startMin, startMax, endMin, endMax, clampInput);
        return new Vector3(x, y, z);
    }
    public static Vector4 Remap(Vector4 input, Vector4 startMin, Vector4 startMax, Vector4 endMin, Vector4 endMax, bool clampInput)
    {
        float x = RemapFloat(input.x, startMin.x, startMax.x, endMin.x, endMax.x, clampInput);
        float y = RemapFloat(input.y, startMin.y, startMax.y, endMin.y, endMax.y, clampInput);
        float z = RemapFloat(input.z, startMin.z, startMax.z, endMin.z, endMax.z, clampInput);
        float w = RemapFloat(input.w, startMin.w, startMax.w, endMin.w, endMax.w, clampInput);

        return new Vector4(x, y, z, w);
    }
    public static Vector4 Remap(Vector4 input, float startMin, float startMax, float endMin, float endMax, bool clampInput)
    {
        float x = RemapFloat(input.x, startMin, startMax, endMin, endMax, clampInput);
        float y = RemapFloat(input.y, startMin, startMax, endMin, endMax, clampInput);
        float z = RemapFloat(input.z, startMin, startMax, endMin, endMax, clampInput);
        float w = RemapFloat(input.w, startMin, startMax, endMin, endMax, clampInput);
        return new Vector4(x, y, z, w);
    }
    #endregion



}
