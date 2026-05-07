using System.IO;
using System.Runtime.InteropServices;

namespace Wevito.VNext.Shell;

public interface IMediaFoundationVideoEncoder
{
    Task EncodeBgraFramesAsync(
        string outputPath,
        int width,
        int height,
        int frameRate,
        IReadOnlyList<byte[]> frames,
        CancellationToken cancellationToken = default);
}

public sealed class MediaFoundationVideoEncoder : IMediaFoundationVideoEncoder
{
    private const int MfVersion = 0x00020070;
    private const int MfStartupFull = 0;
    private const int MfVideoInterlaceProgressive = 2;
    private const int DefaultBitrate = 2_000_000;
    private const long TicksPerSecond = 10_000_000;

    public Task EncodeBgraFramesAsync(
        string outputPath,
        int width,
        int height,
        int frameRate,
        IReadOnlyList<byte[]> frames,
        CancellationToken cancellationToken = default)
    {
        if (width <= 0 || height <= 0 || frameRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Video width, height, and frame rate must be positive.");
        }

        if (frames.Count == 0)
        {
            throw new ArgumentException("At least one frame is required.", nameof(frames));
        }

        var parent = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        WriteMp4(outputPath, width, height, frameRate, frames, cancellationToken);
        return Task.CompletedTask;
    }

    private static void WriteMp4(
        string outputPath,
        int width,
        int height,
        int frameRate,
        IReadOnlyList<byte[]> frames,
        CancellationToken cancellationToken)
    {
        Marshal.ThrowExceptionForHR(MFStartup(MfVersion, MfStartupFull));
        IMFSinkWriter? writer = null;
        try
        {
            Marshal.ThrowExceptionForHR(MFCreateSinkWriterFromURL(outputPath, IntPtr.Zero, IntPtr.Zero, out writer));

            Marshal.ThrowExceptionForHR(MFCreateMediaType(out var outputType));
            outputType.SetGUID(MfGuids.MF_MT_MAJOR_TYPE, MfGuids.MFMediaType_Video);
            outputType.SetGUID(MfGuids.MF_MT_SUBTYPE, MfGuids.MFVideoFormat_H264);
            outputType.SetUINT32(MfGuids.MF_MT_AVG_BITRATE, DefaultBitrate);
            outputType.SetUINT32(MfGuids.MF_MT_INTERLACE_MODE, MfVideoInterlaceProgressive);
            outputType.SetUINT64(MfGuids.MF_MT_FRAME_SIZE, PackRatio(width, height));
            outputType.SetUINT64(MfGuids.MF_MT_FRAME_RATE, PackRatio(frameRate, 1));
            outputType.SetUINT64(MfGuids.MF_MT_PIXEL_ASPECT_RATIO, PackRatio(1, 1));
            Marshal.ThrowExceptionForHR(writer.AddStream(outputType, out var streamIndex));

            Marshal.ThrowExceptionForHR(MFCreateMediaType(out var inputType));
            inputType.SetGUID(MfGuids.MF_MT_MAJOR_TYPE, MfGuids.MFMediaType_Video);
            inputType.SetGUID(MfGuids.MF_MT_SUBTYPE, MfGuids.MFVideoFormat_RGB32);
            inputType.SetUINT32(MfGuids.MF_MT_INTERLACE_MODE, MfVideoInterlaceProgressive);
            inputType.SetUINT64(MfGuids.MF_MT_FRAME_SIZE, PackRatio(width, height));
            inputType.SetUINT64(MfGuids.MF_MT_FRAME_RATE, PackRatio(frameRate, 1));
            inputType.SetUINT64(MfGuids.MF_MT_PIXEL_ASPECT_RATIO, PackRatio(1, 1));
            Marshal.ThrowExceptionForHR(writer.SetInputMediaType(streamIndex, inputType, IntPtr.Zero));
            Marshal.ThrowExceptionForHR(writer.BeginWriting());

            var frameDuration = TicksPerSecond / frameRate;
            var expectedBytes = width * height * 4;
            for (var index = 0; index < frames.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var frame = frames[index];
                if (frame.Length != expectedBytes)
                {
                    throw new InvalidOperationException($"Frame {index} has {frame.Length} bytes; expected {expectedBytes}.");
                }

                WriteFrame(writer, streamIndex, frame, index * frameDuration, frameDuration);
            }

            Marshal.ThrowExceptionForHR(writer.Finalize_());
        }
        finally
        {
            if (writer is not null)
            {
                Marshal.FinalReleaseComObject(writer);
            }

            _ = MFShutdown();
        }
    }

