using System;

namespace examportal.Api.ServiceExtensions;

public static class EnumExtensions
{
    public static string GetEnumText(this Enum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        var attributes = fieldInfo.GetCustomAttributes(typeof(EnumTextAttribute), false) as EnumTextAttribute[];
        return attributes?.Length > 0 ? attributes[0].Text : value.ToString();
    }
}

public class EnumTextAttribute : Attribute
{
    public string Text { get; }

    public EnumTextAttribute(string text)
    {
        Text = text;
    }
}