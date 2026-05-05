using System;
using System.Collections.Generic;
using DG.Tweening;
using FangamerRPG;
using HarmonyLib;
using OFFGame.Battle;
using UnityEngine;

namespace OFFRestored
{
    // Replace sound, reusable
    class ReplaceSound
    {
        public static void Replace(ScriptedAudioClip sound, string type)
        {
            // Before playing anything, check if we should replace the clip
            if (sound != null && sound.clip != null)
            {
                // Load new audio
                AudioClip new_audio = OFFSoundLoader.GetCustomClip(sound.clip, type);

                // Set new audio as replacement
                if (new_audio != null)
                {
                    Debug.Log($"Found replacement for {sound.clip.name}");
                    sound.clip = new_audio;
                }
            }
        }
    }

    // Patch: Dedan theme fix
    [HarmonyPatch(typeof(CrossFadableAudio), "CrossFade")]
    class DedanThemePatch
    {
        static bool Prefix(CrossFadableAudio __instance, ScriptedAudioClip sound)
        {
            // Skips the fix if the player doesn't want it
            if (!OFFMainPlugin.DedanLoopFix.Value)
            {
                return true;
            }

            // Access private fields
            var traverse = Traverse.Create(__instance);
            traverse.Field("_isLooping").SetValue(false);

            if (sound.fadeDuration <= 0f)
            {
                __instance.Play(sound);
                return false;
            }

            List<Tween> list = DOTween.TweensByTarget(__instance.primarySource, false, null);
            if (list != null && list.Count > 2)
            {
                __instance.Stop();
            }

            if (!__instance.primarySource.isPlaying)
            {
                ScriptedAudioClip.CopyClipSettingsToSource(ref sound, ref __instance.primarySource);
                // Replaces:
                //__instance.FadeIn(__instance.primarySource, sound, sound.fadeDuration);
                traverse.Method("FadeIn", __instance.primarySource, sound, sound.fadeDuration).GetValue();
                return false;
            }

            // Replaces:
            //AudioSource audioSource = __instance.CreateCrossFadeSource();
            AudioSource audioSource = traverse.Method("CreateCrossFadeSource").GetValue<AudioSource>();
            audioSource.Play();
            ScriptedAudioClip.CopyClipSettingsToSource(ref sound, ref __instance.primarySource);
            __instance.primarySource.volume = 0f;

            // Replaces:
            //__instance.FadeOut(audioSource, sound.fadeDuration, true);
            traverse.Method("FadeOut", [
                typeof(AudioSource),
                typeof(float),
                typeof(bool)
            ], [
                audioSource,
                sound.fadeDuration,
                true
            ]).GetValue();

            // Replaces:
            //__instance.FadeIn(__instance.primarySource, sound, sound.fadeDuration);
            traverse.Method("FadeIn", __instance.primarySource, sound, sound.fadeDuration).GetValue();

            return false;
        }
    }

    // Patch: Remove ATB sound
    [HarmonyPatch(typeof(BATUnit), "PlayReadySFX")]
    class ATBSoundPatch
    {
        static bool Prefix()
        {
            // Skips removing it if the player wants the sound
            if (!OFFMainPlugin.NoATBSound.Value)
            {
                return true;
            }
            return false;
        }
    }

    // Patch: Music replacement
    [HarmonyPatch(typeof(FPGAudioManager), "ChangeBGM")]
    class MusicPatch
    {
        static bool Prefix(ScriptedAudioClip sound, ref bool savePositionOfCurrentlyPlayingTrack)
        {
            if (OFFMainPlugin.DontSaveBGMProgress.Value)
                savePositionOfCurrentlyPlayingTrack = false;
            ReplaceSound.Replace(sound, "Music");

            // Continue with the rest of the function
            return true;
        }
    }

    // Disables looping for the credits music
    [HarmonyPatch(typeof(FPGAudioManager), "ChangeBGM")]
    class CreditsPatch
    {
        static void Postfix(FPGAudioManager __instance, ScriptedAudioClip sound)
        {
            // Skip if the player doesn't want to skip the credits
            if (!OFFMainPlugin.DisableCreditsLoop.Value)
            {
                return;
            }

            // Check for the credits theme, and disable the loop
            if (sound.clip.name == "Ending Song_January2025")
            {
                __instance.appBGMSource.primarySource.loop = false;
            }

            // End the function
            return;
        }
    }

    // Patch: SFX replacement
    [HarmonyPatch(typeof(FPGAudioManager), "PlaySFX")]
    class SFXPatch
    {
        static bool Prefix(ScriptedAudioClip sound)
        {
            ReplaceSound.Replace(sound, "SFX");

            // Continue with the rest of the function
            return true;
        }
    }
    
