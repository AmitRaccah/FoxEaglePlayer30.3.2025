using System;

/// <summary>
/// A static “messaging bus” for simple one‑string story events.
/// Any script can raise an event; any script can subscribe.
/// </summary>
public static class StoryEventChannel
{
    /// <param name="string">The event name (e.g. "EagleReveal").</param>
    public static event Action<string> OnStoryEvent;

    /// <summary>Raise a story event from anywhere.</summary>
    public static void Raise(string eventName) => OnStoryEvent?.Invoke(eventName);
}
