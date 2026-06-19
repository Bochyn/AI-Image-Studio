using RhinoImageStudio.Shared.Enums;
using RhinoImageStudio.Shared.Utilities;
using Xunit;

namespace RhinoImageStudio.Backend.Tests;

public class DisplayModeMappingTests
{
    [Theory]
    [InlineData("X-Ray", DisplayMode.XRay)]
    [InlineData("Shaded", DisplayMode.Shaded)]
    [InlineData("viewport", null)]
    [InlineData("Current", null)]
    public void Parse_handles_rhino_names_and_sentinels(string input, DisplayMode? expected)
    {
        Assert.Equal(expected, DisplayModeMapping.Parse(input));
    }

    [Fact]
    public void ToRhinoEnglishName_maps_xray()
    {
        Assert.Equal("X-Ray", DisplayModeMapping.ToRhinoEnglishName(DisplayMode.XRay));
    }
}
