using System.ComponentModel.DataAnnotations;
using ConcertTicketsApi.Models;

namespace ConcertTicketsApi.Contracts;

public sealed class CreateVenueDto
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(240, MinimumLength = 4)]
    public string Address { get; init; } = string.Empty;
}

public sealed record VenueDto(int Id, string Name, string Address);

public sealed class CreateSponsorDto
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
}

public sealed record SponsorDto(int Id, string Name);

public sealed class CreateConcertEventDto
{
    [Required, StringLength(160, MinimumLength = 2)]
    public string Title { get; init; } = string.Empty;

    public DateTime StartsAt { get; init; }

    [Required]
    public ConcertEventStatus Status { get; init; } = ConcertEventStatus.Scheduled;

    [Range(1, int.MaxValue)]
    public int VenueId { get; init; }

    [Range(1, int.MaxValue)]
    public int? SponsorId { get; init; }
}

public sealed record EventListItemDto(int Id, string Title, DateTime StartsAt, ConcertEventStatus Status, string VenueName, string? SponsorName);
public sealed record ConcertEventDto(int Id, string Title, DateTime StartsAt, ConcertEventStatus Status, int VenueId, VenueDto Venue, int? SponsorId, SponsorDto? Sponsor);
public sealed record EventPageDto(int Page, int PageSize, int TotalCount, IReadOnlyList<EventListItemDto> Items);
public sealed record EventCursorPageDto(DateTime? NextStartsAt, int? NextId, IReadOnlyList<EventListItemDto> Items);

public sealed class CreateSectorDto
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 100000)]
    public int Capacity { get; init; }

    [Range(1, int.MaxValue)]
    public int ConcertEventId { get; init; }
}

public sealed class UpdateSectorDto
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 100000)]
    public int Capacity { get; init; }

    [Required]
    public Guid Version { get; init; }
}

public sealed record SectorDto(int Id, string Name, int Capacity, int SoldCount, Guid Version, int ConcertEventId);

public sealed class CreatePriceCategoryDto
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "10000000")]
    public decimal Price { get; init; }

    [Range(1, int.MaxValue)]
    public int SectorId { get; init; }
}

public sealed record PriceCategoryDto(int Id, string Name, decimal Price, int SectorId);

public sealed class CreateBuyerDto
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string LastName { get; init; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 1)]
    public string FirstName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;
}

public sealed record BuyerDto(int Id, string LastName, string FirstName, string Email);
