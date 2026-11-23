using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Views;

public partial class MainWindow
{
    private TextBox? _lastTextBox;

    private void InitializeMenus(MainWindowViewModel model)
    {
        var mac = OperatingSystem.IsMacOS();
        var modifier = mac ? KeyModifiers.Meta : KeyModifiers.Control;
        var export = new RelayCommand(() => ShowBiographyExport(null, new RoutedEventArgs()));
        var close = new RelayCommand(Close);
        var undo = new RelayCommand(() => _lastTextBox?.Undo());
        var redo = new RelayCommand(() => _lastTextBox?.Redo());
        var cut = new RelayCommand(() => _lastTextBox?.Cut());
        var copy = new RelayCommand(() => _lastTextBox?.Copy());
        var paste = new RelayCommand(() => _lastTextBox?.Paste());
        var selectAll = new RelayCommand(() => _lastTextBox?.SelectAll());
        var file = new (string, ICommand, KeyGesture?)[]
        {
            ("Indlæs GEDCOM…", model.SelectGedcomFileCommand, new(Key.O, modifier)),
            ("Gem alle", model.SaveAllCommand, new(Key.S, modifier)),
            ("Gem version…", model.CommitCommand, null),
            ("Eksportér biografier…", export, null),
            ("Versionshistorik…", model.ShowVersionHistoryCommand, null),
            ("Anvend skabelon…", model.ApplyBiographyTemplateCommand, null),
            ("Indstillinger…", model.OpenSettingsCommand, new(Key.OemComma, modifier)),
            ("Luk", close, null),
        };
        var edit = new (string, ICommand, KeyGesture?)[]
        {
            ("Fortryd", undo, new(Key.Z, modifier)),
            ("Gentag", redo, new(mac ? Key.Z : Key.Y, mac ? modifier | KeyModifiers.Shift : modifier)),
            ("Klip", cut, new(Key.X, modifier)),
            ("Kopiér", copy, new(Key.C, modifier)),
            ("Indsæt", paste, new(Key.V, modifier)),
            ("Markér alt", selectAll, new(Key.A, modifier)),
        };
        var help = new (string, ICommand, KeyGesture?)[]
        {
            ("Introduktion", model.ShowIntroductionCommand, null),
            ("Om", model.ShowAboutCommand, null),
            ("Markdown-hjælp", model.ShowMarkdownCheatSheetCommand, null),
            ("Hjælp til skabeloner", model.ShowTemplateCheatSheetCommand, null),
            ("Introduktion til GEDCOM", model.ShowGedcomHelpCommand, null),
        };

        if (mac)
        {
            var native = new NativeMenu();
            if (Application.Current is { } app)
            {
                var appMenu = NativeMenu.GetMenu(app);
                if (appMenu is null)
                {
                    appMenu = new NativeMenu();
                    NativeMenu.SetMenu(app, appMenu);
                }
                var settings = appMenu.Items.OfType<NativeMenuItem>()
                    .FirstOrDefault(item => item.Header == "Indstillinger…");
                if (settings is null)
                {
                    settings = new NativeMenuItem { Header = "Indstillinger…", Gesture = new KeyGesture(Key.OemComma, modifier) };
                    appMenu.Items.Insert(0, settings);
                }
                settings.Command = model.OpenSettingsCommand;
            }
            AddNativeGroup(native, "Fil", file.Where(item => item.Item1 != "Indstillinger…").ToArray());
            AddNativeGroup(native, "Rediger", edit);
            AddNativeGroup(native, "Hjælp", help);
            NativeMenu.SetMenu(this, native);
            WindowMenu.IsVisible = false;
        }
        else
        {
            WindowMenu.Items.Clear();
            KeyBindings.Clear();
            AddWindowGroup("_Fil", file);
            AddWindowGroup("_Rediger", edit);
            AddWindowGroup("_Hjælp", help);
            foreach (var (_, command, gesture) in file)
                if (gesture is not null) KeyBindings.Add(new KeyBinding { Gesture = gesture, Command = command });
        }
    }

    private void AddWindowGroup(string title, (string Label, ICommand Command, KeyGesture? Gesture)[] entries)
    {
        var group = new MenuItem { Header = title };
        foreach (var (label, command, gesture) in entries)
            group.Items.Add(new MenuItem { Header = label, Command = command, InputGesture = gesture });
        WindowMenu.Items.Add(group);
    }

    private static void AddNativeGroup(NativeMenu parent, string title, (string Label, ICommand Command, KeyGesture? Gesture)[] entries)
    {
        var menu = new NativeMenu();
        foreach (var (label, command, gesture) in entries)
            menu.Items.Add(new NativeMenuItem { Header = label, Command = command, Gesture = gesture });
        parent.Items.Add(new NativeMenuItem { Header = title, Menu = menu });
    }

    private void RememberTextFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox textBox) _lastTextBox = textBox;
        else if (e.Source is Control control && control is not MenuItem && control is not Menu)
            _lastTextBox = null;
    }
}
