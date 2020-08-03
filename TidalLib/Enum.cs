using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TidalLib
{
    public enum eSoundQuality
    {
        Normal,
        High,
        HiFi,
        Master,
    }

    public enum eResolution
    {
        P240 = 240,
        P360 = 360,
        P480 = 480,
        P720 = 720,
        P1080 = 1080,
    }

    public enum eObjectType
    {
        ALBUM,
        ARTIST,
        PLAYLIST,
        TRACK,
        VIDEO,
        SEARCH,
        NONE,
    }

    public enum eContributorRole
    {
        PRODUCER,
        COMPOSER,
        LYRICIST,
        ASSOCIATED_PERFORMER,
        BACKGROUND_VOCAL,
        BASS,
        DRUMS,
        GUITAR,
        MASTERING_ENGINEER,
        MIX_ENGINEER,
        PERCUSSION,
        SYNTHESIZER,
        VOCAL,
        PERFORMER,
        REMIXER,
        ENSEMBLE_ORCHESTRA,
        CHOIR,
        CONDUCTOR,
        ELSE,
    }
}
