namespace FiveThreeOneTracker.Components.Shared;

/// <summary>Maps a plate weight (lbs) to the shared plate color class defined in app.css.</summary>
public static class PlateTone
{
    public static string For(double weight) => weight switch
    {
        >= 45 => "plate-tone-45",
        >= 35 => "plate-tone-35",
        >= 25 => "plate-tone-25",
        >= 15 => "plate-tone-15",
        >= 10 => "plate-tone-10",
        >= 5 => "plate-tone-5",
        _ => "plate-tone-sm"
    };
}
