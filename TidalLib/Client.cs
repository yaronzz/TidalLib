using AIGS.Common;
using AIGS.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static AIGS.Helper.HttpHelper;

namespace TidalLib
{
    public class Client
    {
        public LoginKey key { get; set; } = null;
        public ProxyInfo proxy { get; set; } = null;
        public Dictionary<string, string> apiKey = new Dictionary<string, string>() {
            {"ClientId", "zU4XHVVkc2tDPo4t"},
            {"ClientSecret", "VJKhDFqJPqvsPVNBV6ukXTJmwlvbttP7wlMlrc72se4="}
        };

        private class TidalRespon
        {
            public string Status { get; set; }
            public string SubStatus { get; set; }
            public string UserMessage { get; set; }
        }

        private class TidalStreamRespon
        {
            public string TrackID { get; set; }
            public string VideoID { get; set; }
            public string StreamType { get; set; }
            public string AssetPresentation { get; set; }
            public string AudioMode { get; set; }
            public string AudioQuality { get; set; }
            public string VideoQuality { get; set; }
            public string ManifestMimeType { get; set; }
            public string Manifest { get; set; }
        }

        private class TidalManifest
        {
            public string MimeType { get; set; }
            public string Codecs { get; set; }
            public string EncryptionType { get; set; }
            public string KeyID { get; set; }
            public string[] Urls { get; set; }
        }

        #region Http
        private async Task<Result> HttpPost(string path, Dictionary<string, string> paras = null, string header = null)
        {
            Result result = await HttpHelper.GetOrPostAsync("https://auth.tidal.com/v1/oauth2" + path,
                                                            paras,
                                                            Retry: 3,
                                                            Header: header,
                                                            Proxy: proxy);
            return result;
        }

        private async Task<string> HttpGet(string path, Dictionary<string, string> paras = null, string urlpre = null)
        {
            path += $"?countryCode={key.CountryCode}";
            if (paras != null)
            {
                foreach (var item in paras)
                    path += $"&{item.Key}={item.Value}";
            }

            urlpre = urlpre ?? "https://api.tidalhifi.com/v1/";
            Result result = await HttpHelper.GetOrPostAsync(urlpre + path,
                                                            Header: $"authorization:Bearer {key.AccessToken}",
                                                            Retry: 3,
                                                            Proxy: proxy);
            if (!result.Success)
            {
                if (result.Errresponse.IsBlank())
                    throw new Exception(result.Errmsg);

                TidalRespon respon = JsonHelper.ConverStringToObject<TidalRespon>(result.Errresponse);
                if (respon.Status == "404" && respon.SubStatus == "2001")
                    throw new Exception("This might be region-locked.");
                else if (respon.Status == "200")
                    throw new Exception("Get operation err.");
                throw new Exception(respon.UserMessage);
            }

            return result.sData;
        }

        private async Task<T> HttpGet<T>(string path, Dictionary<string, string> paras = null, params string[] names)
        {
            string data = await HttpGet(path, paras);
            return JsonHelper.ConverStringToObject<T>(data, names);
        }


        private async Task<ObservableCollection<T>> HttpGetItems<T>(string path, Dictionary<string, string> paras = null)
        {
            paras = paras ?? new Dictionary<string, string>();
            paras.Add("limit", "50");
            paras.Add("offset", "0");

            int offset = 0;
            var ret = new ObservableCollection<T>();
            while (true)
            {
                var array = await HttpGet<ObservableCollection<T>>(path, paras, "items");
                foreach (var item in array)
                    ret.Add(item);

                if (array.Count() < 50)
                    break;

                offset += array.Count();
                paras["offset"] = offset.ToString();
            }
            return ret;
        }

        #endregion

        #region Tool

        public static string GetCoverUrl(string id, string width = "320", string height = "320")
        {
            return $"https://resources.tidal.com/images/{id.Replace('-', '/')}/{width}x{height}.jpg";
        }

        public static string GetArtistsName(ObservableCollection<Artist> artists)
        {
            var names = new List<string>();
            foreach (var item in artists)
                names.Add(item.Name);

            return string.Join(", ", names);
        }

        public static string GetFlag(object data, eType type, bool isShort = true, string separator = " / ")
        {
            var flags = new List<string>();
            if (type == eType.ALBUM)
            {
                Album album = (Album)data;
                if (album.AudioQuality == "HI_RES")
                    flags.Add("M");
                if (album.Explicit)
                    flags.Add("E");
                if (album.AudioModes.Contains("DOLBY_ATMOS"))
                    flags.Add("A");
            }
            else if (type == eType.TRACK)
            {
                Track track = (Track)data;
                if (track.AudioQuality == "HI_RES")
                    flags.Add("M");
                if (track.Explicit)
                    flags.Add("E");
                if (track.AudioModes.Contains("DOLBY_ATMOS"))
                    flags.Add("A");
            }
            else if (type == eType.VIDEO)
            {
                if (((Video)data).Explicit)
                    flags.Add("E");
            }

            if (flags.Count < 0)
                return "";

            var ret = string.Join(separator, flags.ToArray());
            if (!isShort)
            {
                ret = ret.Replace("M", "Master");
                ret = ret.Replace("E", "Explicit");
                ret = ret.Replace("A", "Dolby Atmos");
            }
            return ret;
        }

