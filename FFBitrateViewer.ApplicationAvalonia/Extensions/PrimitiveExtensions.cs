using System;

namespace FFBitrateViewer.ApplicationAvalonia.Extensions;

public static class PrimitiveExtensions
{

    public static string ToString(this bool? optionalBoolean, string? nullText, string? trueText = null, string? falseText = null)
        => optionalBoolean == true ? (trueText ?? bool.TrueString) : optionalBoolean == false ? (falseText ?? bool.FalseString) : (nullText ?? "null");

    public static string? ToString(this bool? optionalBoolean, string? trueText = null, string? falseText = null)
        => optionalBoolean == true ? (trueText ?? bool.TrueString) : optionalBoolean == false ? (falseText ?? bool.FalseString) : null;

}
