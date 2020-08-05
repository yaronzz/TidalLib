using AIGS.Common;
using AIGS.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AIGS.Helper.HttpHelper;

namespace TidalLib
{
    public class Client
    {
        /* From:https://github.com/arnesongit/plugin.audio.tidal2
         * wc8j_yBJd20zOmx0 :download FLAC(only hifi), but cant download video
         * _DSTon1kC8pABnTw :download video, but uses ALAC instead of FLAC(only hifi)
         */

        private static string TOKEN = "wc8j_yBJd20zOmx0";
        private static string BASE_URL = "https://api.tidalhifi.com/v1/";
        private static string VERSION = "1.9.1";

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

        #region Request

        private static async Task<(string, string)> Request(LoginKey oKey, string sPath, Dictionary<string, string> oParas = null, int iRetry = 3)
        {
            string paras = $"?countryCode={oKey.CountryCode}";
            if (oParas != null)
            {
                foreach (var item in oParas)
                    paras += $"&{item.Key}={item.Value}";
            }

            string header = $"X-Tidal-SessionId:{oKey.SessionID}";
            if (oKey.AccessToken.IsNotBlank())
                header = $"authorization:Bearer {oKey.AccessToken}";

            Result result = await HttpHelper.GetOrPostAsync(BASE_URL + sPath + paras, Header: header, Retry: iRetry, Proxy: oKey.Proxy);
            if (result.Success == false)
            {
                TidalRespon respon = JsonHelper.ConverStringToObject<TidalRespon>(result.Errresponse);
                string msg = respon.UserMessage + "! ";
                if (respon.Status == "404" && respon.SubStatus == "2001")
                    msg += "This might be region-locked.";
                else if (respon.Status == "200")
                    msg += "Get operation err.";
                return (msg, null);
            }

            return (null, result.sData);
        }

        private static async Task<(string, T)> Request<T>(LoginKey oKey, string sPath, Dictionary<string, string> oParas = null, int iRetry = 3, params string[] sKeyName)
        {
            (string msg, string data) = await Request(oKey, sPath, oParas, iRetry);
            if (msg.IsNotBlank() || data.IsBlank())
                return (msg, default(T));

            T aRet = JsonHelper.ConverStringToObject<T>(data, sKeyName);
            return (null, aRet);
        }


        private static async Task<(string, ObservableCollection<T>)> RequestItems<T>(LoginKey oKey, string sPath, Dictionary<string, string> oParas = null, int iRetry = 3)
        {
            if (oParas == null)
                oParas = new Dictionary<string, string>();
            oParas.Add("limit", "50");
            oParas.Add("offset", "0");

            int iOffset = 0;
            ObservableCollection<T> pRet = new ObservableCollection<T>();
            while (true)
            {
                (string msg, string data) = await Request(oKey, sPath, oParas, iRetry);
                if (msg.IsNotBlank() || data.IsBlank())
                    return (msg, null);

                ObservableCollection<T> pList = JsonHelper.ConverStringToObject<ObservableCollection<T>>(data, "items");
                foreach (var item in pList)
                    pRet.Add(item);

                if (pList.Count() < 50)
                    break;

                iOffset += pList.Count();
                oParas["offset"] = iOffset.ToString();
            }
            return (null,pRet);
        }

        #endregion

        #region Tool

        public static string GetCoverUrl(string sID, string iWidth = "320", string iHeight = "320")
        {
            if (sID == null)
                return null;
            return string.Format("https://resources.tidal.com/images/{0}/{1}x{2}.jpg", sID.Replace('-', '/'), iWidth, iHeight);
        }

        public static string[] GetArtistsList(ObservableCollection<Artist> Artists)
        {
            if (Artists == null)
                return null;
            List<string> names = new List<string>();
            foreach (var item in Artists)
                names.Add(item.Name);
            return names.ToArray();
        }

        public static string GetArtists(ObservableCollection<Artist> Artists)
        {
            if (Artists == null)
                return null;
            string[] names = GetArtistsList(Artists);
            string ret = string.Join(" / ", names);
            return ret;
        }

