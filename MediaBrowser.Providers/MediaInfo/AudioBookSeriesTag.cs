using System;
using System.Collections.Generic;

namespace MediaBrowser.Providers.MediaInfo;

/// <summary>
/// Reads the series an audiobook belongs to from its tags.
/// </summary>
internal static class AudioBookSeriesTag
{
    private const string SeriesKey = "SERIES";

    /// <summary>
    /// Gets the series name of an audiobook.
    /// </summary>
    /// <remarks>
    /// ATL maps the standard movement name tags (MP4 <c>©mvn</c>, ID3 <c>MVNM</c>) to <paramref name="seriesTitle"/>.
    /// Audiobook taggers such as tone and Mp3tag also write a custom <c>SERIES</c> tag,
    /// which reaches ATL's additional fields as an MP4 freeform atom (<c>----:com.pilabor.tone:SERIES</c>),
    /// an ID3 <c>TXXX</c> frame, a Vorbis comment or a Matroska tag (<c>track.series</c>).
    /// </remarks>
    /// <param name="seriesTitle">The series title read by ATL.</param>
    /// <param name="additionalFields">The additional tag fields read by ATL.</param>
    /// <returns>The series name, or <c>null</c> if the tags name none.</returns>
    public static string? GetSeriesName(string? seriesTitle, IDictionary<string, string> additionalFields)
    {
        if (!string.IsNullOrWhiteSpace(seriesTitle))
        {
            return seriesTitle.Trim();
        }

        foreach (var (key, value) in additionalFields)
        {
            if (!string.IsNullOrWhiteSpace(value) && IsSeriesKey(key))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static bool IsSeriesKey(string key)
    {
        var name = key.AsSpan();

        // MP4 freeform atom: "----:<mean>:<name>"
        if (name.StartsWith("----:", StringComparison.Ordinal))
        {
            name = name[(name.LastIndexOf(':') + 1)..];
        }
        else if (name.StartsWith("track.", StringComparison.OrdinalIgnoreCase))
        {
            // Matroska tags, see AudioFileProber.GetMkaFallbackKey
            name = name[6..];
        }

        return name.Equals(SeriesKey, StringComparison.OrdinalIgnoreCase);
    }
}
