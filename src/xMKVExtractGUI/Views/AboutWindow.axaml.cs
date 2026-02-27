using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using xMKVExtractGUI.ViewModels;

namespace xMKVExtractGUI.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var closeBtn  = this.FindControl<Button>("CloseBtn");
        var githubBtn = this.FindControl<Button>("GitHubBtn");

        if (closeBtn != null)
            closeBtn.Click += (_, _) => Close();

        if (githubBtn != null && DataContext is AboutViewModel vm)
            githubBtn.Click += async (_, _) =>
            {
                try
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel != null)
                    {
                        await topLevel.Launcher.LaunchUriAsync(new Uri(vm.GitHubUrl));
                    }
                }
                catch { /* ignores if the system does not have a default browser defined */ }
            };
    }
}