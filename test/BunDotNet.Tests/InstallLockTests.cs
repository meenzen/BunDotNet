namespace BunDotNet.Tests;

public class InstallLockTests
{
    private static BunInstallDirectory CreateDirectory() =>
        new(Path.Combine(Path.GetTempPath(), "BunDotNet.Tests", Guid.NewGuid().ToString("N")));

    [Test]
    // This test was flaky, repeat it to make sure it is stable
    [Repeat(100)]
    public async Task Acquire_AfterReleaseOnDifferentThread_Succeeds()
    {
        var directory = CreateDirectory();
        var acquiringThread = Environment.CurrentManagedThreadId;

        var @lock = InstallLock.Acquire(directory);

        // release from a thread other than the one that acquired the lock, like after an await. A dedicated thread
        // is used because a thread pool work item may run on the acquiring thread once it returns to the pool.
        var releasingThread = new Thread(() => @lock.Dispose());
        releasingThread.Start();
        releasingThread.Join();
        await Assert.That(releasingThread.ManagedThreadId).IsNotEqualTo(acquiringThread);

        var reacquired = await Task.Run(() => InstallLock.Acquire(directory)).WaitAsync(TimeSpan.FromSeconds(10));
        reacquired.Dispose();
        Directory.Delete(directory.Base, recursive: true);
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
        Directory.Delete(directory.Base, recursive: true);
    }
}
