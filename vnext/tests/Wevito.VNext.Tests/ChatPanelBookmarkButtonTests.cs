using System.Threading;
using System.Windows;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class ChatPanelBookmarkButtonTests
{
    [Fact]
    public void RendersOnAssistantMessages()
    {
        var assistant = new ChatMessageViewModel("assistant", "useful answer", false, "", "", "");
        var user = new ChatMessageViewModel("user", "question", false, "", "", "");

        Assert.True(assistant.CanBookmarkForBenchmark);
        Assert.Equal(Visibility.Visible, assistant.BenchmarkBookmarkVisibility);
        Assert.False(user.CanBookmarkForBenchmark);
        Assert.Equal(Visibility.Collapsed, user.BenchmarkBookmarkVisibility);
    }

    [Fact]
    public void OpensInlineEditorOnClick()
    {
        RunSta(() =>
        {
            var panel = new ChatPanel();
            var assistant = new ChatMessageViewModel("assistant", "expected answer", false, "", "", "");

            panel.OpenBenchmarkBookmarkEditorForTest(assistant);

            Assert.True(panel.IsBenchmarkBookmarkEditorVisibleForTest);
        });
    }

    private static void RunSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception is not null)
        {
            throw exception;
        }
    }
}
