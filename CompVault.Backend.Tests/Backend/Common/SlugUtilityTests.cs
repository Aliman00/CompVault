using CompVault.Backend.Common.Utils;

using FluentAssertions;

using Xunit;

namespace CompVault.Backend.Tests.Backend.Common;

public class SlugUtilityTests
{
    // ============================ GenerateSlug — standard-caser ============================

    [Fact]
    public void GenerateSlug_SingleWord_NoHyphen()
    {
        SlugUtility.GenerateSlug("Stilling").Should().Be("stilling");
    }

    [Fact]
    public void GenerateSlug_WithSpace_ReplacesWithHyphen()
    {
        SlugUtility.GenerateSlug("Stillings Instruks").Should().Be("stillings-instruks");
    }

    [Fact]
    public void GenerateSlug_MultipleWords_ReplacesSpacesWithHyphens()
    {
        SlugUtility.GenerateSlug("HMS Dokumenter").Should().Be("hms-dokumenter");
    }

    // ============================ Norske tegn ============================

    [Theory]
    [InlineData("Årsrapport", "aarsrapport")]
    [InlineData("Økonomi", "oekonomi")]
    [InlineData("Bærekraft", "baerekraft")]
    [InlineData("Sjøfart", "sjoefart")]
    [InlineData("Fugl Ørn", "fugl-oern")]
    public void GenerateSlug_NorwegianChars_Transliterates(string input, string expected)
    {
        SlugUtility.GenerateSlug(input).Should().Be(expected);
    }

    // ============================ Spesialtegn fjernes ============================

    [Fact]
    public void GenerateSlug_SpecialCharacters_Removed()
    {
        SlugUtility.GenerateSlug("Sikkerhet & HMS").Should().Be("sikkerhet-hms");
    }

    [Fact]
    public void GenerateSlug_ParenthesesAndDots_Removed()
    {
        SlugUtility.GenerateSlug("Vedtak (2024)").Should().Be("vedtak-2024");
    }

    // ============================ Whitespace-håndtering ============================

    [Fact]
    public void GenerateSlug_MultipleSpaces_SingleHyphen()
    {
        SlugUtility.GenerateSlug("Foo   Bar").Should().Be("foo-bar");
    }

    [Fact]
    public void GenerateSlug_LeadingTrailingSpaces_Trimmed()
    {
        SlugUtility.GenerateSlug("  HMS Dokumenter  ").Should().Be("hms-dokumenter");
    }

    // ============================ Lengde og edge cases ============================

    [Fact]
    public void GenerateSlug_LongName_TruncatedTo50()
    {
        string longName = new('a', 60);
        string slug = SlugUtility.GenerateSlug(longName);
        slug.Should().HaveLength(50);
    }

    [Fact]
    public void GenerateSlug_MixedCase_ToLowerCase()
    {
        SlugUtility.GenerateSlug("MiXeD CaSe NaMe").Should().Be("mixed-case-name");
    }

    [Fact]
    public void GenerateSlug_WithNumbers_Preserved()
    {
        SlugUtility.GenerateSlug("Kurs 2026").Should().Be("kurs-2026");
    }

    [Fact]
    public void GenerateSlug_ConsecutiveHyphensAfterCleanup_Collapsed()
    {
        SlugUtility.GenerateSlug("Foo -- Bar").Should().Be("foo-bar");
    }

    // ============================ Ugyldig input ============================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateSlug_NullOrWhitespace_ThrowsArgumentException(string? input)
    {
        Action act = () => SlugUtility.GenerateSlug(input!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateSlug_OnlySpecialChars_ThrowsArgumentException()
    {
        Action act = () => SlugUtility.GenerateSlug("@#$%^&*()");
        act.Should().Throw<ArgumentException>();
    }

    // ============================ IsValidSlug ============================

    [Theory]
    [InlineData("stilling", true)]
    [InlineData("stillings-instruks", true)]
    [InlineData("hms-dokumenter-2026", true)]
    [InlineData("a", true)]
    [InlineData("123", true)]
    public void IsValidSlug_ValidSlugs_ReturnsTrue(string slug, bool expected)
    {
        SlugUtility.IsValidSlug(slug).Should().Be(expected);
    }

    [Theory]
    [InlineData("HMS-Dokumenter", false)]
    [InlineData("hms_dokumenter", false)]
    [InlineData("hms dokumenter", false)]
    [InlineData("-leading", false)]
    [InlineData("trailing-", false)]
    [InlineData("double--hyphen", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsValidSlug_InvalidSlugs_ReturnsFalse(string slug, bool expected)
    {
        SlugUtility.IsValidSlug(slug).Should().Be(expected);
    }
}
