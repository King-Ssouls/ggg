using FluentValidation;
using ParkingApi.DTOs;

namespace ParkingApi.Validation;

public sealed class CreateParkingBookingValidator : AbstractValidator<CreateParkingBookingDto>
{
    public CreateParkingBookingValidator()
    {
        RuleFor(booking => booking.ParkingSpotId)
            .GreaterThan(0)
            .WithMessage("Выберите существующее машиноместо.");

        AddBookingRules(booking => booking.VehiclePlate, booking => booking.StartAt, booking => booking.EndAt);
    }

    private void AddBookingRules(
        System.Linq.Expressions.Expression<Func<CreateParkingBookingDto, string>> plate,
        System.Linq.Expressions.Expression<Func<CreateParkingBookingDto, DateTime>> start,
        System.Linq.Expressions.Expression<Func<CreateParkingBookingDto, DateTime>> end)
    {
        RuleFor(plate).NotEmpty().WithMessage("Укажите госномер автомобиля.")
            .MaximumLength(15).WithMessage("Госномер не должен быть длиннее 15 символов.")
            .Matches("^[АВЕКМНОРСТУХ][0-9]{3}[АВЕКМНОРСТУХ]{2}[0-9]{2,3}$")
            .WithMessage("Укажите российский госномер, например А123ВС777.");

        RuleFor(start).NotEqual(default(DateTime)).WithMessage("Укажите начало брони.");
        RuleFor(end).NotEqual(default(DateTime)).WithMessage("Укажите окончание брони.");
        RuleFor(end).Must((booking, endAt) => endAt > booking.StartAt)
            .WithMessage("Окончание брони должно быть позже начала.");
        RuleFor(end).Must((booking, endAt) => endAt <= booking.StartAt || endAt - booking.StartAt >= TimeSpan.FromMinutes(30))
            .WithMessage("Минимальная длительность брони — 30 минут.");
    }
}

public sealed class UpdateParkingBookingValidator : AbstractValidator<UpdateParkingBookingDto>
{
    public UpdateParkingBookingValidator()
    {
        RuleFor(booking => booking.VehiclePlate).NotEmpty().WithMessage("Укажите госномер автомобиля.")
            .MaximumLength(15).WithMessage("Госномер не должен быть длиннее 15 символов.")
            .Matches("^[АВЕКМНОРСТУХ][0-9]{3}[АВЕКМНОРСТУХ]{2}[0-9]{2,3}$")
            .WithMessage("Укажите российский госномер, например А123ВС777.");
        RuleFor(booking => booking.StartAt).NotEqual(default(DateTime)).WithMessage("Укажите начало брони.");
        RuleFor(booking => booking.EndAt).NotEqual(default(DateTime)).WithMessage("Укажите окончание брони.");
        RuleFor(booking => booking.EndAt).Must((booking, endAt) => endAt > booking.StartAt)
            .WithMessage("Окончание брони должно быть позже начала.");
        RuleFor(booking => booking.EndAt).Must((booking, endAt) => endAt <= booking.StartAt || endAt - booking.StartAt >= TimeSpan.FromMinutes(30))
            .WithMessage("Минимальная длительность брони — 30 минут.");
    }
}
