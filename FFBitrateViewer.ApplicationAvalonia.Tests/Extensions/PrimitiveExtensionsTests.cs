using FFBitrateViewer.ApplicationAvalonia.Extensions;

namespace FFBitrateViewer.ApplicationAvalonia.Tests.Extensions;

internal class PrimitiveExtensionsTests
{
    [Test]
    public void ToStringWithNullText()
    {
        // arrange
        bool? optionalBoolean = null;
        string? nullText = "(empty)";

        // act
        var result = PrimitiveExtensions.ToString(optionalBoolean, nullText: nullText);

        // assert
        Assert.That(result, Is.EqualTo(nullText));
    }

    [Test]
    public void ToStringWithTrueText()
    {
        // arrange
        bool? optionalBoolean = true;
        string? trueText = "Yes";

        // act
        var result = PrimitiveExtensions.ToString(optionalBoolean, trueText: trueText);

        // assert
        Assert.That(result, Is.EqualTo(trueText));
    }

    [Test]
    public void ToStringWithFalseText()
    {
        // arrange
        bool? optionalBoolean = false;
        string? falseText = "No";

        // act
        var result = PrimitiveExtensions.ToString(optionalBoolean, falseText: falseText);

        // assert
        Assert.That(result, Is.EqualTo(falseText));
    }

    [Test]
    public void ToStringWithNullTextAndTrueText()
    {
        // arrange
        bool? optionalBoolean = null;
        string? nullText = "(empty)";
        string? trueText = "Yes";

        // act
        var result = PrimitiveExtensions.ToString(optionalBoolean, nullText: nullText, trueText: trueText);

        // assert
        Assert.That(result, Is.EqualTo(nullText));
    }
}
