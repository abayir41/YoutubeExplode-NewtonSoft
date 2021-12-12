using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YoutubeExplode.Utils;
using YoutubeExplode.Utils.Extensions;

namespace YoutubeExplode.Bridge;

internal partial class PlayerResponseExtractor
{
    private readonly JsonElement _content;
    private readonly Memo _memo = new();

    public PlayerResponseExtractor(JsonElement content) => _content = content;

    private JsonElement? TryGetVideoPlayability() => _memo.Wrap(() =>
        _content.GetPropertyOrNull("playabilityStatus")
    );

    private string? TryGetVideoPlayabilityStatus() => _memo.Wrap(() =>
        TryGetVideoPlayability()?
            .GetPropertyOrNull("status")?
            .GetStringOrNull()
    );

    public string? TryGetVideoPlayabilityError() => _memo.Wrap(() =>
        TryGetVideoPlayability()?
            .GetPropertyOrNull("reason")?
            .GetStringOrNull()
    );

    public bool IsVideoAvailable() => _memo.Wrap(() =>
        !string.Equals(TryGetVideoPlayabilityStatus(), "error", StringComparison.OrdinalIgnoreCase) &&
        TryGetVideoDetails() is not null
    );

    public bool IsVideoPlayable() => _memo.Wrap(() =>
        string.Equals(TryGetVideoPlayabilityStatus(), "ok", StringComparison.OrdinalIgnoreCase)
    );

    private JsonElement? TryGetVideoDetails() => _memo.Wrap(() =>
        _content.GetPropertyOrNull("videoDetails")
    );

    public string? TryGetVideoTitle() => _memo.Wrap(() =>
        TryGetVideoDetails()?
            .GetPropertyOrNull("title")?
            .GetStringOrNull()
    );

    public string? TryGetVideoChannelId() => _memo.Wrap(() =>
        TryGetVideoDetails()?
            .GetPropertyOrNull("channelId")?
            .GetStringOrNull()
    );

    public string? TryGetVideoAuthor() => _memo.Wrap(() =>
        TryGetVideoDetails()?
            .GetPropertyOrNull("author")?
            .GetStringOrNull()
    );

    public DateTimeOffset? TryGetVideoUploadDate() => _memo.Wrap(() =>
        _content
            .GetPropertyOrNull("microformat")?
            .GetPropertyOrNull("playerMicroformatRenderer")?
            .GetPropertyOrNull("uploadDate")?
            .GetDateTimeOffset()
    );

    public TimeSpan? TryGetVideoDuration() => _memo.Wrap(() =>
        TryGetVideoDetails()?
            .GetPropertyOrNull("lengthSeconds")?
            .GetStringOrNull()?
            .ParseDoubleOrNull()?
            .Pipe(TimeSpan.FromSeconds)
    );

    public IReadOnlyList<ThumbnailExtractor> GetVideoThumbnails() => _memo.Wrap(() =>
        TryGetVideoDetails()?
            .GetPropertyOrNull("thumbnail")?
            .GetPropertyOrNull("thumbnails")?
            .EnumerateArrayOrNull()?
            .Select(j => new ThumbnailExtractor(j))
            .ToArray() ??

        Array.Empty<ThumbnailExtractor>()
    );

    public IReadOnlyList<string> GetVideoKeywords() => _memo.Wrap(() =>
        TryGetVideoDetails()?
            .GetPropertyOrNull("keywords")?
            .EnumerateArrayOrNull()?
            .Select(j => j.GetStringOrNull())
            .WhereNotNull()
            .ToArray() ??

        Array.Empty<string>()
    );

    public string? TryGetVideoDescription() => _memo.Wrap(() =>
        TryGetVideoDetails()?
            .GetPropertyOrNull("shortDescription")?
            .GetStringOrNull()
    );

    public long? TryGetVideoViewCount() => _memo.Wrap(() =>
        TryGetVideoDetails()?
            .GetPropertyOrNull("viewCount")?
            .GetStringOrNull()?
            .ParseLongOrNull()
    );

    public string? TryGetPreviewVideoId() => _memo.Wrap(() =>
        TryGetVideoPlayability()?
            .GetPropertyOrNull("errorScreen")?
            .GetPropertyOrNull("playerLegacyDesktopYpcTrailerRenderer")?
            .GetPropertyOrNull("trailerVideoId")?
            .GetStringOrNull() ??

        TryGetVideoPlayability()?
            .GetPropertyOrNull("errorScreen")?
            .GetPropertyOrNull("ypcTrailerRenderer")?
            .GetPropertyOrNull("playerVars")?
            .GetStringOrNull()?
            .Pipe(Url.SplitQuery)
            .GetValueOrDefault("video_id") ??

        TryGetVideoPlayability()?
            .GetPropertyOrNull("errorScreen")?
            .GetPropertyOrNull("ypcTrailerRenderer")?
            .GetPropertyOrNull("playerResponse")?
            .GetStringOrNull()?
            // YouTube uses weird base64-like encoding here that I don't know how to deal with.
            // It's supposed to have JSON inside, but if extracted as is, it contains garbage.
            // Luckily, some of the text gets decoded correctly, which is enough for us to
            // extract the preview video ID using regex.
            .Replace('-', '+')
            .Replace('_', '/')
            .Pipe(Convert.FromBase64String)
            .Pipe(Encoding.UTF8.GetString)
            .Pipe(s => Regex.Match(s, @"video_id=(.{11})").Groups[1].Value)
            .NullIfWhiteSpace()
    );

    private JsonElement? TryGetStreamingData() => _memo.Wrap(() =>
        _content.GetPropertyOrNull("streamingData")
    );

    public string? TryGetDashManifestUrl() => _memo.Wrap(() =>
        TryGetStreamingData()?
            .GetPropertyOrNull("dashManifestUrl")?
            .GetStringOrNull()
    );

    public string? TryGetHlsManifestUrl() => _memo.Wrap(() =>
        TryGetStreamingData()?
            .GetPropertyOrNull("hlsManifestUrl")?
            .GetStringOrNull()
    );

    public IReadOnlyList<IStreamInfoExtractor> GetStreams() => _memo.Wrap(() =>
    {
        var result = new List<IStreamInfoExtractor>();

        var muxedStreams = TryGetStreamingData()?
            .GetPropertyOrNull("formats")?
            .EnumerateArrayOrNull()?
            .Select(j => new PlayerStreamInfoExtractor(j));

        if (muxedStreams is not null)
            result.AddRange(muxedStreams);

        var adaptiveStreams = TryGetStreamingData()?
            .GetPropertyOrNull("adaptiveFormats")?
            .EnumerateArrayOrNull()?
            .Select(j => new PlayerStreamInfoExtractor(j));

        if (adaptiveStreams is not null)
            result.AddRange(adaptiveStreams);

        return result;
    });

    public IReadOnlyList<PlayerClosedCaptionTrackInfoExtractor> GetClosedCaptionTracks() => _memo.Wrap(() =>
        _content
            .GetPropertyOrNull("captions")?
            .GetPropertyOrNull("playerCaptionsTracklistRenderer")?
            .GetPropertyOrNull("captionTracks")?
            .EnumerateArrayOrNull()?
            .Select(j => new PlayerClosedCaptionTrackInfoExtractor(j))
            .ToArray() ??

        Array.Empty<PlayerClosedCaptionTrackInfoExtractor>()
    );
}

internal partial class PlayerResponseExtractor
{
    public static PlayerResponseExtractor Create(string raw) => new(Json.Parse(raw));
}