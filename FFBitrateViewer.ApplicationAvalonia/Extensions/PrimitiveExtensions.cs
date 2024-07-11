using System;

namespace FFBitrateViewer.ApplicationAvalonia.Extensions;


/// <summary>
/// Provides extension methods for primitive types.
/// </summary>
public static class PrimitiveExtensions
{
    /// <summary>Converts the specified <see cref="Nullable{Boolean}"/> value to its string representation using the specified format.</summary>
    /// <param name="optionalBoolean">The nullable <see cref="Nullable{Boolean}"/> value to convert to its string representation.</param>
    /// <param name="nullText">The text to return when <paramref name="optionalBoolean"/> is <c>null</c>.</param>
    /// <param name="trueText">The text to return when <paramref name="optionalBoolean"/> is <c>true</c>.</param>
    /// <param name="falseText">The text to return when <paramref name="optionalBoolean"/> is <c>false</c>.</param>
    public static string ToString(this bool? optionalBoolean, string? nullText, string? trueText = null, string? falseText = null)
        => optionalBoolean == true ? (trueText ?? bool.TrueString) : optionalBoolean == false ? (falseText ?? bool.FalseString) : (nullText ?? "null");

    /// <summary>Converts the specified <see cref="Nullable{Boolean}"/> value to its string representation.</summary>
    /// <param name="optionalBoolean">The nullable <see cref="Nullable{Boolean}"/> value to convert to its string representation.</param>
    /// <param name="trueText">The text to return when <paramref name="optionalBoolean"/> is <c>true</c>.</param>
    /// <param name="falseText">The text to return when <paramref name="optionalBoolean"/> is <c>false</c>.</param>
    /// <returns>The string representation of <paramref name="optionalBoolean"/>; otherwise, <c>null</c>.</returns>
    public static string? ToString(this bool? optionalBoolean, string? trueText = null, string? falseText = null)
        => optionalBoolean == true ? (trueText ?? bool.TrueString) : optionalBoolean == false ? (falseText ?? bool.FalseString) : null;

}