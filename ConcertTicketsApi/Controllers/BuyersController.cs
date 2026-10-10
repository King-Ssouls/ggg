using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Contracts;
using ConcertTicketsApi.Data;
using ConcertTicketsApi.Models;

namespace ConcertTicketsApi.Controllers;

[ApiController]
[Route("api/buyers")]
public sealed class BuyersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BuyerDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await db.Buyers.AsNoTracking().OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new BuyerDto(x.Id, x.LastName, x.FirstName, x.Email)).ToListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BuyerDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var buyer = await db.Buyers.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new BuyerDto(x.Id, x.LastName, x.FirstName, x.Email)).SingleOrDefaultAsync(cancellationToken);
        return buyer is null ? NotFound(ApiProblem.Create(HttpContext, 404, "Покупатель не найден", "Проверьте идентификатор покупателя.")) : Ok(buyer);
    }

    [HttpPost]
    public async Task<ActionResult<BuyerDto>> Create(CreateBuyerDto dto, CancellationToken cancellationToken)
    {
        var buyer = new Buyer
        {
            LastName = dto.LastName.Trim(), FirstName = dto.FirstName.Trim(), Email = dto.Email.Trim().ToLowerInvariant()
        };
        db.Buyers.Add(buyer);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(ApiProblem.Create(HttpContext, 409, "Покупатель уже зарегистрирован", "Адрес электронной почты должен быть уникальным."));
        }

        var response = new BuyerDto(buyer.Id, buyer.LastName, buyer.FirstName, buyer.Email);
        return CreatedAtAction(nameof(GetById), new { id = buyer.Id }, response);
    }
}
