using System.Net;
using System.Net.Http;
using DesktopImagePin.Services;

namespace DesktopImagePin.Tests;

public sealed class ImageImportServiceDownloadTests : IDisposable
{
    private static readonly TimeSpan TestWatchdog = TimeSpan.FromSeconds(5);
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"DesktopImagePin.DownloadTests-{Guid.NewGuid():N}");

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task ImportFromUrlAsync_HeadersThenStalledBody_TimesOutAndDeletesPartialFile(int prefixLength)
    {
        var clock = new DeadlineTimeProvider();
        using var source = new StallingStream(prefixLength);
        using var client = CreateClient(_ => Task.FromResult(CreateResponse(new StreamContent(source))));
        var service = new ImageImportService(_directory, client, clock);

        var import = service.ImportFromUrlAsync("https://example.com/image.png");
        await source.Stalled.Task.WaitAsync(TestWatchdog);

        Assert.False(import.IsCompleted);
        Assert.Single(Directory.GetFiles(_directory));
        Assert.Equal(prefixLength, source.BytesDelivered);
        Assert.True(source.ReadToken.CanBeCanceled);
        clock.Expire();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => import.WaitAsync(TestWatchdog));
        Assert.True(source.ReadToken.IsCancellationRequested);
        Assert.True(source.IsDisposed);
        Assert.Empty(Directory.GetFiles(_directory));
        Assert.True(clock.TimerDisposed);
    }

    [Fact]
    public async Task ImportFromUrlAsync_StalledHeaders_UsesSameDeadline()
    {
        var clock = new DeadlineTimeProvider();
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = CreateClient(async token =>
        {
            requested.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            throw new InvalidOperationException("The request must be canceled.");
        });
        var service = new ImageImportService(_directory, client, clock);

        var import = service.ImportFromUrlAsync("https://example.com/image.png");
        await requested.Task.WaitAsync(TestWatchdog);
        clock.Expire();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => import.WaitAsync(TestWatchdog));
        Assert.False(Directory.Exists(_directory));
        Assert.True(clock.TimerDisposed);
    }

    [Fact]
    public async Task ImportFromUrlAsync_StalledStreamAcquisition_TimesOutWithoutLeavingFile()
    {
        var clock = new DeadlineTimeProvider();
        using var content = new StallingContent();
        using var client = CreateClient(_ => Task.FromResult(CreateResponse(content)));
        var service = new ImageImportService(_directory, client, clock);

        var import = service.ImportFromUrlAsync("https://example.com/image.png");
        await content.Stalled.Task.WaitAsync(TestWatchdog);
        clock.Expire();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => import.WaitAsync(TestWatchdog));
        Assert.Empty(Directory.GetFiles(_directory));
        Assert.True(clock.TimerDisposed);
    }

    [Fact]
    public async Task ImportFromUrlAsync_ValidImage_SavesCompleteImageAndReleasesFile()
    {
        Directory.CreateDirectory(_directory);
        var referencePath = Path.Combine(_directory, "reference.png");
        WpfTestHelper.Run(() => WpfTestHelper.SaveImage(referencePath));
        var bytes = File.ReadAllBytes(referencePath);
        File.Delete(referencePath);
        var clock = new DeadlineTimeProvider();
        using var client = CreateClient(_ => Task.FromResult(CreateResponse(new ByteArrayContent(bytes))));
        var service = new ImageImportService(_directory, client, clock);

        var filePath = await service.ImportFromUrlAsync("https://example.com/image.png").WaitAsync(TestWatchdog);

        Assert.Equal(bytes, File.ReadAllBytes(filePath));
        Assert.True(clock.TimerDisposed);
        File.Delete(filePath);
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ImportFromUrlAsync_OversizedImage_RejectsAndLeavesNoFile(bool advertisedLength)
    {
        var clock = new DeadlineTimeProvider();
        using HttpContent content = advertisedLength
            ? new ByteArrayContent([])
            : new StreamContent(new NonSeekableMemoryStream(new byte[checked((int)ImageImportService.MaximumDownloadBytes + 1)]));
        content.Headers.ContentLength = advertisedLength ? ImageImportService.MaximumDownloadBytes + 1 : null;
        using var client = CreateClient(_ => Task.FromResult(CreateResponse(content)));
        var service = new ImageImportService(_directory, client, clock);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ImportFromUrlAsync("https://example.com/image.png").WaitAsync(TestWatchdog));

        Assert.Equal("The image exceeds the 25 MB download limit.", exception.Message);
        Assert.True(!Directory.Exists(_directory) || Directory.GetFiles(_directory).Length == 0);
        Assert.True(clock.TimerDisposed);
    }

    [Fact]
    public async Task ImportFromUrlAsync_InvalidImage_DeletesDownloadedFile()
    {
        var clock = new DeadlineTimeProvider();
        using var client = CreateClient(_ => Task.FromResult(CreateResponse(new ByteArrayContent([1, 2, 3]))));
        var service = new ImageImportService(_directory, client, clock);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ImportFromUrlAsync("https://example.com/image.png").WaitAsync(TestWatchdog));

        Assert.Equal("The downloaded data is not a valid image.", exception.Message);
        Assert.Empty(Directory.GetFiles(_directory));
        Assert.True(clock.TimerDisposed);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private static HttpResponseMessage CreateResponse(HttpContent content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static HttpClient CreateClient(Func<CancellationToken, Task<HttpResponseMessage>> send)
    {
        return new HttpClient(new StubHandler(send)) { Timeout = Timeout.InfiniteTimeSpan };
    }

    private sealed class StubHandler(Func<CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return send(cancellationToken);
        }
    }

    private sealed class DeadlineTimeProvider : TimeProvider
    {
        private DeadlineTimer? _timer;

        public bool TimerDisposed => _timer?.IsDisposed == true;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Assert.Null(_timer);
            Assert.Equal(TimeSpan.FromSeconds(30), dueTime);
            Assert.Equal(Timeout.InfiniteTimeSpan, period);
            _timer = new DeadlineTimer(callback, state);
            return _timer;
        }

        public void Expire()
        {
            Assert.NotNull(_timer);
            Assert.False(_timer.IsDisposed);
            _timer.Fire();
        }

        private sealed class DeadlineTimer(TimerCallback callback, object? state) : ITimer
        {
            public bool IsDisposed { get; private set; }

            public void Fire() => callback(state);

            public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();

            public void Dispose() => IsDisposed = true;

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed class NonSeekableMemoryStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }

    private sealed class StallingContent : HttpContent
    {
        public TaskCompletionSource Stalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
        {
            Stalled.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Stream acquisition must be canceled.");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => throw new NotSupportedException();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed class StallingStream(int prefixLength) : Stream
    {
        public TaskCompletionSource Stalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken ReadToken { get; private set; }
        public int BytesDelivered { get; private set; }
        public bool IsDisposed { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadToken = cancellationToken;
            if (BytesDelivered < prefixLength)
            {
                buffer.Span[..prefixLength].Fill(1);
                BytesDelivered = prefixLength;
                return prefixLength;
            }

            Stalled.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("The body read must be canceled.");
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
