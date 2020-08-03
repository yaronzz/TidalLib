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
        private static string TOKEN = "wc8j_yBJd20zOmx0";
        private static string BASE_URL = "https://api.tidalhifi.com/v1/";
        private static string VERSION = "1.9.1";

        private class TidalRespon
        {
            public string Status { get; set; }
            public string SubStatus { get; set; }
            public string UserMessage { get; set; }
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
            if (result.Errresponse.IsNotBlank())
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

        private static string GetCoverUrl(string sID)
        {
            return string.Format("https://resources.tidal.com/images/{0}/{1}x{1}.jpg", sID.Replace('-', '/'), "320");
        }

        private static string GetQualityString(eSoundQuality eQuality)
        {
            switch(eQuality)
            {
                case eSoundQuality.Normal: return "LOW";
                case eSoundQuality.High: return "HIGH";
                case eSoundQuality.HiFi: return "LOSSLESS";
                case eSoundQuality.Master: return "HI_RES";
            }
            return null;
        }

        private static string GetTrackDisplayTitle(Track track)
        {
            if(track.Version != null && track.Version.IsNotBlank())
                return $"{track.Title} - {track.Version}";
            return track.Title;
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
            if (result.Errresponse.IsNotBlank())
            {
                TidalRespon respon = JsonHelper.ConverStringToObject<TidalRespon>(result.Errresponse);
                return (respon.UserMessage, null);
            }

            LoginKey key = JsonHelper.ConverStringToObject<LoginKey>(result.sData);
            key.UserName = sUserName;
            key.Password = sPassword;
            key.Proxy = oProxy;
            return (null, key);
        }


        public static async Task<(string, Album)> GetAlbum(LoginKey oKey, string ID, bool bGetItems = true)
        {
            (string msg, Album data) = await Request<Album>(oKey, "albums/" + ID);
            if (data != null)
            {
                data.CoverUrl = GetCoverUrl(data.Cover);
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
                data.CoverUrl = GetCoverUrl(data.SquareImage);
                if (bGetItems)
                    (msg, data.Tracks, data.Videos) = await GetItems(oKey, ID, eObjectType.PLAYLIST);
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
        public static async Task<(string, ObservableCollection<Track>, ObservableCollection<Video>)> GetItems(LoginKey oKey, string ID, eObjectType eType = eObjectType.ALBUM)
        {
            string type = "albums/";
            if (eType == eObjectType.PLAYLIST)
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
                    track.DisplayName = GetTrackDisplayTitle(track);
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

            data.CoverUrl = GetCoverUrl(data.Picture);

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

            foreach (var item in data.Albums)
                (msg, item.Tracks, item.Videos) = await GetItems(oKey, item.ID);
            return (null, data);
        }


        public static async Task<(string,Track)> GetTrack(LoginKey oKey, string ID)
        {
            (string msg, Track data) = await Request<Track>(oKey, "tracks/" + ID);
            if (data != null)
                data.DisplayTitle = GetTrackDisplayTitle(data);
            return (null, data);
        }

        public static async Task<(string, Video)> GetVideo(LoginKey oKey, string ID)
        {
            (string msg, Video data) = await Request<Video>(oKey, "videos/" + ID);
            if (data != null)
                data.CoverUrl = GetCoverUrl(data.ImageID);
            return (null, data);
        }

        //public static async Task<(string, StreamUrl)> GetTrackStreamUrl(LoginKey oKey, string ID, eSoundQuality eQuality)
        //{
        //    string quality = GetQualityString(eQuality);
        //    (string msg, object resp) = await Request<object>(oKey, "tracks/" + ID + "/playbackinfopostpaywall", new Dictionary<string, string>() { { "audioquality", quality }, { "playbackmode", "STREAM" }, { "assetpresentation", "FULL" } }, 3);
        //    if(resp != null)
        //    {

        //    }
        //    (string msg2, StreamUrl data) = await Request<StreamUrl>(oKey, "tracks/" + ID + "/streamUrl", new Dictionary<string, string>() { { "soundQuality", quality } });
        //}
    }

    
}