    // Changes speed-change-based tracks back to behaving that way as in the original French version of OFF, and also undo some remake changes
    [HarmonyPatch(typeof(FPGCmdPlayMusic), "Activate")]
    class PitchShiftPatch_Events
    {
        static bool Prefix(FPGCmdPlayMusic __instance)
        {
            if (OFFMainPlugin.QueenPostFightOriginal.Value && __instance.bgmTrack == OFFMusicTracks.Stille)
                return false;
            if (OFFMainPlugin.RestoreEnochPrefightSlowdown.Value &&
                __instance.bgmTrack == OFFMusicTracks.ORostoDeUmAssassinoCansado)
            {
                __instance.scriptedAudio.pitch = 0.7f;
                return true;
            }
            if (!OFFMainPlugin.RestoreOriginalSpeedChanges.Value)
                return true;
            float pitch;
            (__instance.bgmTrack, pitch) = OFFMainPlugin.ConvertSpeed(__instance.bgmTrack);
            if (pitch != 0f)
                __instance.scriptedAudio.pitch = pitch;
            return true;
        }
    }

    [HarmonyPatch(typeof(FPGGameState), "PlaySystemBGM")]
    class PitchShiftPatch_System
    {
        static bool Prefix(FPGGameState __instance, FPGSystemBGM.SystemBGMType type, bool savePositionOfCurrentlyPlayingTrack)
        {
            FPGSystemBGM fPGSystemBGM = null;
            foreach (FPGAudioOverride audioOverride in __instance.audioOverrides)
            {
                if (audioOverride.type == type)
                {
                    fPGSystemBGM = new FPGSystemBGM();
                    fPGSystemBGM.bgmTrack = audioOverride.track;
                }
            }
            if (fPGSystemBGM == null)
            {
                foreach (FPGSystemBGM systemBGM in FPGOverworldMode.instance.globalDatabase.systemBGMs)
                {
                    if (systemBGM.systemBGMType == type)
                    {
                        fPGSystemBGM = systemBGM;
                    }
                }
            }
            float pitch = 0f;
            if (OFFMainPlugin.RestoreOriginalSpeedChanges.Value)
                (fPGSystemBGM.bgmTrack, pitch) =  OFFMainPlugin.ConvertSpeed(fPGSystemBGM.bgmTrack);
            if (pitch != 0f)
                fPGSystemBGM.scriptedAudioClip.pitch = pitch;
            fPGSystemBGM.scriptedAudioClip.clip = FPGOverworldMode.instance.audioManager.musicMap.GetBGM(fPGSystemBGM.bgmTrack);
            FPGOverworldMode.instance.audioManager.ChangeBGM(fPGSystemBGM.scriptedAudioClip, loop: true, savePositionOfCurrentlyPlayingTrack);
            return false;
        }
    }

    [HarmonyPatch(typeof(FPGOverworldMode), "StartMusic")]
    class PitchShiftPatch_OverworldMode
    {
        static bool Prefix(FPGOverworldMode __instance, bool isBackFromBattle, OFFMapComponent ___m_mapComp)
        {
            float pitch = 0f;
            OFFMusicTracks track;
            if (!__instance.gameState.playerState.isInBoat && isBackFromBattle &&
                ___m_mapComp.bgmTrackBackFromBattle != (OFFMusicTracks)(-1))
            {
                if (OFFMainPlugin.RestoreOriginalSpeedChanges.Value)
                    (track, pitch) = OFFMainPlugin.ConvertSpeed(___m_mapComp.bgmTrackBackFromBattle);
                else
                    track = ___m_mapComp.bgmTrackBackFromBattle;
                ___m_mapComp.backgroundMusic.clip = __instance.audioManager.musicMap.GetBGM(track);
                if (pitch != 0f)
                    ___m_mapComp.backgroundMusic.pitch = pitch;
                __instance.audioManager.ChangeBGM(___m_mapComp.backgroundMusic, loop: true);
            }
            else if (!__instance.gameState.playerState.isInBoat)
            {
                if (OFFMainPlugin.RestoreOriginalSpeedChanges.Value)
                    (track, pitch) = OFFMainPlugin.ConvertSpeed(___m_mapComp.bgmTrack);
                else
                    track = ___m_mapComp.bgmTrack;
                ___m_mapComp.backgroundMusic.clip = __instance.audioManager.musicMap.GetBGM(track);
                if (pitch != 0f)
                    ___m_mapComp.backgroundMusic.pitch = pitch;
                __instance.audioManager.ChangeBGM(___m_mapComp.backgroundMusic, loop: true);
            }
            else
            {
                __instance.gameState.PlaySystemBGM(FPGSystemBGM.SystemBGMType.Boat);
            }

            return false;
        }
    }
    
    // boss death animations don't cut out the music
    [HarmonyPatch(typeof(BATBattleAnimator), "FadeOutMusic")]
    class BossDeathNoPause
    {
        static bool Prefix(BATBattleAnimator __instance)
        {
            if (OFFMainPlugin.BossDeathDontStopBGM.Value)
                return false;
            return true;
        }
    }
}