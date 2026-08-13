using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;

namespace SlaegtsAssistent.App.Services;

public static class SafePreviewWebViewController
{
    public static void Attach(NativeWebView webView, Window owner)
    {
        ArgumentNullException.ThrowIfNull(webView);
        ArgumentNullException.ThrowIfNull(owner);

        webView.NavigationStarted += async (_, eventArgs) =>
        {
            if (eventArgs.Request is null)
            {
                return;
            }

            var decision = PreviewNavigationPolicy.Evaluate(eventArgs.Request);
            if (decision.AllowInPreview)
            {
                return;
            }

            eventArgs.Cancel = true;
            if (decision.RequiresConfirmation)
            {
                await ShowExternalDestinationAsync(owner, eventArgs.Request);
            }
        };
        webView.NewWindowRequested += async (_, eventArgs) =>
        {
            eventArgs.Handled = true;
            if (eventArgs.Request is null)
            {
                return;
            }

            var decision = PreviewNavigationPolicy.Evaluate(eventArgs.Request);
            if (decision.RequiresConfirmation)
            {
                await ShowExternalDestinationAsync(owner, eventArgs.Request);
            }
        };
        webView.WebResourceRequested += (_, eventArgs) =>
        {
            if (!PreviewNavigationPolicy.AllowsResource(eventArgs.Request.Uri))
            {
                webView.Stop();
            }
        };
    }

    private static async Task ShowExternalDestinationAsync(Window owner, Uri destination)
    {
        var copyButton = new Button
        {
            Content = "Kopiér destination",
            MinWidth = 145,
        };
        var closeButton = new Button
        {
            Content = "Luk",
            MinWidth = 90,
            IsCancel = true,
        };
        var dialog = new Window
        {
            Title = "Eksternt link blokeret i preview",
            Width = 560,
            Height = 260,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Grid
            {
                Margin = new Thickness(18),
                RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
                RowSpacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Previewet åbner aldrig eksterne links automatisk.",
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        [Grid.RowProperty] = 1,
                        Text = "Destination:",
                    },
                    new TextBox
                    {
                        [Grid.RowProperty] = 2,
                        Text = destination.AbsoluteUri,
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new StackPanel
                    {
                        [Grid.RowProperty] = 3,
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { closeButton, copyButton },
                    },
                },
            },
        };

        copyButton.Click += async (_, _) =>
        {
            if (owner.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(destination.AbsoluteUri);
            }

            dialog.Close();
        };
        closeButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(owner);
    }
}
