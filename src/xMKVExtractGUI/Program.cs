using Avalonia;
using System;

namespace xMKVExtractGUI;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null)
        {
            Environment.SetEnvironmentVariable("AVALONIA_WINDOWING_SUBSYSTEM", "wayland");
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}