        private static string GetQualityString(eAudioQuality eQuality)
        {
            switch (eQuality)
            {
                case eAudioQuality.Normal: return "LOW";
                case eAudioQuality.High: return "HIGH";
                case eAudioQuality.HiFi: return "LOSSLESS";
                case eAudioQuality.Master: return "HI_RES";
            }
            return null;
        }

        public static string GetDisplayTitle(Track track)
        {
            if (track.Version != null && track.Version.IsNotBlank())
                return $"{track.Title} ({track.Version})";
            return track.Title;
        }

        private static List<VideoStreamUrl> GetResolutionList(string url)
        {
            List<VideoStreamUrl> ret = new List<VideoStreamUrl>();
            string text = NetHelper.DownloadString(url);
            string[] array = text.Split("#");
            foreach (var item in array)
            {
                if (item.Contains("RESOLUTION=") == false)
                    continue;
                if (item.Contains("EXT-X-STREAM-INF:") == false)
                    continue;

                string codec = StringHelper.GetSubString(item, "CODECS=\"", "\"");
                string reso = StringHelper.GetSubString(item, "RESOLUTION=", "http").Trim();
                if (reso.IndexOf(',') >= 0)
                    reso = reso.Split(',')[0];

                string surl = "http" + StringHelper.GetSubStringOnlyStart(item, "http").Trim();
                ret.Add(new VideoStreamUrl()
                {
                    Codec = codec,
                    Resolution = reso,
                    ResolutionArray = reso.Split("x").ToArray(),
                    M3u8Url = surl,
                });
            }
            return ret;
        }

        #endregion

        #region Login

        public async Task<LoginKey> Login(string accessToken)
        {
            Result result = await HttpHelper.GetOrPostAsync("https://api.tidal.com/v1/sessions",
                                                            Header: $"authorization:Bearer {accessToken}",
                                                            Proxy: proxy);
            if (!result.Success)
            {
                TidalRespon respon = JsonHelper.ConverStringToObject<TidalRespon>(result.Errresponse);
                if (respon == null)
                    throw new Exception(result.Errmsg);
                throw new Exception(respon.UserMessage);
            }

            this.key = JsonHelper.ConverStringToObject<LoginKey>(result.sData);
            key.AccessToken = accessToken;
            return key;
        }

        public async Task<TidalDeviceCode> GetDeviceCode()
        {
            var result = await HttpPost("/device_authorization", new Dictionary<string, string>(){
                                        {"client_id", apiKey["ClientId"]},
                                        {"scope","r_usr+w_usr+w_sub"}});
            if (!result.Success)
                throw new Exception("Device authorization failed.");

            return JsonHelper.ConverStringToObject<TidalDeviceCode>(result.sData);
        }

        public async Task<LoginKey> CheckAuthStatus(TidalDeviceCode deviceCode)
        {
            string authorization = apiKey["ClientId"] + ":" + apiKey["ClientSecret"];
            string base64 = System.Convert.ToBase64String(Encoding.Default.GetBytes(authorization));
            string header = $"Authorization: Basic {base64}";

            DateTime startTime = TimeHelper.GetCurrentTime();
            while (TimeHelper.CalcConsumeTime(startTime) / 1000 < deviceCode.ExpiresIn)
            {
                var result = await HttpPost("/token", new Dictionary<string, string>(){
                                            {"client_id", apiKey["ClientId"]},
                                            {"device_code", deviceCode.DeviceCode},
                                            {"grant_type","urn:ietf:params:oauth:grant-type:device_code"},
                                            {"scope","r_usr+w_usr+w_sub"}},
                                            header);
                if (!result.Success)
                {
                    TidalRespon respon = JsonHelper.ConverStringToObject<TidalRespon>(result.Errresponse);
                    if (respon.Status == "400" && JsonHelper.GetValue(result.Errresponse, "sub_status") == "1002")
                    {
                        Thread.Sleep(1000 * deviceCode.Interval);
                        continue;
                    }
                    throw new Exception("Error while checking for authorization. Trying again...");
                }

                this.key = JsonHelper.ConverStringToObject<LoginKey>(result.sData);
                key.ExpiresIn = JsonHelper.GetValue(result.sData, "expires_in");
                key.RefreshToken = JsonHelper.GetValue(result.sData, "refresh_token");
                key.AccessToken = JsonHelper.GetValue(result.sData, "access_token");
                key.CountryCode = JsonHelper.GetValue(result.sData, "user", "countryCode");
                key.UserID = JsonHelper.GetValue(result.sData, "user", "userId");
                return key;
            }

            throw new Exception("Login timeout...");
        }

