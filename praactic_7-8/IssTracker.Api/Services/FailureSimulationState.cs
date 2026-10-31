namespace IssTracker.Api.Services;

// Режимы нужны только для учебной проверки ошибок внешнего сервиса.
public enum FailureMode
{
    None,
    FailTwice,
    AlwaysFail,
    Slow
}

// Хранит текущий режим и счётчик запросов для режима FailTwice.
public class FailureSimulationState
{
    private readonly object _lock = new();
    private FailureMode _mode;
    private int _requestCount;

    // Устанавливает режим и начинает отсчёт запросов заново.
    public void SetMode(FailureMode mode)
    {
        lock (_lock)
        {
            _mode = mode;
            _requestCount = 0;
        }
    }

    // Возвращает режим и номер текущего запроса.
    public (FailureMode Mode, int RequestNumber) NextRequest()
    {
        lock (_lock)
        {
            _requestCount++;
            return (_mode, _requestCount);
        }
    }
}
