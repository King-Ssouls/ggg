using System.ComponentModel.DataAnnotations;

namespace FailConfig.Contracts;

// Владелец заявки берётся из JWT и не передаётся в теле запроса
public sealed class CreateTicketDto
{
    [Required(ErrorMessage = "Укажите тему заявки")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Тема должна содержать от 3 до 150 символов")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Опишите проблему")]
    [StringLength(5000, MinimumLength = 3, ErrorMessage = "Описание должно содержать от 3 до 5000 символов")]
    public string Text { get; init; } = string.Empty;
}

// Автор выбирает агента из списка сотрудников поддержки
public sealed class AssignAgentDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Выберите агента поддержки")]
    public int AgentId { get; init; }
}

// Ответ показывает сведения о заявке без внутренних объектов базы
public sealed record TicketResponse(
    int Id, string Title, string Text, int AuthorId, int? AgentId, DateTime CreatedAt);

// Список агентов содержит только идентификатор и имя
public sealed record AgentResponse(int Id, string UserName);
