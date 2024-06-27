using System;
using System.Text;
using System.Text.RegularExpressions;


namespace FFBitrateViewer.ApplicationAvalonia.Models.Media;

public record NDPair(string Value, int? Numerator, int? Denominator)
{
    public static readonly NDPair Default = new(string.Empty, null, null);
    private static readonly Regex NDPairRegex = new(@"^(?<numerator>\d+)/(?<denominator>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    public static NDPair Parse(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));

        var match = NDPairRegex.Match(value);

        if (!match.Success)
        { throw new FormatException($"Value '{value}' is not in the expected format."); }

        if (!match.Groups.TryGetValue("numerator", out var numeratorGroup))
        { throw new FormatException($"Value '{value}' is not in the expected format."); }

        if (!match.Groups.TryGetValue("denominator", out var denominatorGroup))
        { throw new FormatException($"Value '{value}' is not in the expected format."); }

        return new NDPair(value, int.Parse(numeratorGroup.Value), int.Parse(denominatorGroup.Value));
    }

    public double? ToDouble()
    {
        if (Numerator is null || Denominator is null)
        { return null; }
        return (double)Numerator / (double)Denominator;
    }

    public string ToString(bool isNumberOnly)
    {
        if (isNumberOnly)
        {
            return $"{ToDouble():F3}";
        }

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(nameof(NDPair));
        stringBuilder.Append(" { ");
        if (PrintMembers(stringBuilder))
        {
            stringBuilder.Append(' ');
        }
        stringBuilder.Append('}');
        stringBuilder.AppendFormat("[ {0} / (1)= {2:F3} ]", Numerator, Denominator, ToDouble());
        return stringBuilder.ToString();
    }

}