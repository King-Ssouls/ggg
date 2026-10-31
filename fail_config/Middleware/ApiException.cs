namespace FailConfig.Middleware;

// Передаёт понятную ошибку сервиса в общий обработчик
public sealed class ApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