        public static string GetFlag(object data, eType type, bool bShort = true, string separator = " / ")
        {
            bool bMaster = false;
            bool bExplicit = false;

            if (type == eType.ALBUM)
            {
                Album album = (Album)data;
                if (album.AudioQuality == "HI_RES")
                    bMaster = true;
                if (album.Explicit)
                    bExplicit = true;
            }
            else if (type == eType.TRACK)
            {
                Track track = (Track)data;
                if (track.AudioQuality == "HI_RES")
                    bMaster = true;
                if (track.Explicit)
                    bExplicit = true;
            }
            else if (type == eType.VIDEO)
            {
                Video video = (Video)data;
                if (video.Explicit)
                    bExplicit = true;
            }

            if (bMaster == false && bExplicit == false)
                return "";

            List<string> flags = new List<string>();
            if (bMaster)
                flags.Add(bShort ? "M" : "Master");
            if (bExplicit)
                flags.Add(bShort ? "E" : "Explicit");
            return string.Join(separator, flags.ToArray());
        }

        private static string GetQualityString(eAudioQuality eQuality)
        {
            switch(eQuality)
            {
                case eAudioQuality.Normal: return "LOW";
                case eAudioQuality.High: return "HIGH";
                case eAudioQuality.HiFi: return "LOSSLESS";
                case eAudioQuality.Master: return "HI_RES";
            }
            return null;
        }

        private static string GetTrackDisplayTitle(Track track)
        {
            if(track.Version != null && track.Version.IsNotBlank())
                return $"{track.Title} - {track.Version}";
            return track.Title;
        }

