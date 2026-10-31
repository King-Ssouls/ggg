namespace IssTracker.Api.Services;

// Не даёт ручному и фоновому обновлениям идти одновременно.
public class SyncLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // Сразу занимает блокировку, если она свободна.
    public bool TryEnter()
    {
        return _semaphore.Wait(0);
    }

    // Освобождает блокировку после синхронизации.
    public void Exit()
    {
        _semaphore.Release();
    }
}
