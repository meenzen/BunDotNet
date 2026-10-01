namespace BunDotNet.Tests;

public class InstallLockTests
{
    private static BunInstallDirectory CreateDirectory() =>
        new(Path.Combine(Path.GetTempPath(), "BunDotNet.Tests", Guid.NewGuid().ToString("N")));

    [Test]
    public async Task Acquire_AfterReleaseOnDifferentThread_Succeeds()
    {
        var directory = CreateDirectory();
        var acquiringThread = Environment.CurrentManagedThreadId;

        var @lock = InstallLock.Acquire(directory);
        await Task.Run(() =>
        {
            // release from a thread other than the one that acquired the lock, like after an await
            if (Environment.CurrentManagedThreadId == acquiringThread)
            {
                throw new InvalidOperationException("Expected a different thread");
            }

            @lock.Dispose();
        });

        var reacquired = await Task.Run(() => InstallLock.Acquire(directory)).WaitAsync(TimeSpan.FromSeconds(10));
        reacquired.Dispose();
    }

    [Test]
    public async Task Acquire_WhileHeld_WaitsForRelease()
    {
        var directory = CreateDirectory();
        var @lock = InstallLock.Acquire(directory);

        var second = Task.Run(() => InstallLock.Acquire(directory));
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        await Assert.That(second.IsCompleted).IsFalse();

        @lock.Dispose();
        var acquired = await second.WaitAsync(TimeSpan.FromSeconds(10));
        acquired.Dispose();
    }
}
