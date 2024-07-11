using FFBitrateViewer.ApplicationAvalonia.Extensions;

namespace FFBitrateViewer.ApplicationAvalonia.UnitTests.Extensions;

public class PrimitiveExtensionsTests
{
    [Fact]
    public void ToStringWithNullText()
    {
        // arrange
        bool? optionalBoolean = null;
        string? nullText = "(empty)";

        // act
        var result = PrimitiveExtensions.ToString(optionalBoolean, nullText: nullText);

        // assert
        Assert.Equal(nullText, result);
    }

    [Fact]
    public void ToStringWithTrueText()
    {
        // arrange
        bool? optionalBoolean = true;
        string? trueText = "Yes";

        // act
        var result = PrimitiveExtensions.ToString(optionalBoolean, trueText: trueText);

        // assert
        Assert.Equal(trueText, result);
    }

    [Fact]
    public void ToStringWithFalseText()
    {
        // arrange
        bool? optionalBoolean = false;
        string? falseText = "No";

        // act
        var result = PrimitiveExtensions.ToString(optionalBoolean, falseText: falseText);

        // assert
        Assert.Equal(falseText, result);
    }

    [Fact]
    public void ToStringWithNullTextAndTrueText()
    {
        // arrange
        bool? optionalBoolean = null;
        string? nullText = "(empty)";
        string? trueText = "Yes";

        // act
        var result = PrimitiveExtensions.ToString(optionalBoolean, nullText: nullText, trueText: trueText);

        // assert
        Assert.Equal(nullText, result);
    }
}