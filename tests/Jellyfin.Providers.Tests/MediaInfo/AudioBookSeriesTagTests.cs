using System.Collections.Generic;
using MediaBrowser.Providers.MediaInfo;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class AudioBookSeriesTagTests
{
    [Theory]
    [InlineData("----:com.pilabor.tone:SERIES")] // tone, MP4 freeform atom
    [InlineData("----:com.apple.iTunes:series")]
    [InlineData("SERIES")] // ID3 TXXX, Vorbis comment
    [InlineData("track.series")] // Matroska
    public void GetSeriesName_SeriesTag_ReturnsValue(string key)
    {
        var fields = new Dictionary<string, string> { [key] = " Example Saga " };

        Assert.Equal("Example Saga", AudioBookSeriesTag.GetSeriesName(null, fields));
    }

    [Fact]
    public void GetSeriesName_SeriesTitle_TakesPrecedence()
    {
        var fields = new Dictionary<string, string> { ["SERIES"] = "Other" };

        Assert.Equal("Example Saga", AudioBookSeriesTag.GetSeriesName("Example Saga", fields));
    }

    [Theory]
    [InlineData("----:com.pilabor.tone:SERIES-PART", "4")]
    [InlineData("----:com.pilabor.tone:PART", "4")]
    [InlineData("----:com.pilabor.tone:SUBTITLE", "Example Saga, Book 4")]
    [InlineData("SERIESFOO", "Example Saga")]
    [InlineData("SERIES", " ")]
    public void GetSeriesName_NoSeriesTag_ReturnsNull(string key, string value)
    {
        var fields = new Dictionary<string, string> { [key] = value };

        Assert.Null(AudioBookSeriesTag.GetSeriesName(" ", fields));
    }
}
