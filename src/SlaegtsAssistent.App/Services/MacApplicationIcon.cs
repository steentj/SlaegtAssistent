using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace SlaegtsAssistent.App.Services;

internal static class MacApplicationIcon
{
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";
    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private static readonly Uri IconUri = new("avares://SlaegtsAssistent.App/Assets/AppIkon.png");

    public static void SetDockIcon()
    {
        using var stream = AssetLoader.Open(IconUri);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        var data = CFDataCreate(0, buffer.GetBuffer(), (nint)buffer.Length);
        if (data == 0)
        {
            return;
        }

        var image = SendMessage(GetClass("NSImage"), GetSelector("alloc"));
        image = SendMessage(image, GetSelector("initWithData:"), data);
        CFRelease(data);
        if (image == 0)
        {
            return;
        }

        var application = SendMessage(GetClass("NSApplication"), GetSelector("sharedApplication"));
        SendVoidMessage(application, GetSelector("setApplicationIconImage:"), image);
        SendVoidMessage(image, GetSelector("release"));
    }

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFDataCreate(nint allocator, byte[] bytes, nint length);

    [DllImport(CoreFoundationLibrary)]
    private static extern void CFRelease(nint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_getClass")]
    private static extern nint GetClass(string name);

    [DllImport(ObjectiveCLibrary, EntryPoint = "sel_registerName")]
    private static extern nint GetSelector(string name);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern nint SendMessage(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern nint SendMessage(nint receiver, nint selector, nint argument);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void SendVoidMessage(nint receiver, nint selector, nint argument);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void SendVoidMessage(nint receiver, nint selector);
}
