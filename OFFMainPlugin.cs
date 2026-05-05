using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using BepInEx.Logging;
using FangamerRPG;
using HarmonyLib;

namespace OFFRestored;

[BepInPlugin(OFFPluginInfo.PLUGIN_GUID, OFFPluginInfo.PLUGIN_NAME, OFFPluginInfo.PLUGIN_VERSION)]
public class OFFMainPlugin : BaseUnityPlugin
{
    // Get self
    public static OFFMainPlugin Instance { get; private set; }

    // Logger
    internal static new ManualLogSource Logger;

    // Main config
    public static ConfigEntry<bool> ShowVersionInfoOnTitle;

    // Music config
    public static ConfigEntry<bool> ReplaceMusic;
    public static ConfigEntry<bool> DedanLoopFix;
    public static ConfigEntry<bool> DisableCreditsLoop;
    public static ConfigEntry<bool> RestoreEnochPrefightSlowdown;
    public static ConfigEntry<bool> RestoreOriginalSpeedChanges;
    public static ConfigEntry<bool> QueenPostFightOriginal;
    public static ConfigEntry<bool> BossDeathDontStopBGM;
    public static ConfigEntry<bool> DontSaveBGMProgress;

    // SFX config
    public static ConfigEntry<bool> ReplaceSFX;
    public static ConfigEntry<bool> NoATBSound;

    // Get amount of custom music and SFX for the mod info
    public static int custom_audio_count = 0;
    public static int custom_audio_error_count = 0;

    // Startup
    private void Awake()
    {
        // Initialize plugin
        Instance = this;
        Logger = base.Logger;
        Logger.LogInfo("Audio purification in progress...");

        // Set config
        SetConfig();

        // Initialize music/SFX replacement system
        OFFSoundLoader.Initialize();

        // Only show the mod version if the player wants it
        if (ShowVersionInfoOnTitle.Value)
        {
            OFFModUI.Initialize();
        }

        // Apply all Harmony patches
        var harmony = new Harmony(OFFPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        // Log plugin start complete
        Logger.LogInfo("Audversaries purified!");
    }

    // Game start
    private void Start()
    {
        // Preload all custom audio after game starts
        OFFSoundLoader.PreloadCustomAudio();
    }

    // Set config data
    private void SetConfig()
    {
        // Show mod version on title screen
        ShowVersionInfoOnTitle = Config.Bind(
            "Main",
            "ShowVersionInfoOnTitle",
            true,
            "If true, The mod name and version will be shown on the top-left of the title screen."
        );

        // Replace Music
        ReplaceMusic = Config.Bind(
            "Music",
            "ReplaceMusic",
            true,
            "If true, music will be replaced with anything in BepInEx/plugins/OFFRestored/Music"
        );
        // Dedan fix
        DedanLoopFix = Config.Bind(
            "Music",
            "DedanLoopFix",
            true,
            "If true, removes the hardcoded loop points for Dedan's battle theme (Dedan2.wav). If you're not replacing that file, set this to false."
        );
        // Credits loop removal
        DisableCreditsLoop = Config.Bind(
            "Music",
            "DisableCreditsLoop",
            true,
            "If true, The credits music will not loop."
        );
        // Queen post-fight restoration
        QueenPostFightOriginal = Config.Bind(
            "Music",
            "QueenPostFightOriginal",
            true,
            "If true, The Woman of Your Dreams (or its counterpart) will be restored in the cutscene after Queen is defeated, instead of playing 50% speed Silence (or its counterpart)."
        );
        // all Burned Bodies variants restored
        RestoreEnochPrefightSlowdown = Config.Bind(
            "Music",
            "RestoreEnochPrefightSlowdown",
            true,
            "If true, Enoch's battle theme will be slowed down in the prefight cutscene, like in the original game."
        );
        // bosses don't stop music during their death animations
        BossDeathDontStopBGM = Config.Bind(
            "Music",
            "BossDeathDontStopBGM",
            false,
            "If true, battle music will not pause during boss defeat animations. This also means Tender Sugar won't restart after winning."
        );
        // pitch-shifted music behaves like in French version
        RestoreOriginalSpeedChanges = Config.Bind(
            "Music",
            "RestoreOriginalSpeedChanges",
            true,
            "If true, tracks that were originally just sped-up or slowed-down of other tracks in the original French version will be restored to that form, making them transition as they did originally before the English fan translation."
        );
        // bgm progress doesn't save
        DontSaveBGMProgress = Config.Bind(
            "Music",
            "DontSaveBGMProgress",
            false,
            "If true, disables the remake's new feature where overworld music continues from where it left off after battles instead of starting over from the beginning of the track (this doesn't change the behavior when battle music and overworld music are the same)"
        );

        // Replace SFX
        ReplaceSFX = Config.Bind(
            "SFX",
            "ReplaceSFX",
            true,
            "If true, sound effects will be replaced with anything in BepInEx/plugins/OFFRestored/SFX"
        );
        // ATB removal
        NoATBSound = Config.Bind(
            "SFX",
            "NoATBSound",
            true,
            "If true, removes the sound that plays at the start of an ally's turn in battle."
        );
    }

    public static (OFFMusicTracks, float) ConvertSpeed(OFFMusicTracks track) => track switch
    {
        OFFMusicTracks.BrainPlagueSlowRewind => (OFFMusicTracks.BrainPlagueRewind, 0.6f),
        OFFMusicTracks.TheRaceOfAThousandAntsOhno => (OFFMusicTracks.TheRaceOfAThousandAnts, 0.8f),
        OFFMusicTracks.TheRaceofAThousandAntsSafe => (OFFMusicTracks.TheRaceOfAThousandAnts, 0.5f),
        OFFMusicTracks.Stille => (OFFMusicTracks.Silence, 0.5f),
        OFFMusicTracks.Shhhhhh => (OFFMusicTracks.Silencio, 1.5f),
        OFFMusicTracks.TheWallsAreListeningCliff => (OFFMusicTracks.TheWallsAreListeningCliff, 0.6f),
        OFFMusicTracks.ClockworkLostGripOfTime => (OFFMusicTracks.Clockwork, 0.6f),
        OFFMusicTracks.EndlessHallwayStuck => (OFFMusicTracks.EndlessHallway, 0.5f),
        OFFMusicTracks.FourteenFakeResidents => (OFFMusicTracks.FourteenResidents, 0.8f),
        OFFMusicTracks.FourteenResidentsOFFTitle => (OFFMusicTracks.FourteenResidents, 0.5f),
        OFFMusicTracks.BurnedBodiesOut => (OFFMusicTracks.BurnedBodies, 0.8f),
        OFFMusicTracks.BurnedBodiesThe => (OFFMusicTracks.BurnedBodies, 0.7f),
        OFFMusicTracks.BurnedBodiesChimney => (OFFMusicTracks.BurnedBodies, 0.6f),
        OFFMusicTracks.BurnedBodiesSweetTooth => (OFFMusicTracks.BurnedBodies, 0.5f),
        OFFMusicTracks.YesterdayWasEvenBetter => (OFFMusicTracks.YesterdayWasBetter, 0.8f),
        OFFMusicTracks.TodayIsWorstSweet => (OFFMusicTracks.TodayIsWorst, 0.8f),
        _ => (track, 0f)
    };
}