namespace BunDotNet;

internal static class InstallLock
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Acquires an exclusive lock on the installation directory, shared across threads and processes.
    /// </summary>
    /// <remarks>
    /// This uses an exclusively opened lock file instead of a named mutex, because a mutex is owned by the thread
    /// that acquired it and cannot be released after an await resumes on a different thread.
    /// </remarks>
    internal static IDisposable Acquire(BunInstallDirectory directory)
    {
        var lockPath = Path.Combine(directory.Full, ".lock");
        var deadline = DateTimeOffset.UtcNow + Timeout;

        while (true)
        {
            try
            {
                return new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.None
                );
            }
            catch (IOException) when (DateTimeOffset.UtcNow < deadline)
            {
                // the lock is held by another thread or process
                Thread.Sleep(RetryDelay);
            }
            catch (IOException e)
            {
                throw new TimeoutException("Timeout acquiring bun install lock", e);
            }
        }
    }
}