        private static List<VideoStreamUrl> GetResolutionList(string url)
        {
            List<VideoStreamUrl> ret = new List<VideoStreamUrl>();
            string text = NetHelper.DownloadString(url);
            string[] array = text.Split("#EXT-X-STREAM-INF");
            foreach (var item in array)
            {
                if (item.Contains("RESOLUTION=") == false)
                    continue;

                string codec = StringHelper.GetSubString(item, "CODECS=\"", "\"");
                string reso = StringHelper.GetSubString(item, "RESOLUTION=", "http").Trim();
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

        public static async Task<(string, LoginKey)> Login(string sUserName, string sPassword, string sToken = null, HttpHelper.ProxyInfo oProxy = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>() {
                {"username", sUserName },
                {"password", sPassword },
                {"token", sToken ?? TOKEN },
                {"clientVersion", VERSION},
                {"clientUniqueKey", Guid.NewGuid().ToString().Replace("-","").Substring(0, 16)} };

            Result result = await HttpHelper.GetOrPostAsync(BASE_URL + "login/username", data, Proxy: oProxy);
            if (result.Success == false)
            {
                TidalRespon respon = JsonHelper.ConverStringToObject<TidalRespon>(result.Errresponse);
                if(respon != null)
                    return (respon.UserMessage, null);
                return (null, null);
            }

            LoginKey key = JsonHelper.ConverStringToObject<LoginKey>(result.sData);
            key.UserName = sUserName;
            key.Password = sPassword;
            key.Proxy = oProxy;
            return (null, key);
        }

        public static async Task<(string, LoginKey)> Login(string sAccessToken, HttpHelper.ProxyInfo oProxy = null)
        {
            Result result = await HttpHelper.GetOrPostAsync("https://api.tidal.com/v1/sessions", Header: $"authorization:Bearer {sAccessToken}", Proxy: oProxy);
            if (result.Success == false)
            {
                TidalRespon respon = JsonHelper.ConverStringToObject<TidalRespon>(result.Errresponse);
                return (respon.UserMessage, null);
            }

            LoginKey key = JsonHelper.ConverStringToObject<LoginKey>(result.sData);
            key.AccessToken = sAccessToken;
            key.Proxy = oProxy;
            return (null, key);
        }

        public static async Task<(string, Album)> GetAlbum(LoginKey oKey, string ID, bool bGetItems = true)
        {
            (string msg, Album data) = await Request<Album>(oKey, "albums/" + ID);
            if (data != null)
            {
                if (bGetItems)
                    (msg, data.Tracks, data.Videos) = await GetItems(oKey, ID);
            }
            return (null, data);
        }


        public static async Task<(string, Playlist)> GetPlaylist(LoginKey oKey, string ID, bool bGetItems = true)
        {
            (string msg, Playlist data) = await Request<Playlist>(oKey, "playlists/" + ID);
            if (data != null)
            {
                if (bGetItems)
                    (msg, data.Tracks, data.Videos) = await GetItems(oKey, ID, eType.PLAYLIST);
            }
            return (null, data);
        }

        /// <summary>
        /// Get playlist or album items
        /// </summary>
        /// <param name="oKey"></param>
        /// <param name="ID"></param>
        /// <param name="eType"></param>
        /// <returns></returns>
        public static async Task<(string, ObservableCollection<Track>, ObservableCollection<Video>)> GetItems(LoginKey oKey, string ID, eType eType = eType.ALBUM)
        {
            string type = "albums/";
            if (eType == eType.PLAYLIST)
                type = "playlists/";

            (string msg, ObservableCollection<object> data) = await RequestItems<object>(oKey, type + ID + "/items");
            if (msg.IsNotBlank() || data == null)
                return (msg, null, null);

            ObservableCollection<Track> tracks = new ObservableCollection<Track>();
            ObservableCollection<Video> videos = new ObservableCollection<Video>();
            foreach (object item in data)
            {
                if (JsonHelper.GetValue(item.ToString(), "type") == "track")
                {
                    Track track = JsonHelper.ConverStringToObject<Track>(item.ToString(), "item");
                    track.DisplayTitle = GetTrackDisplayTitle(track);
                    tracks.Add(track);
                }
                else
                    videos.Add(JsonHelper.ConverStringToObject<Video>(item.ToString(), "item"));
            }

            return (null, tracks, videos);
        }


        public static async Task<(string, Artist)> GetArtist(LoginKey oKey, string ID, bool bContainEPSingle = true, bool bGetItems = true)
        {
            (string msg, Artist data) = await Request<Artist>(oKey, "artists/" + ID);
            if (msg.IsNotBlank() || data == null)
                return (msg, null);

            //get albums
            (msg, data.Albums) = await RequestItems<Album>(oKey, "artists/" + ID + "/albums");
            if (data.Albums == null)
                data.Albums = new ObservableCollection<Album>();

            //get ep&single
            if(bContainEPSingle)
            {
                (string msg1, ObservableCollection<Album> eps) = await RequestItems<Album>(oKey, "artists/" + ID + "/albums", new Dictionary<string, string>() { { "filter", "EPSANDSINGLES" } });
                foreach (var item in eps)
                    data.Albums.Add(item);
            }

            //get items
            if (bGetItems)
            {
                foreach (var item in data.Albums)
                    (msg, item.Tracks, item.Videos) = await GetItems(oKey, item.ID);
            }
            return (null, data);
        }


        public static async Task<(string,Track)> GetTrack(LoginKey oKey, string ID)
        {
            (string msg, Track data) = await Request<Track>(oKey, "tracks/" + ID);
            if (data != null)
                data.DisplayTitle = GetTrackDisplayTitle(data);
            return (msg, data);
        }

        public static async Task<(string, StreamUrl)> GetTrackStreamUrl(LoginKey oKey, string ID, eAudioQuality eQuality)
        {
            string quality = GetQualityString(eQuality);
            (string msg, TidalStreamRespon resp) = await Request<TidalStreamRespon>(oKey, "tracks/" + ID + "/playbackinfopostpaywall", new Dictionary<string, string>() { { "audioquality", quality }, { "playbackmode", "STREAM" }, { "assetpresentation", "FULL" } }, 3);
            if (resp != null)
            {
                string manifest = StringHelper.Base64Decode(resp.Manifest);
                if (resp.ManifestMimeType.Contains("vnd.tidal.bt"))
                {
                    TidalManifest tmanifest = JsonHelper.ConverStringToObject<TidalManifest>(manifest);
                    return (null, new StreamUrl()
                    {
                        TrackID = resp.TrackID,
                        Url = tmanifest.Urls[0],
                        Codec = tmanifest.Codecs,
                        EncryptionKey = tmanifest.KeyID,
                        SoundQuality = resp.AudioQuality,
                    });
                }
            }
            return (msg, null);
        }

        public static async Task<(string, Video)> GetVideo(LoginKey oKey, string ID)
        {
            (string msg, Video data) = await Request<Video>(oKey, "videos/" + ID);
            return (msg, data);
        }

        public static async Task<(string, List<VideoStreamUrl>)> GetVideStreamUrls(LoginKey oKey, string ID)
        {
            (string msg, TidalStreamRespon resp) = await Request<TidalStreamRespon>(oKey, "videos/" + ID + "/playbackinfopostpaywall", new Dictionary<string, string>() { { "videoquality", "HIGH" }, { "playbackmode", "STREAM" }, { "assetpresentation", "FULL" } }, 3);
            if (resp != null)
            {
                string manifest = StringHelper.Base64Decode(resp.Manifest);
                if (resp.ManifestMimeType.Contains("vnd.tidal.emu"))
                {
                    TidalManifest tmanifest = JsonHelper.ConverStringToObject<TidalManifest>(manifest);
                    List<VideoStreamUrl> list = GetResolutionList(tmanifest.Urls[0]);
                    return (null, list);
                }
            }
            return (msg, null);
        }

        public static async Task<(string, VideoStreamUrl)> GetVideStreamUrl(LoginKey oKey, string ID, eVideoQuality eReso)
        {
            (string msg, List<VideoStreamUrl> list) = await GetVideStreamUrls(oKey, ID);
            if(list != null)
            {
                int iCmp = (int)eReso;
                int iIndex = list.Count - 1;
                for (int i = 0; i < list.Count(); i++)
                {
                    if (iCmp >= int.Parse(list[i].ResolutionArray[1]))
                    {
                        iIndex = i;
                        break;
                    }
                }
                return (null, list[iIndex]);
            }
            return (msg, null);
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
            Dictionary<int, string> list = AIGS.Common.Convert.ConverEnumToDictionary(typeof(eType), false);
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

        public static async Task<(string, SearchResult)> Search(LoginKey oKey, string sTex, int iLimit = 10, eType eType = eType.NONE)
        {
            string types = "ARTISTS,ALBUMS,TRACKS,VIDEOS,PLAYLISTS";
            if (eType == eType.ALBUM)
                types = "ALBUMS";
            if (eType == eType.ARTIST)
                types = "ARTISTS";
            if (eType == eType.TRACK)
                types = "TRACKS";
            if (eType == eType.VIDEO)
                types = "VIDEOS";
            if (eType == eType.PLAYLIST)
                types = "PLAYLISTS";

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "query", sTex },
                { "offset", "0" },
                { "types", types },
                { "limit", iLimit.ToString()},
            };
            (string msg, string res) = await Request(oKey, "search", data);
            if (msg.IsNotBlank() || res.IsBlank())
                return (msg, null);

            SearchResult result = new SearchResult();
            result.Artists = JsonHelper.ConverStringToObject<ObservableCollection<Artist>>(res, "artists", "items");
            result.Albums = JsonHelper.ConverStringToObject<ObservableCollection<Album>>(res, "albums", "items");
            result.Tracks = JsonHelper.ConverStringToObject<ObservableCollection<Track>>(res, "tracks", "items");
            result.Videos = JsonHelper.ConverStringToObject<ObservableCollection<Video>>(res, "videos", "items");
            result.Playlists = JsonHelper.ConverStringToObject<ObservableCollection<Playlist>>(res, "playlists", "items");
            return (null, result);
        }

        public static async Task<(string, eType, object)> Get(LoginKey oKey, string sTex, eType intype = eType.NONE, int iLimit = 10, bool GetArtistEPSingle = true, bool bGetArtistItems = false)
        {
            (string id, eType type) = ParseUrl(sTex);
            if(intype != eType.NONE)
            {
                type = intype;
                id = sTex;
            }
            string msg = null;
            object ret = null;

            //jump
            if (type != eType.NONE)
            {
                switch (type)
                {
                    case eType.ARTIST: goto POINT_ARTIST;
                    case eType.ALBUM: goto POINT_ALBUM;
                    case eType.TRACK: goto POINT_TRACK;
                    case eType.VIDEO: goto POINT_VIDEO;
                }
            }
            if (AIGS.Common.Convert.ConverStringToInt(id, -1) == -1)
                goto POINT_SEARCH;

            POINT_ALBUM:
            {
                (msg, ret) = await GetAlbum(oKey, id);
                if (ret != null)
                    return (msg, eType.ALBUM, ret);
            }
            POINT_TRACK:
            {
                (msg, ret) = await GetTrack(oKey, id);
                if (ret != null)
                    return (msg, eType.TRACK, ret);
            }
            POINT_VIDEO:
            {
                (msg, ret) = await GetVideo(oKey, id);
                if (ret != null)
                    return (msg, eType.VIDEO, ret);
            }
            POINT_ARTIST:
            {
                (msg, ret) = await GetArtist(oKey, id, GetArtistEPSingle, bGetArtistItems);
                if (ret != null)
                    return (msg, eType.ARTIST, ret);
            }

            POINT_SEARCH:
            {
                if (id.Contains("-"))
                {
                    (msg, ret) = await GetPlaylist(oKey, id);
                    if (ret != null)
                        return (msg, eType.PLAYLIST, ret);
                }

                (msg, ret) = await Search(oKey, sTex, iLimit);
                if (ret != null)
                    return (msg, eType.SEARCH, ret);

                return ("Search for nothing!", eType.NONE, null);
            }
        }

    }
}
