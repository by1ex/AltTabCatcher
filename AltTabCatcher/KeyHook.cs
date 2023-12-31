﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AltTabCatch;

public class KeyHookEventArgs
{
    public readonly Keys Key;
    public bool Handled;

    public KeyHookEventArgs(Keys key) => Key = key;
}

public class KeyHook : IDisposable
{
    public event Action<KeyHookEventArgs> KeyEvent;

    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
    private readonly LowLevelKeyProc hookProc;

    private readonly IntPtr hookId;
    private bool disposed;

    public KeyHook()
    {
        hookProc = HookProc;
        using Process curProcess = Process.GetCurrentProcess();
        using ProcessModule curModule = curProcess.MainModule;

        hookId = SetWindowsHookEx(13, hookProc, GetModuleHandle(curModule.ModuleName), 0);
    }

    ~KeyHook() => Dispose();

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        UnhookWindowsHookEx(hookId);
        disposed = true;
        GC.SuppressFinalize(this);
    }

    private IntPtr HookProc(int nCode, IntPtr wParam, ref LowLevelKeyStruct lParam)
    {
        if (HookProcInner(nCode, (WindowMessages)wParam, ref lParam))
        {
            return (IntPtr)1;
        }

        return CallNextHookEx(hookId, nCode, wParam, ref lParam);
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private bool HookProcInner(int nCode, WindowMessages wParam, ref LowLevelKeyStruct lParam)
    {
        if (nCode < 0)
        {
            return false;
        }

        if (lParam.Flags.HasFlag(LowLevelKeyFlags.Injected))
        {
            return false;
        }

        bool alt = lParam.Flags.HasFlag(LowLevelKeyFlags.AltDown);
        Keys key = alt ? lParam.VkCode | Keys.Alt : lParam.VkCode;
        KeyHookEventArgs e = new KeyHookEventArgs(key);
        KeyEvent?.Invoke(e);
        return e.Handled;
    }

    private struct LowLevelKeyStruct
    {
        public Keys VkCode;
        public int ScanCode;
        public LowLevelKeyFlags Flags;
        public int Time;
        public int DwExtraInfo;
    }

    [Flags]
    private enum LowLevelKeyFlags
    {
        Injected = 16,
        AltDown = 32
    }

    private delegate IntPtr LowLevelKeyProc(int nCode, IntPtr wParam, ref LowLevelKeyStruct lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyProc lpfn, IntPtr hMod, int dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref LowLevelKeyStruct lParam);
}