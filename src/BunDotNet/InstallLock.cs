using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace BunDotNet;

internal static class InstallLock
{
    internal static IDisposable Acquire(BunInstallDirectory directory)
    {
        // derive a stable mutex name from the lock path
        var hash = directory.GetMetadataJsonPath().Hash();
        var mutexName = $"{ThisAssembly.AssemblyName}.{hash.Substring(0, 16)}";
        var timeout = TimeSpan.FromMinutes(5);

        var mutex = new Mutex(false, mutexName);
        try
        {
            var acquired = false;
            try
            {
                acquired = mutex.WaitOne(timeout);
            }
            catch (AbandonedMutexException)
            {
                // previous holder crashed — consider lock acquired
                acquired = true;
            }

            if (!acquired)
            {
                mutex.Dispose();
                throw new TimeoutException("Timeout acquiring bun install lock");
            }

            return new InstallLockReleaser(mutex);
        }
        catch
        {
            try
            {
                mutex.Dispose();
            }
            catch
            {
                // best-effort
            }

            throw;
        }
    }
}

[SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly")]
internal class InstallLockReleaser(Mutex mutex) : IDisposable
{
    public void Dispose()
    {
        try
        {
            mutex.ReleaseMutex();
        }
        catch
        {
            // best-effort
        }

        try
        {
            mutex.Dispose();
        }
        catch
        {
            // best-effort
        }
    }
}
