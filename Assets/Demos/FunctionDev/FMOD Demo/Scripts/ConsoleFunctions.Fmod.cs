using Core;
using FMODUnity;
using IngameDebugConsole;
using UnityEngine;
using Logger = Core.Logger;

public enum FmodOperation
{
    PlayEvent,
    StopEvent,
}

public partial class ConsoleFunctions
{
    [ConsoleMethod("echo", "Echoes the argument ")]
    public static void Echo(string arg)
    {
        Logger.LogInfo(arg, LogTag.Editor);
    }

    [ConsoleMethod("PlayAudioEvent", "Play Fmod Event")]
    public static void PlayAudioEvent(string eventRef)
    {
        var ef = RuntimeManager.PathToEventReference(eventRef);
        if (ef.Guid == null)
        {
            Logger.LogError("Event not found: " + eventRef, LogTag.Editor);
            return;
        }
        else
        {
            Logger.LogInfo(
                "Event found: " + eventRef + "GUID: " + ef.Guid.ToString(),
                LogTag.Editor
            );
            RuntimeManager.PlayOneShot(ef);
        }
    }

    [ConsoleMethod("StopAudioEvent", "Stop Fmod Event")]
    public static void StopAudioEvent()
    {
        RuntimeManager.PauseAllEvents(true);
    }
}
