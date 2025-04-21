using UnityEngine;
using Cinemachine;

/// <summary>
/// Inspector‑friendly struct describing one companion reveal.
/// Add as many rows as you like in StoryDirector.reveals.
/// </summary>
[System.Serializable]
public struct CompanionRevealData
{
    [Tooltip("Event name that will trigger this reveal.")]
    public string eventName;

    [Tooltip("playerID used in CameraSwitchManager.controllerItemArray.")]
    public int playerID;

    [Tooltip("Optional one‑shot virtual camera shown during the reveal.")]
    public CinemachineVirtualCamera revealCamera;

    [Tooltip("Optional audio clip played at reveal.")]
    public AudioClip revealClip;
}
