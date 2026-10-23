using System.Diagnostics;

namespace CursorEqualizer;

internal static class ProjectLinks
{
    internal const string Author = "MelvynPals";
    internal static readonly Uri Profile = new("https://github.com/MelvynPals");
    internal static readonly Uri Feedback = new("https://github.com/MelvynPals/win-cursor-equalizer/issues/new?" +
        "title=" + Uri.EscapeDataString("Feedback: ") + "&body=" + Uri.EscapeDataString(
            "## Issue or suggestion\n\nDescribe what happened or what you would like improved.\n\n" +
            "## Setup\n\n- Windows version:\n- Display scaling:\n"));
    internal static readonly Uri PayPal = new("https://paypal.me/emjaypals");

    internal static void Open(Uri destination)
    {
        if (destination.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Project links must use HTTPS.", nameof(destination));
        Process.Start(new ProcessStartInfo(destination.AbsoluteUri) { UseShellExecute = true });
    }
}
