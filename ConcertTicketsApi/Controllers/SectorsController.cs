using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertTicketsApi.Contracts;
using ConcertTicketsApi.Data;
using ConcertTicketsApi.Models;

namespace ConcertTicketsApi.Controllers;

[ApiController]
[Route("api/sectors")]
public sealed class SectorsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SectorDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await db.Sectors.AsNoTracking().OrderBy(x => x.ConcertEventId).ThenBy(x => x.Name)
            .Select(x => new SectorDto(x.Id, x.Name, x.Capacity, x.SoldCount, x.Version, x.ConcertEventId))
            .ToListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SectorDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var sector = await db.Sectors.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new SectorDto(x.Id, x.Name, x.Capacity, x.SoldCount, x.Version, x.ConcertEventId))
            .SingleOrDefaultAsync(cancellationToken);
        return sector is null ? NotFound(ApiProblem.Create(HttpContext, 404, "Сектор не найден", "Проверьте идентификатор сектора.")) : Ok(sector);
    }

    [HttpPost]
    public async Task<ActionResult<SectorDto>> Create(CreateSectorDto dto, CancellationToken cancellationToken)
    {
        var concert = await db.ConcertEvents.SingleOrDefaultAsync(x => x.Id == dto.ConcertEventId, cancellationToken);
        if (concert is null)
        {
            return BadRequest(ApiProblem.Create(HttpContext, 400, "Событие не найдено", "Укажите существующий ConcertEventId."));
        }

        var sector = new Sector
        {
            Name = dto.Name.Trim(), Capacity = dto.Capacity, ConcertEvent = concert, Version = Guid.NewGuid()
        };
        db.Sectors.Add(sector);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(ApiProblem.Create(HttpContext, 409, "Сектор уже существует", "В одном событии названия секторов должны быть разными."));
        }

        return CreatedAtAction(nameof(GetById), new { id = sector.Id }, ToDto(sector));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SectorDto>> Update(int id, UpdateSectorDto dto, CancellationToken cancellationToken)
    {
        var sector = await db.Sectors.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (sector is null)
        {
            return NotFound(ApiProblem.Create(HttpContext, 404, "Сектор не найден", "Проверьте идентификатор сектора."));
        }

        if (sector.Version != dto.Version)
        {
            return Conflict(ApiProblem.Create(HttpContext, 409, "Версия устарела", "Сектор изменился после чтения. Прочитайте его заново и повторите изменение."));
        }

        if (dto.Capacity < sector.SoldCount)
        {
            return Conflict(ApiProblem.Create(HttpContext, 409, "Вместимость меньше числа проданных билетов", "Сначала верните лишние билеты."));
        }

        sector.Name = dto.Name.Trim();
        sector.Capacity = dto.Capacity;
        sector.Version = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ApiProblem.Create(HttpContext, 409, "Конфликт версии", "Сектор уже изменен другим запросом."));
        }
        catch (DbUpdateException)
        {
            return Conflict(ApiProblem.Create(HttpContext, 409, "Конфликт данных", "Проверьте уникальность названия сектора."));
        }

        return Ok(ToDto(sector));
    }

    [HttpGet("{id:int}/tickets/explicit")]
    public async Task<ActionResult<object>> LoadTicketsExplicitly(int id, CancellationToken cancellationToken)
    {
        var sector = await db.Sectors.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (sector is null)
        {
            return NotFound(ApiProblem.Create(HttpContext, 404, "Сектор не найден", "Проверьте идентификатор сектора."));
        }

        await db.Entry(sector).Collection(x => x.Tickets).LoadAsync(cancellationToken);
        return Ok(new
        {
            sector.Id,
            sector.Name,
            Tickets = sector.Tickets.Select(x => new { x.Id, x.Number, x.Status, x.SoldAt, x.SoldPrice }).ToList()
        });
    }

    private static SectorDto ToDto(Sector sector) =>
        new(sector.Id, sector.Name, sector.Capacity, sector.SoldCount, sector.Version, sector.ConcertEventId);
}

[ApiController]
[Route("api/price-categories")]
public sealed class PriceCategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PriceCategoryDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await db.PriceCategories.AsNoTracking().OrderBy(x => x.SectorId).ThenBy(x => x.Price)
            .Select(x => new PriceCategoryDto(x.Id, x.Name, x.Price, x.SectorId)).ToListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PriceCategoryDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var category = await db.PriceCategories.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new PriceCategoryDto(x.Id, x.Name, x.Price, x.SectorId)).SingleOrDefaultAsync(cancellationToken);
        return category is null ? NotFound(ApiProblem.Create(HttpContext, 404, "Ценовая категория не найдена", "Проверьте идентификатор категории.")) : Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<PriceCategoryDto>> Create(CreatePriceCategoryDto dto, CancellationToken cancellationToken)
    {
        var sector = await db.Sectors.SingleOrDefaultAsync(x => x.Id == dto.SectorId, cancellationToken);
        if (sector is null)
        {
            return BadRequest(ApiProblem.Create(HttpContext, 400, "Сектор не найден", "Укажите существующий SectorId."));
        }

        var category = new PriceCategory { Name = dto.Name.Trim(), Price = dto.Price, Sector = sector };
        db.PriceCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        var response = new PriceCategoryDto(category.Id, category.Name, category.Price, category.SectorId);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, response);
    }
}
