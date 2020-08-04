using AIGS.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TidalLib
{
    public class LoginKey
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string UserID { get; set; }
        public string CountryCode { get; set; }
        public string SessionID { get; set; }
        public string AccessToken { get; set; }
        public HttpHelper.ProxyInfo Proxy { get; set; }
    }

    public class Album 
    {
        public string   ID { get; set; }
        public string   Title { get; set; }
        public int      Duration { get; set; }
        public bool     StreamReady { get; set; }
        public string   StreamStartDate { get; set; }
        public bool     AllowStreaming { get; set; }
        public bool     PremiumStreamingOnly { get; set; }
        public int      NumberOfTracks { get; set; }
        public int      NumberOfVideos { get; set; }
        public int      NumberOfVolumes { get; set; }
        public string   ReleaseDate { get; set; }
        public string   Copyright { get; set; }
        public string   Type { get; set; }
        public string   Version { get; set; }
        public string   Url { get; set; }
        public string   Cover { get; set; }
        public string   VideoCover { get; set; }
        public bool     Explicit { get; set; }
        public string   Upc { get; set; }
        public int      Popularity { get; set; }
        public string   AudioQuality { get; set; }
        public Artist   Artist { get; set; }
        public string[] AudioModes { get; set; }

        public string DurationStr { get { return TimeHelper.ConverIntToString(Duration); } }
        public string CoverUrl { get { return Client.GetCoverUrl(Cover); } }
        public string ArtistsName { get { return Client.GetArtists(Artists); } }
        public string Flag { get { return Client.GetFlag(this, eType.ALBUM, false); } }
        public string FlagShort { get { return Client.GetFlag(this, eType.ALBUM, true); } }

        public ObservableCollection<Artist> Artists { get; set; }
        public ObservableCollection<Track> Tracks { get; set; }
        public ObservableCollection<Video> Videos { get; set; }
    }

    public class Artist 
    {
        public string   ID { get; set; }
        public string   Name { get; set; }
        public string   Type { get; set; }
        public string   Url { get; set; }
        public string   Picture { get; set; }
        public int      Popularity { get; set; }
        public string[] ArtistTypes { get; set; }

        public string CoverUrl { get { return Client.GetCoverUrl(Picture); } }

        public ObservableCollection<Album> Albums { get; set; }
    }

    public class Track 
    {
        public string   ID { get; set; }
        public string   Title { get; set; }
        public string   DisplayTitle { get; set; }
        public int      Duration { get; set; }
        public string   ReplayGain { get; set; }
        public string   Peak { get; set; }
        public bool     AllowStreaming { get; set; }
        public bool     StreamReady { get; set; }
        public string   StreamStartDate { get; set; }
        public bool     PremiumStreamingOnly { get; set; }
        public int      TrackNumber { get; set; }
        public int      VolumeNumber { get; set; }
        public string   Version { get; set; }
        public int      Popularity { get; set; }
        public string   Copyright { get; set; }
        public string   Url { get; set; }
        public string   Isrc { get; set; }
        public bool     Editable { get; set; }
        public bool     Explicit { get; set; }
        public string   AudioQuality { get; set; }
        public Artist   Artist { get; set; }
        public Album    Album { get; set; }
        public string[] AudioModes { get; set; }

        public string DurationStr { get { return TimeHelper.ConverIntToString(Duration); } }
        public string ArtistsName { get { return Client.GetArtists(Artists); } }
        public string Flag { get { return Client.GetFlag(this, eType.TRACK, false); } }
        public string FlagShort { get { return Client.GetFlag(this, eType.TRACK, true); } }

        public ObservableCollection<Artist> Artists { get; set; }
    }

    public class StreamUrl 
    {
        public string TrackID { get; set; }
        public string Url { get; set; }
        public string Codec { get; set; }
        public string EncryptionKey { get; set; }
        public int    PlayTimeLeftInMinutes { get; set; }
        public string SoundQuality { get; set; }
    }

    public class VideoStreamUrl 
    {
        public string   Codec { get; set; }
        public string   Resolution { get; set; }
        public string[] ResolutionArray { get; set; }
        public string   M3u8Url { get; set; }
    }

    public class Video 
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public int    Duration { get; set; }
        public string ImageID { get; set; }
        public int    TrackNumber { get; set; }
        public string ReleaseDate { get; set; }
        public string Version { get; set; }
        public string Copyright { get; set; }
        public string Quality { get; set; }
        public bool   Explicit { get; set; }
        public Artist Artist { get; set; }
        public Album  Album { get; set; }

        public string DurationStr { get { return TimeHelper.ConverIntToString(Duration); } }
        public string CoverUrl { get { return Client.GetCoverUrl(ImageID); } }
        public string ArtistsName { get { return Client.GetArtists(Artists); } }
        public string Flag { get { return Client.GetFlag(this, eType.VIDEO, false); } }
        public string FlagShort { get { return Client.GetFlag(this, eType.VIDEO, true); } }

        public ObservableCollection<Artist> Artists { get; set; }
    }

    public class Playlist 
    {
        public string UUID { get; set; }
        public string Title { get; set; }
        public int    NumberOfTracks { get; set; }
        public int    NumberOfVideos { get; set; }
        public string Description { get; set; }
        public int    Duration { get; set; }
        public string LastUpdated { get; set; }
        public string Created { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
        public string SquareImage { get; set; }
        public bool   PublicPlaylist { get; set; }
        public int    Popularity { get; set; }

        public string DurationStr { get { return TimeHelper.ConverIntToString(Duration); } }
        public string CoverUrl { get { return Client.GetCoverUrl(SquareImage); } }

        public ObservableCollection<Track> Tracks { get; set; }
        public ObservableCollection<Video> Videos { get; set; }
    }

    public class SearchResult
    {
        public ObservableCollection<Artist>   Artists { get; set; }
        public ObservableCollection<Album>    Albums { get; set; }
        public ObservableCollection<Track>    Tracks { get; set; }
        public ObservableCollection<Video>    Videos { get; set; }
        public ObservableCollection<Playlist> Playlists { get; set; }
    }
}
