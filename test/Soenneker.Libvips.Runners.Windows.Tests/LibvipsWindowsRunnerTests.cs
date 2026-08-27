namespace Soenneker.Libvips.Runners.Windows.Tests;

public sealed class LibvipsWindowsRunnerTests
{
    [Test]
    public void Targets_windows_x64_library()
    {
        if (Constants.Library != "Soenneker.Libvips.Windows" || Constants.RuntimeIdentifier != "win-x64")
            throw new System.InvalidOperationException("The Windows runner target is not configured correctly.");
    }
}