        public async Task<LoginKey> RefreshAccessToken(string refreshToken)
        {
            string authorization = apiKey["ClientId"] + ":" + apiKey["ClientSecret"];
            string base64 = System.Convert.ToBase64String(Encoding.Default.GetBytes(authorization));
            string header = $"Authorization: Basic {base64}";

            var result = await HttpPost("/token", new Dictionary<string, string>(){
                                            {"client_id", apiKey["ClientId"]},
                                            {"refresh_token", refreshToken},
                                            {"grant_type","refresh_token"},
                                            {"scope","r_usr+w_usr+w_sub"}},
                                            header);
            if (!result.Success)
                throw new Exception("Refresh failed. Please login again.");

            this.key = JsonHelper.ConverStringToObject<LoginKey>(result.sData);
            key.ExpiresIn = JsonHelper.GetValue(result.sData, "expires_in");
            key.RefreshToken = refreshToken;
            key.AccessToken = JsonHelper.GetValue(result.sData, "access_token");
            key.CountryCode = JsonHelper.GetValue(result.sData, "user", "countryCode");
            key.UserID = JsonHelper.GetValue(result.sData, "user", "userId");
            return key;
        }
        #endregion

        public async Task<Track> GetTrack(string id)
        {
            return await HttpGet<Track>($"tracks/{id}");
        }

        public async Task<Video> GetVideo(string id)
        {
            return await HttpGet<Video>($"videos/{id}");
        }

        public async Task<Album> GetAlbum(string id)
        {
            return await HttpGet<Album>($"albums/{id}");
        }

        public async Task<Playlist> GetPlaylist(string id)
        {
            return await HttpGet<Playlist>($"playlists/{id}");
        }

        public async Task<Artist> GetArtist(string id)
        {
            return await HttpGet<Artist>($"artists/{id}");
        }

        public async Task<TrackLyrics> GetTrackLyrics(string id)
        {
            string data = await HttpGet($"tracks/{id}/lyrics", urlpre: "https://listen.tidal.com/v1/");
            return JsonHelper.ConverStringToObject<TrackLyrics>(data);
        }

        public async Task<(ObservableCollection<Track>, ObservableCollection<Video>)> GetItems(string id, eType type = eType.ALBUM)
        {
            var data = await HttpGetItems<object>($"{type.ToString().ToLower()}s/{id}/items");
            var tracks = new ObservableCollection<Track>();
            var videos = new ObservableCollection<Video>();
            foreach (object item in data)
            {
                if (JsonHelper.GetValue(item.ToString(), "type") == "track")
                    tracks.Add(JsonHelper.ConverStringToObject<Track>(item.ToString(), "item"));
                else
                    videos.Add(JsonHelper.ConverStringToObject<Video>(item.ToString(), "item"));
            }
            return (tracks, videos);
        }

        public async Task<ObservableCollection<Album>> GetArtistAlbums(string id, bool includeEP = true)
        {
            var data = await HttpGetItems<Album>($"artists/{id}/albums");
            if (includeEP)
            {
                var eps = await HttpGetItems<Album>($"artists/{id}/albums", new Dictionary<string, string>() { { "filter", "EPSANDSINGLES" } });
                foreach (var item in eps)
                    data.Add(item);
            }
            return data;
        }

        public async Task<StreamUrl> GetTrackStreamUrl(string id, eAudioQuality eQuality)
        {
            string quality = GetQualityString(eQuality);
            var resp = await HttpGet<TidalStreamRespon>("tracks/" + id + "/playbackinfopostpaywall",
                                                        new Dictionary<string, string>() {
                                                            { "audioquality", quality },
                                                            { "playbackmode", "STREAM" },
                                                            { "assetpresentation", "FULL" }});

            string manifest = StringHelper.Base64Decode(resp.Manifest);
            if (resp.ManifestMimeType.Contains("vnd.tidal.bt"))
            {
                TidalManifest tmanifest = JsonHelper.ConverStringToObject<TidalManifest>(manifest);
                return (new StreamUrl()
                {
                    TrackID = resp.TrackID,
                    Url = tmanifest.Urls[0],
                    Codec = tmanifest.Codecs,
                    EncryptionKey = tmanifest.KeyID,
                    SoundQuality = resp.AudioQuality,
                });
            }
            throw new Exception("Can't get the streamUrl, type is " + resp.ManifestMimeType);
        }

