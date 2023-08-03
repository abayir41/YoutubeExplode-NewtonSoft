using Newtonsoft.Json.Linq;
using YoutubeExplode.Utils;
using YoutubeExplode.Utils.Extensions;

namespace YoutubeExplode.Bridge;

internal class ThumbnailData
{
    private readonly JToken _content;

    public ThumbnailData(JToken content) => _content = content;

    public string? Url => Memo.Cache(this, () =>
        _content.GetPropertyOrNull("url")?.GetStringOrNull()
    );

    public int? Width => Memo.Cache(this, () =>
        _content.GetPropertyOrNull("width")?.GetInt32OrNull()
    );

    public int? Height => Memo.Cache(this, () =>
        _content.GetPropertyOrNull("height")?.GetInt32OrNull()
    );
}