    private static void WriteFrame(IMFSinkWriter writer, int streamIndex, byte[] frame, long sampleTime, long sampleDuration)
    {
        Marshal.ThrowExceptionForHR(MFCreateMemoryBuffer(frame.Length, out var buffer));
        IntPtr destination = IntPtr.Zero;
        try
        {
            Marshal.ThrowExceptionForHR(buffer.Lock(out destination, out _, out _));
            Marshal.Copy(frame, 0, destination, frame.Length);
            Marshal.ThrowExceptionForHR(buffer.Unlock());
            destination = IntPtr.Zero;
            Marshal.ThrowExceptionForHR(buffer.SetCurrentLength(frame.Length));

            Marshal.ThrowExceptionForHR(MFCreateSample(out var sample));
            Marshal.ThrowExceptionForHR(sample.AddBuffer(buffer));
            Marshal.ThrowExceptionForHR(sample.SetSampleTime(sampleTime));
            Marshal.ThrowExceptionForHR(sample.SetSampleDuration(sampleDuration));
            Marshal.ThrowExceptionForHR(writer.WriteSample(streamIndex, sample));
            Marshal.FinalReleaseComObject(sample);
        }
        finally
        {
            if (destination != IntPtr.Zero)
            {
                _ = buffer.Unlock();
            }

            Marshal.FinalReleaseComObject(buffer);
        }
    }

    private static ulong PackRatio(int numerator, int denominator)
    {
        return ((ulong)(uint)numerator << 32) | (uint)denominator;
    }

    [DllImport("mfplat.dll", ExactSpelling = true)]
    private static extern int MFStartup(int version, int flags);

    [DllImport("mfplat.dll", ExactSpelling = true)]
    private static extern int MFShutdown();

    [DllImport("mfplat.dll", ExactSpelling = true)]
    private static extern int MFCreateMediaType(out IMFMediaType mediaType);

    [DllImport("mfplat.dll", ExactSpelling = true)]
    private static extern int MFCreateMemoryBuffer(int maxLength, out IMFMediaBuffer buffer);

    [DllImport("mfplat.dll", ExactSpelling = true)]
    private static extern int MFCreateSample(out IMFSample sample);

