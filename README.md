# TidalLib
Unofficial C# API for TIDAL music streaming service.

## 🖥 Installation

OpenTidl is now available on NuGet

```
PM> Install-Package TidalLib
```

## 🤖 Example 

### ☑️ Login

```c#
//login by email and password
(string msg1, LoginKey key1) = await Client.Login("xxxx@xx.com", "xxxxxx");
//login by your accesstoken
(string msg2, LoginKey key2) = await Client.Login("your_accctoken");
```
### ☑️ Get Album\Track\Video\Playlist\Artist

```c#
(string msg, LoginKey key) = await Client.Login("xxxx@xx.com", "xxxxxx");
(string msg1, Album album) = await Client.GetAlbum(key, "120929182");
(string msg2, Playlist playlist) = await Client.GetPlaylist(key, "6896171c-2b4a-47bf-b044-ae3886a521d7");
(string msg3, Artist artist) = await Client.GetArtist(key, "8292198");
(string msg4, Track track) = await Client.GetTrack(key, "90521281");
(string msg5, StreamUrl stream) = await Client.GetTrackStreamUrl(key, "90521281", eAudioQuality.Master);
(string msg6, Video video) = await Client.GetVideo(key, "84094460");
(string msg7, VideoStreamUrl vstream) = await Client.GetVideStreamUrl(key, "124586613", eVideoQuality.P1080);
```

