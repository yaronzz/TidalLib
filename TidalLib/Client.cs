using AIGS.Common;
using AIGS.Helper;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
        private static string AUTH_URL = "https://auth.tidal.com/v1/oauth2";
        private static string VERSION = "1.9.1";
        private static Dictionary<string, string> API_KEY = new Dictionary<string, string>() { { "clientId", "zU4XHVVkc2tDPo4t" } , { "clientSecret", "VJKhDFqJPqvsPVNBV6ukXTJmwlvbttP7wlMlrc72se4=" } };
        //private static Dictionary<string, string> API_KEY = new Dictionary<string, string>() { { "clientId", "8SEZWa4J1NVC5U5Y" } , { "clientSecret", "owUYDkxddz+9FpvGX24DlxECNtFEMBxipU0lBfrbq60=" } };
        //private static Dictionary<string, string> API_KEY = new Dictionary<string, string>() { { "clientId", "OmDtrzFgyVVL6uW56OnFA2COiabqm" } , { "clientSecret", "zxen1r3pO0hgtOC7j6twMo9UAqngGrmRiWpV7QC1zJ8=" } };
        //private static Dictionary<string, string> API_KEY = new Dictionary<string, string>() { { "clientId", "aR7gUaTK1ihpXOEP" } , { "clientSecret", "eVWBEkuL2FCjxgjOkR3yK0RYZEbcrMXRc2l8fU3ZCdE=" } };

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

        public static (string, string) GetDefaultToken()
        {
            return ("wc8j_yBJd20zOmx0", "_DSTon1kC8pABnTw");
        }

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
            bool bDolbyAtmos = false;
            List<string> tmpList = new List<string>();


            if (type == eType.ALBUM)
            {
                Album album = (Album)data;
                if (album.AudioQuality == "HI_RES")
                    bMaster = true;
                if (album.Explicit)
                    bExplicit = true;
                if (album.AudioModes.Contains("DOLBY_ATMOS"))
                    bDolbyAtmos = true;

                tmpList.Add(album.AudioQuality);
            }
            else if (type == eType.TRACK)
            {
                Track track = (Track)data;
                if (track.AudioQuality == "HI_RES")
                    bMaster = true;
                if (track.Explicit)
                    bExplicit = true;
                if (track.AudioModes.Contains("DOLBY_ATMOS"))
                    bDolbyAtmos = true;

                tmpList.Add(track.AudioQuality);
            }
            else if (type == eType.VIDEO)
            {
                Video video = (Video)data;
                if (video.Explicit)
                    bExplicit = true;

                tmpList.Add(video.Quality);
            }

            List<string> flags = new List<string>();
            if (bShort)
            {
                if (bMaster)
                    flags.Add("M");
                if (bExplicit)
                    flags.Add("E");
                if (bDolbyAtmos)
                    flags.Add("A");
            }
            else
            {
                if (bExplicit)
                    flags.Add("Explicit");
                if (bDolbyAtmos)
                    flags.Add("Dolby Atmos");
                flags.AddRange(tmpList);
            }

            if (flags.Count < 0)
                return "";
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

        public static string GetDisplayTitle(Track track)
        {
            if(track.Version != null && track.Version.IsNotBlank())
                return $"{track.Title} ({track.Version})";
            return track.Title;
        }

        private static List<VideoStreamUrl> GetResolutionList(string url)
        {
            List<VideoStreamUrl> ret = new List<VideoStreamUrl>();
            string text = NetHelper.DownloadString(url);
            //string[] array = text.Split("#EXT-X-STREAM-INF");
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


        private static string ReadFile(string path)
        {
            try
            {
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader sr = new StreamReader(fs, System.Text.Encoding.Default);
                string ret = sr.ReadToEnd();
                sr.Close();
                fs.Close();
                return ret;
            }
            catch
            {
                return "";
            }
        }
        public static (string, LoginKey) GetAccessTokenFromTidalDesktop(string sUserID)
        {
            SystemHelper.UserFolders folders = SystemHelper.GetUserFolders();
            string path = folders.AppdataPath + "\\TIDAL\\Logs\\app.log";

            string content = ReadFile(path);
            if (content.IsBlank())
                return ("Can't read tidal desktop log file.", null);

            bool bHaveNotMatchID = false;
            List<string[]> array = new List<string[]>();
            string[] lines = content.Split("[info] - Session was changed");
            foreach (var item in lines)
            {
                string sjson = item.Split('(')[0];
                string oAuthAccessToken = JsonHelper.GetValue(sjson, "oAuthAccessToken");
                if (oAuthAccessToken.IsBlank())
                    continue;
                string oAuthRefreshToken = JsonHelper.GetValue(sjson, "oAuthRefreshToken");
                string userId = JsonHelper.GetValue(sjson, "userId");
                if (userId != sUserID)
                {
                    bHaveNotMatchID = true;
                    continue;
                }
                array.Add(new string[]{ userId, oAuthAccessToken, oAuthRefreshToken});
            }

            int count = array.Count();
            if (count <= 0)
            {
                if (bHaveNotMatchID)
                    return ("User mismatch! Please login by the same-account.", null);
                else
                    return ("Can't find accesstoken in the tidal desktop log file, please login first.", null);
            }

            LoginKey key = new LoginKey();
            key.UserID = array[count - 1][0];
            key.AccessToken = array[count - 1][1];
            return ("", key);
        }

        #endregion

        #region Login
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
                if (respon == null)
                    return (result.Errmsg, null);
                return (respon.UserMessage, null);
            }

            LoginKey key = JsonHelper.ConverStringToObject<LoginKey>(result.sData);
            key.AccessToken = sAccessToken;
            key.Proxy = oProxy;
            return (null, key);
        }

        #endregion

        #region Login by url

        public static async Task<(string, TidalDeviceCode)> GetDeviceCode(HttpHelper.ProxyInfo oProxy = null)
        {
            Result result = await HttpHelper.GetOrPostAsync(AUTH_URL + "/device_authorization", new Dictionary<string, string>(){
                {"client_id", API_KEY["clientId"]},
                {"scope","r_usr+w_usr+w_sub"}}, Proxy: oProxy);
            if (result.Success == false)
            {
                TidalRespon respon = JsonHelper.ConverStringToObject<TidalRespon>(result.Errresponse);
                if (respon == null)
                    return (result.Errmsg, null);
                return (respon.UserMessage, null);
            }

            TidalDeviceCode code = JsonHelper.ConverStringToObject<TidalDeviceCode>(result.sData);
            return (null, code);
        }

        public static async Task<(string, LoginKey)> CheckAuthStatus(TidalDeviceCode deviceCode, HttpHelper.ProxyInfo oProxy = null)
        {
            string authorization = API_KEY["clientId"] + ":" + API_KEY["clientSecret"];
            string base64 = System.Convert.ToBase64String(Encoding.Default.GetBytes(authorization));
            string header = $"Authorization: Basic {base64}";

            DateTime startTime = TimeHelper.GetCurrentTime();
            while (TimeHelper.CalcConsumeTime(startTime)/1000 < deviceCode.ExpiresIn)
            {
                Result result = await HttpHelper.GetOrPostAsync(AUTH_URL + "/token", new Dictionary<string, string>(){
                    {"client_id", API_KEY["clientId"]},
                    {"device_code", deviceCode.DeviceCode},
                    {"grant_type","urn:ietf:params:oauth:grant-type:device_code"},
                    {"scope","r_usr+w_usr+w_sub"}}, Proxy: oProxy, Header: header);
                if (result.Success == false)
                {
                    TidalRespon respon = JsonHelper.ConverStringToObject<TidalRespon>(result.Errresponse);
                    string msg = respon.UserMessage + "! ";
                    if (respon.Status == "400" && JsonHelper.GetValue(result.Errresponse, "sub_status") == "1002")
                    {
                        Thread.Sleep(1000 * deviceCode.Interval);
                        continue;
                    }
                    else
                        msg += "Error while checking for authorization. Trying again...";
                    return (msg, null);
                }

                LoginKey key = JsonHelper.ConverStringToObject<LoginKey>(result.sData);
                key.ExpiresIn = JsonHelper.GetValue(result.sData, "expires_in");
                key.RefreshToken = JsonHelper.GetValue(result.sData, "refresh_token");
                key.AccessToken = JsonHelper.GetValue(result.sData, "access_token");
                key.CountryCode = JsonHelper.GetValue(result.sData, "user", "countryCode");
                key.UserID = JsonHelper.GetValue(result.sData, "user", "userId");
                key.Proxy = oProxy;
                return (null, key);
            }

            return ("Time out.", null);
        }

        public static async Task<(string, LoginKey)> RefreshAccessToken(string refreshToken, HttpHelper.ProxyInfo oProxy = null)
        {
            string authorization = API_KEY["clientId"] + ":" + API_KEY["clientSecret"];
            string base64 = System.Convert.ToBase64String(Encoding.Default.GetBytes(authorization));
            string header = $"Authorization: Basic {base64}";

            Result result = await HttpHelper.GetOrPostAsync(AUTH_URL + "/token", new Dictionary<string, string>(){
                    {"client_id", API_KEY["clientId"]},
                    {"refresh_token", refreshToken},
                    {"grant_type","refresh_token"},
                    {"scope","r_usr+w_usr+w_sub"}}, Proxy: oProxy, Header: header);
            if (result.Success == false)
            {
                if (result.Errresponse == null)
                    return (result.Errmsg, null);

                TidalRespon respon = JsonHelper.ConverStringToObject<TidalRespon>(result.Errresponse);
                string msg = respon.UserMessage + "! ";
                if (respon.Status != "200")
                    msg += "Refresh failed. Please log in again.";
                return (msg, null);
            }

            LoginKey key = JsonHelper.ConverStringToObject<LoginKey>(result.sData);
            key.ExpiresIn = JsonHelper.GetValue(result.sData, "expires_in");
            key.RefreshToken = refreshToken;
            key.AccessToken = JsonHelper.GetValue(result.sData, "access_token");
            key.CountryCode = JsonHelper.GetValue(result.sData, "user", "countryCode");
            key.UserID = JsonHelper.GetValue(result.sData, "user", "userId");
            key.Proxy = oProxy;
            return (null, key);
        }
        #endregion

        #region Lyrics

        public static string GetLyrics(LoginKey oKey, string title, string artist)
        {
            if (title.IsBlank() || artist.IsBlank())
                return "";

            try
            {
                string paras = $"?q={title} + ',' + {artist}";
                string url = "https://api.genius.com/search";
                string header = $"Authorization:Bearer vNKbAWAE3rVY_48nRaiOrDcWNLvsxS-Z8qyG5XfEzTOtZvkTfg6P3pxOVlA2BjaW";
                string errmsg = "";
                var result = HttpHelper.GetOrPost(url + paras, 
                    out errmsg, 
                    Header: header, 
                    Retry: 3, 
                    Proxy: oKey.Proxy);

                if (errmsg.IsNotBlank())
                    return "";

                JObject jo = JObject.Parse(result.ToString());
                var songId = jo["response"]["hits"][0]["result"]["id"];
                var result2 = HttpHelper.GetOrPost($"https://api.genius.com/songs/{songId}", 
                    out errmsg, 
                    Header: header, 
                    Retry: 3, 
                    Proxy: oKey.Proxy);

                if (errmsg.IsNotBlank())
                    return "";

                jo = JObject.Parse(result2.ToString());
                var song_url = jo["response"]["song"]["url"].ToString();

            
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(song_url.ToString());
                request.Timeout = 30000;
                request.Headers.Set("Pragma", "no-cache");
                if (oKey.Proxy != null)
                    request.Proxy = HttpHelper.GetWebProxy(oKey.Proxy);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream streamReceive = response.GetResponseStream();
                //Encoding encoding = Encoding.GetEncoding("GB2312");
                //StreamReader streamReader = new StreamReader(streamReceive, encoding);
                StreamReader streamReader = new StreamReader(streamReceive);
                string strResult = streamReader.ReadToEnd();
                var doc = new HtmlDocument();
                doc.LoadHtml(strResult.Replace("<br/>", "\n"));

                string text = "";
                var node = doc.DocumentNode.SelectNodes("//div[@class='lyrics']");
                if (node != null)
                    text = node[0].InnerText;
                else
                {
                    node = doc.DocumentNode.SelectNodes("//div[contains(@class,'Lyrics__Root')]");
                    var childs = node[0].SelectNodes("//div[contains(@class,'Lyrics__Container-sc')]");
                    if (childs != null)
                    {
                        foreach(var item in childs)
                            text += item.InnerText;
                    }
                }

                if (text.IsBlank())
                    return "";

                text = text.Replace("<br>", "\n").Replace("&#x27;", "'");
                text = System.Text.RegularExpressions.Regex.Replace(text, @"(\[.*?\])*", "");
                text = text.TrimStart('\n', ' ');
                return text;
            }
            catch
            {
                return "";
            }
        }

        #endregion


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
                int iIndex = 0;
                for (int i = 0; i < list.Count(); i++)
                {
                    if (iCmp >= int.Parse(list[i].ResolutionArray[1]))
                        iIndex = i;
                    else
                        break;
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

        public static async Task<(string, SearchResult)> Search(LoginKey oKey, string sTex, int iLimit = 10, int offset = 0, eType eType = eType.NONE)
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
                { "offset", offset.ToString() },
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

        public static async Task<(string, eType, object)> Get(LoginKey oKey, 
            string sTex, 
            eType intype = eType.NONE, 
            int iLimit = 10, 
            bool GetArtistEPSingle = true, 
            bool bGetArtistItems = false,
            int iOffset = 0)
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

                (msg, ret) = await Search(oKey, sTex, iLimit, iOffset);
                if (ret != null)
                    return (msg, eType.SEARCH, ret);

                return ("Search for nothing!", eType.NONE, null);
            }
        }

    }


    //public class Test
    //{
    //    static int Main()
    //    {
    //        //string msg;
    //        //TidalDeviceCode code;
    //        //LoginKey key;
    //        //(msg, code) = Client.GetDeviceCode().Result;
    //        //(msg, key) = Client.CheckAuthStatus(code).Result;

    //        string ff = "[test] i am [jiasdkfl] yaronzz.";
    //        ff = System.Text.RegularExpressions.Regex.Replace(ff, @"[^\d]*", "");

    //        try
    //        {
    //            string strResult = File.ReadAllText(@"C:\Users\Yaron\Desktop\lyrics.html");
    //            var doc = new HtmlDocument();
    //            doc.LoadHtml(strResult.Replace("<br/>", "\n"));

    //            var ulS = doc.DocumentNode.SelectNodes("//div[contains(@class,'lyrics')]");
    //            var node = doc.DocumentNode.SelectNodes("//div[@class='lyrics']");
    //            if (node == null)
    //                node = doc.DocumentNode.SelectNodes("//div[contains(@class,'Lyrics__Root')]");

    //            return 0;
    //        }
    //        catch
    //        {
    //            return 0;
    //        }

    //        return 0;
    //    }
    //}
}


