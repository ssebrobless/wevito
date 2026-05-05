using System.Runtime.InteropServices;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class WindowsAudioEndpointStatusReader : IAudioEndpointController
{
    private static readonly Guid IAudioEndpointVolumeId = new("5CDF2C82-841E-4546-9722-0CF74078229A");

    public AudioEndpointStatus ReadDefaultRenderEndpoint(DateTimeOffset inspectedAtUtc)
    {
        return ReadOrChangeDefaultRenderEndpoint(inspectedAtUtc, null);
    }

    public AudioEndpointStatus SetDefaultRenderVolume(double volumePercent, DateTimeOffset changedAtUtc)
    {
        var safePercent = Math.Clamp(volumePercent, 0d, 100d);
        return ReadOrChangeDefaultRenderEndpoint(changedAtUtc, endpointVolume =>
        {
            var context = Guid.Empty;
            Marshal.ThrowExceptionForHR(endpointVolume.SetMasterVolumeLevelScalar((float)(safePercent / 100d), ref context));
        });
    }

    public AudioEndpointStatus SetDefaultRenderMute(bool isMuted, DateTimeOffset changedAtUtc)
    {
        return ReadOrChangeDefaultRenderEndpoint(changedAtUtc, endpointVolume =>
        {
            var context = Guid.Empty;
            Marshal.ThrowExceptionForHR(endpointVolume.SetMute(isMuted, ref context));
        });
    }

    private static AudioEndpointStatus ReadOrChangeDefaultRenderEndpoint(
        DateTimeOffset inspectedAtUtc,
        Action<IAudioEndpointVolume>? change)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Unavailable("Windows Core Audio endpoint inspection is only available on Windows.", inspectedAtUtc);
        }

        object? enumeratorObject = null;
        IMMDevice? device = null;
        object? endpointVolumeObject = null;

        try
        {
            enumeratorObject = new MMDeviceEnumerator();
            var enumerator = (IMMDeviceEnumerator)enumeratorObject;
            Marshal.ThrowExceptionForHR(enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out device));

            var iid = IAudioEndpointVolumeId;
            Marshal.ThrowExceptionForHR(device.Activate(ref iid, CLSCTX.InprocServer, IntPtr.Zero, out endpointVolumeObject));
            var endpointVolume = (IAudioEndpointVolume)endpointVolumeObject;

            change?.Invoke(endpointVolume);
            Marshal.ThrowExceptionForHR(endpointVolume.GetMasterVolumeLevelScalar(out var scalar));
            Marshal.ThrowExceptionForHR(endpointVolume.GetMute(out var muted));
            var endpointId = TryGetEndpointId(device);
            var percent = Math.Round(Math.Clamp(scalar, 0f, 1f) * 100d, 1);

            return new AudioEndpointStatus(
                "Windows Core Audio default render endpoint",
                IsAvailable: true,
                MasterVolumePercent: percent,
                IsMuted: muted,
                EndpointId: endpointId,
                Detail: $"Default output volume is {percent:0.#}% and mute is {muted}.",
                inspectedAtUtc);
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or NotSupportedException)
        {
            return Unavailable($"Unable to inspect Windows default output endpoint: {ex.Message}", inspectedAtUtc);
        }
        finally
        {
            ReleaseComObject(endpointVolumeObject);
            ReleaseComObject(device);
            ReleaseComObject(enumeratorObject);
        }
    }

    private static string TryGetEndpointId(IMMDevice device)
    {
        try
        {
            Marshal.ThrowExceptionForHR(device.GetId(out var endpointId));
            return endpointId ?? "";
        }
        catch (COMException)
        {
            return "";
        }
    }

    private static AudioEndpointStatus Unavailable(string detail, DateTimeOffset inspectedAtUtc)
    {
        return new AudioEndpointStatus(
            "Windows Core Audio default render endpoint",
            IsAvailable: false,
            MasterVolumePercent: null,
            IsMuted: null,
            EndpointId: "",
            Detail: detail,
            inspectedAtUtc);
    }

    private static void ReleaseComObject(object? value)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }

    private enum EDataFlow
    {
        Render,
        Capture,
        All
    }

    private enum ERole
    {
        Console,
        Multimedia,
        Communications
    }

    [Flags]
    private enum CLSCTX
    {
        InprocServer = 0x1
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private sealed class MMDeviceEnumerator
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IntPtr ppDevices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);

        [PreserveSig]
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(IntPtr pClient);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, CLSCTX dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        [PreserveSig]
        int OpenPropertyStore(uint stgmAccess, out IntPtr ppProperties);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

        [PreserveSig]
        int GetState(out uint pdwState);
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig]
        int RegisterControlChangeNotify(IntPtr pNotify);

        [PreserveSig]
        int UnregisterControlChangeNotify(IntPtr pNotify);

        [PreserveSig]
        int GetChannelCount(out uint pnChannelCount);

        [PreserveSig]
        int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);

        [PreserveSig]
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);

        [PreserveSig]
        int GetMasterVolumeLevel(out float pfLevelDB);

        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float pfLevel);

        [PreserveSig]
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);

        [PreserveSig]
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);

        [PreserveSig]
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);

        [PreserveSig]
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);

        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);

        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);

        [PreserveSig]
        int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);

        [PreserveSig]
        int VolumeStepUp(ref Guid pguidEventContext);

        [PreserveSig]
        int VolumeStepDown(ref Guid pguidEventContext);

        [PreserveSig]
        int QueryHardwareSupport(out uint pdwHardwareSupportMask);

        [PreserveSig]
        int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }
}
