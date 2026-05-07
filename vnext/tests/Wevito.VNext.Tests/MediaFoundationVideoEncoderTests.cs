using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class MediaFoundationVideoEncoderTests
{
    [Fact]
    public async Task FakeEncoder_WritesDeterministicOneFrameStub()
    {
        var encoder = new FakeMediaFoundationVideoEncoder();
        var outputPath = Path.Combine(Path.GetTempPath(), "wevito-media-foundation-tests", Guid.NewGuid().ToString("N"), "clip.mp4");
        var frame = new byte[2 * 2 * 4];

        await encoder.EncodeBgraFramesAsync(outputPath, width: 2, height: 2, frameRate: 1, [frame]);

        Assert.True(File.Exists(outputPath));
        Assert.Equal("fake-mf-stub", File.ReadAllText(outputPath));
        Assert.Equal(1, encoder.FrameCount);
    }

    private sealed class FakeMediaFoundationVideoEncoder : IMediaFoundationVideoEncoder
    {
        public int FrameCount { get; private set; }

        public Task EncodeBgraFramesAsync(
            string outputPath,
            int width,
            int height,
            int frameRate,
            IReadOnlyList<byte[]> frames,
            CancellationToken cancellationToken = default)
        {
            FrameCount = frames.Count;
            var parent = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.WriteAllText(outputPath, "fake-mf-stub");
            return Task.CompletedTask;
        }
    }
}
