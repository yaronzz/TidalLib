﻿using AIGS.Helper;
using System.Collections.ObjectModel;

namespace TidalLib
{
    public class LoginKey
    {
        public string UserID { get; set; }
        public string CountryCode { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiresIn { get; set; }
    }

    public class TidalDeviceCode
    {
        public string DeviceCode { get; set; }
        public string UserCode { get; set; }
        public string VerificationUri { get; set; }
        public int ExpiresIn { get; set; }
        public int Interval { get; set; }
    }

    public class Album
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public int Duration { get; set; }
        public bool StreamReady { get; set; }
        public string StreamStartDate { get; set; }
        public bool AllowStreaming { get; set; }
        public bool PremiumStreamingOnly { get; set; }
        public int NumberOfTracks { get; set; }
        public int NumberOfVideos { get; set; }
        public int NumberOfVolumes { get; set; }
        public string ReleaseDate { get; set; }
        public string Copyright { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }
        public string Url { get; set; }
        public string Cover { get; set; }
        public string VideoCover { get; set; }
        public bool Explicit { get; set; }
        public string Upc { get; set; }
        public int Popularity { get; set; }
        public string AudioQuality { get; set; }
        public Artist Artist { get; set; }
        public string[] AudioModes { get; set; }

        public string DurationStr { get { return TimeHelper.ConverIntToString(Duration); } }
        public string CoverUrl { get { return Client.GetCoverUrl(Cover); } }
        public string CoverHighUrl { get { return Client.GetCoverUrl(Cover, "1280", "1280"); } }
        public string ArtistsName { get { return Client.GetArtistsName(Artists); } }
        public string Flag { get { return Client.GetFlag(this, eType.ALBUM, false); } }
        public string FlagShort { get { return Client.GetFlag(this, eType.ALBUM, true); } }

        public ObservableCollection<Artist> Artists { get; set; }
        public ObservableCollection<Track> Tracks { get; set; }
        public ObservableCollection<Video> Videos { get; set; }
    }

    public class Artist
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public string Picture { get; set; }
        public int Popularity { get; set; }
        public string[] ArtistTypes { get; set; }

        public string CoverUrl { get { return Client.GetCoverUrl(Picture); } }
        public string CoverHighUrl { get { return Client.GetCoverUrl(Picture, "750", "750"); } }

        public ObservableCollection<Album> Albums { get; set; }
    }

    public class Track
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public int Duration { get; set; }
        public string ReplayGain { get; set; }
        public string Peak { get; set; }
        public bool AllowStreaming { get; set; }
        public bool StreamReady { get; set; }
        public string StreamStartDate { get; set; }
        public bool PremiumStreamingOnly { get; set; }
        public int TrackNumber { get; set; }
        public int VolumeNumber { get; set; }
        public string Version { get; set; }
        public int Popularity { get; set; }
        public string Copyright { get; set; }
        public string Url { get; set; }
        public string Isrc { get; set; }
        public bool Editable { get; set; }
        public bool Explicit { get; set; }
        public string AudioQuality { get; set; }
        public Artist Artist { get; set; }
        public Album Album { get; set; }
        public string[] AudioModes { get; set; }

        public string DisplayTitle { get { return Client.GetDisplayTitle(this); } }
        public string DurationStr { get { return TimeHelper.ConverIntToString(Duration); } }
        public string ArtistsName { get { return Client.GetArtistsName(Artists); } }
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
        public int PlayTimeLeftInMinutes { get; set; }
        public string SoundQuality { get; set; }
    }

    public class VideoStreamUrl
    {
        public string Codec { get; set; }
        public string Resolution { get; set; }
        public string[] ResolutionArray { get; set; }
        public string M3u8Url { get; set; }
    }

    public class Video
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public int Duration { get; set; }
        public string ImageID { get; set; }
        public int TrackNumber { get; set; }
        public string ReleaseDate { get; set; }
        public string Version { get; set; }
        public string Copyright { get; set; }
        public string Quality { get; set; }
        public bool Explicit { get; set; }
        public Artist Artist { get; set; }
        public Album Album { get; set; }

        public string DurationStr { get { return TimeHelper.ConverIntToString(Duration); } }
        public string CoverUrl { get { return Client.GetCoverUrl(ImageID); } }
        public string CoverHighUrl { get { return Client.GetCoverUrl(ImageID, "1280", "1280"); } }
        public string ArtistsName { get { return Client.GetArtistsName(Artists); } }
        public string Flag { get { return Client.GetFlag(this, eType.VIDEO, false); } }
        public string FlagShort { get { return Client.GetFlag(this, eType.VIDEO, true); } }

        public ObservableCollection<Artist> Artists { get; set; }
    }

    public class Playlist
    {
        public string UUID { get; set; }
        public string Title { get; set; }
        public int NumberOfTracks { get; set; }
        public int NumberOfVideos { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public string LastUpdated { get; set; }
        public string Created { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
        public string SquareImage { get; set; }
        public bool PublicPlaylist { get; set; }
        public int Popularity { get; set; }

        public string DurationStr { get { return TimeHelper.ConverIntToString(Duration); } }
        public string CoverUrl { get { return Client.GetCoverUrl(SquareImage); } }
        public string CoverHighUrl { get { return Client.GetCoverUrl(SquareImage, "1080", "1080"); } }

        public ObservableCollection<Track> Tracks { get; set; }
        public ObservableCollection<Video> Videos { get; set; }
    }

    public class SearchResult
    {
        public ObservableCollection<Artist> Artists { get; set; }
        public ObservableCollection<Album> Albums { get; set; }
        public ObservableCollection<Track> Tracks { get; set; }
        public ObservableCollection<Video> Videos { get; set; }
        public ObservableCollection<Playlist> Playlists { get; set; }
    }

    public class TrackLyrics
    {
        public string TrackID { get; set; }
        public string LyricsProvider { get; set; }
        public string ProviderCommontrackId { get; set; }
        public string ProviderLyricsId { get; set; }
        public string Lyrics { get; set; }
        public string subtitles { get; set; }
    }

}