        public async Task<List<VideoStreamUrl>> GetVideStreamUrls(string id)
        {
            var resp = await HttpGet<TidalStreamRespon>("videos/" + id + "/playbackinfopostpaywall",
                                                        new Dictionary<string, string>() {
                                                            { "videoquality", "HIGH" },
                                                            { "playbackmode", "STREAM" },
                                                            { "assetpresentation", "FULL" } });
            string manifest = StringHelper.Base64Decode(resp.Manifest);
            if (resp.ManifestMimeType.Contains("vnd.tidal.emu"))
            {
                TidalManifest tmanifest = JsonHelper.ConverStringToObject<TidalManifest>(manifest);
                List<VideoStreamUrl> list = GetResolutionList(tmanifest.Urls[0]);
                return list;
            }
            throw new Exception("Can't get the streamUrl, type is " + resp.ManifestMimeType);
        }

        public async Task<VideoStreamUrl> GetVideStreamUrl(string id, eVideoQuality eQuality)
        {
            var list = await GetVideStreamUrls(id);
            int cmp = (int)eQuality;
            int index = 0;
            for (int i = 0; i < list.Count(); i++)
            {
                if (cmp >= int.Parse(list[i].ResolutionArray[1]))
                    index = i;
                else
                    break;
            }
            return list[index];
        }

        public static (string, eType) ParseUrl(string url)
        {
            /* example
             * https://tidal.com/browse/track/126205001
             * https://tidal.com/browse/video/124586613
             * https://tidal.com/browse/album/88107428
             * https://tidal.com/browse/artist/9433250
             * https://tidal.com/browse/track/126205001
             * https://tidal.com/browse/playlist/3f199d1d-5fcd-458c-ac4e-906e77981f34
             * https://listen.tidal.com/playlist/3f199d1d-5fcd-458c-ac4e-906e77981f34
             */
            if (url.Contains("tidal.com") == false)
                return (url, eType.NONE);

            string type = null;
            eType etype = eType.NONE;
            var list = AIGS.Common.Convert.ConverEnumToDictionary(typeof(eType), false);
            foreach (var item in list)
            {
                if (url.Contains(item.Value.ToLower()))
                {
                    type = item.Value.ToLower();
                    etype = (eType)item.Key;
                }
            }
            if (etype == eType.NONE)
                return (url, eType.NONE);

            string id = StringHelper.GetSubString(url, type + "/", "/");
            return (id, etype);
        }

        public async Task<SearchResult> Search(string text, int limit = 10, int offset = 0, eType etype = eType.NONE)
        {
            string types = etype.ToString().ToUpper() + "S";
            if (etype == eType.NONE)
                types = "ARTISTS,ALBUMS,TRACKS,VIDEOS,PLAYLISTS";

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "query", text },
                { "offset", offset.ToString() },
                { "types", types },
                { "limit", limit.ToString()},
            };
            string res = await HttpGet("search", data);
            SearchResult result = new SearchResult();
            result.Artists = JsonHelper.ConverStringToObject<ObservableCollection<Artist>>(res, "artists", "items");
            result.Albums = JsonHelper.ConverStringToObject<ObservableCollection<Album>>(res, "albums", "items");
            result.Tracks = JsonHelper.ConverStringToObject<ObservableCollection<Track>>(res, "tracks", "items");
            result.Videos = JsonHelper.ConverStringToObject<ObservableCollection<Video>>(res, "videos", "items");
            result.Playlists = JsonHelper.ConverStringToObject<ObservableCollection<Playlist>>(res, "playlists", "items");
            return result;
        }

        public async Task<(eType, object)> Get(string text,
                                                eType intype = eType.NONE,
                                                int limit = 10,
                                                int offset = 0)
        {
            (string id, eType type) = ParseUrl(text);
            if (intype != eType.NONE)
            {
                type = intype;
                id = text;
            }

            //jump
            foreach (eType item in Enum.GetValues(typeof(eType)))
            {
                if (type != eType.NONE && type != item)
                    continue;
                switch (item)
                {
                    case eType.ALBUM:
                        return (item, await GetAlbum(id));
                    case eType.TRACK:
                        return (item, await GetTrack(id));
                    case eType.VIDEO:
                        return (item, await GetVideo(id));
                    case eType.PLAYLIST:
                        return (item, await GetPlaylist(id));
                    case eType.ARTIST:
                        return (item, await GetArtist(id));
                }
            }

            try
            {
                var result = await Search(text, limit, offset);
                return (eType.NONE, result);
            }
            catch (System.Exception)
            {
                throw new Exception("Search for nothing!");
            }
        }

    }
}


