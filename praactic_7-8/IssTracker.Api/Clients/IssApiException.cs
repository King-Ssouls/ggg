namespace IssTracker.Api.Clients;

// Ошибка означает, что внешний сервис вернул пустые или неверные данные.
public class IssApiException : Exception
{
    // Создаёт понятную ошибку проверки ответа внешнего API.
    public IssApiException(string message) : base(message)
    {
    }

    // Сохраняет исходную ошибку чтения ответа.
    public IssApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
