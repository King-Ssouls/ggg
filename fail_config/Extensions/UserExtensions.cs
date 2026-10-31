using System.Security.Claims;

namespace FailConfig.Extensions;

// Помощник читает идентификатор пользователя из проверенного токена
public static class UserExtensions
{
    // Идентификатор из JWT связывает запрос с автором заявки
    public static int GetUserId(this ClaimsPrincipal user)
    {
        return int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
