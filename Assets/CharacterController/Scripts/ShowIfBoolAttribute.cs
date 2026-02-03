using UnityEngine;

public class ShowIfBoolAttribute : PropertyAttribute
{
    public readonly string boolName;
    public readonly bool invert;

    public ShowIfBoolAttribute(string boolName, bool invert = false)
    {
        this.boolName = boolName;
        this.invert = invert;
    }
}