    [DllImport("mfreadwrite.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int MFCreateSinkWriterFromURL(
        string outputUrl,
        IntPtr byteStream,
        IntPtr attributes,
        out IMFSinkWriter sinkWriter);

    private static class MfGuids
    {
        public static readonly Guid MF_MT_MAJOR_TYPE = new("48EBA18E-F8C9-4687-BF11-0A74C9F96A8F");
        public static readonly Guid MF_MT_SUBTYPE = new("F7E34C9A-42E8-4714-B74B-CB29D72C35E5");
        public static readonly Guid MF_MT_AVG_BITRATE = new("20332624-FB0D-4D9E-BD0D-CBF6786C102E");
        public static readonly Guid MF_MT_INTERLACE_MODE = new("E2724BB8-E676-4806-B4B2-A8D6EFB44CCD");
        public static readonly Guid MF_MT_FRAME_SIZE = new("1652C33D-D6B2-4012-B834-72030849A37D");
        public static readonly Guid MF_MT_FRAME_RATE = new("C459A2E8-3D2C-4E44-B132-FEE5156C7BB0");
        public static readonly Guid MF_MT_PIXEL_ASPECT_RATIO = new("C6376A1E-8D0A-4027-BE45-6D9A0AD39BB6");
        public static readonly Guid MFMediaType_Video = new("73646976-0000-0010-8000-00AA00389B71");
        public static readonly Guid MFVideoFormat_H264 = new("34363248-0000-0010-8000-00AA00389B71");
        public static readonly Guid MFVideoFormat_RGB32 = new("00000016-0000-0010-8000-00AA00389B71");
    }

    [ComImport]
    [Guid("44AE0FA8-EA31-4109-8D2E-4CAE4997C555")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMFMediaType
    {
        void GetItem();
        void GetItemType();
        void CompareItem();
        void Compare();
        void GetUINT32();
        void GetUINT64();
        void GetDouble();
        void GetGUID();
        void GetStringLength();
        void GetString();
        void GetAllocatedString();
        void GetBlobSize();
        void GetBlob();
        void GetAllocatedBlob();
        void GetUnknown();
        void SetItem();
        void DeleteItem();
        void DeleteAllItems();
        void SetUINT32([In] Guid guidKey, [In] int unValue);
        void SetUINT64([In] Guid guidKey, [In] ulong unValue);
        void SetDouble();
        void SetGUID([In] Guid guidKey, [In] Guid guidValue);
    }

    [ComImport]
    [Guid("3137f1cd-fe5e-4805-a5d8-fb477448cb3d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMFSinkWriter
    {
        [PreserveSig]
        int AddStream(IMFMediaType targetMediaType, out int streamIndex);
        [PreserveSig]
        int SetInputMediaType(int streamIndex, IMFMediaType inputMediaType, IntPtr encodingParameters);
        [PreserveSig]
        int BeginWriting();
        [PreserveSig]
        int WriteSample(int streamIndex, IMFSample sample);
        [PreserveSig]
        int SendStreamTick(int streamIndex, long timestamp);
        [PreserveSig]
        int PlaceMarker(int streamIndex, IntPtr context);
        [PreserveSig]
        int NotifyEndOfSegment(int streamIndex);
        [PreserveSig]
        int Flush(int streamIndex);
        [PreserveSig]
        int Finalize_();
    }

    [ComImport]
    [Guid("045FA593-8799-42b8-BC8D-8968C6453507")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMFMediaBuffer
    {
        [PreserveSig]
        int Lock(out IntPtr buffer, out int maxLength, out int currentLength);
        [PreserveSig]
        int Unlock();
        [PreserveSig]
        int GetCurrentLength(out int currentLength);
        [PreserveSig]
        int SetCurrentLength(int currentLength);
        [PreserveSig]
        int GetMaxLength(out int maxLength);
    }

    [ComImport]
    [Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMFSample
    {
        void GetItem();
        void GetItemType();
        void CompareItem();
        void Compare();
        void GetUINT32();
        void GetUINT64();
        void GetDouble();
        void GetGUID();
        void GetStringLength();
        void GetString();
        void GetAllocatedString();
        void GetBlobSize();
        void GetBlob();
        void GetAllocatedBlob();
        void GetUnknown();
        void SetItem();
        void DeleteItem();
        void DeleteAllItems();
        void SetUINT32();
        void SetUINT64();
        void SetDouble();
        void SetGUID();
        void SetString();
        void SetBlob();
        void SetUnknown();
        void LockStore();
        void UnlockStore();
        void GetCount();
        void GetItemByIndex();
        void CopyAllItems();
        [PreserveSig]
        int GetSampleFlags(out int sampleFlags);
        [PreserveSig]
        int SetSampleFlags(int sampleFlags);
        [PreserveSig]
        int GetSampleTime(out long sampleTime);
        [PreserveSig]
        int SetSampleTime(long sampleTime);
        [PreserveSig]
        int GetSampleDuration(out long sampleDuration);
        [PreserveSig]
        int SetSampleDuration(long sampleDuration);
        [PreserveSig]
        int GetBufferCount(out int bufferCount);
        [PreserveSig]
        int GetBufferByIndex(int index, out IMFMediaBuffer buffer);
        [PreserveSig]
        int ConvertToContiguousBuffer(out IMFMediaBuffer buffer);
        [PreserveSig]
        int AddBuffer(IMFMediaBuffer buffer);
    }
}
