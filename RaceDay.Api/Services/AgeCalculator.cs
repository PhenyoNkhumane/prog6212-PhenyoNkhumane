using System.Globalization;

namespace RaceDay.Api.Services;

public static class AgeCalculator
{
    public static int? Calculate(string? idNumberOrPassport, DateOnly asOf)
    {
        if (idNumberOrPassport is null || idNumberOrPassport.Length != 13 ||
            idNumberOrPassport.Any(character => !char.IsAsciiDigit(character)) ||
            !DateOnly.TryParseExact(idNumberOrPassport[..6], "yyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var birthDate))
        {
            return null;
        }

        birthDate = new DateOnly(2000 + birthDate.Year % 100, birthDate.Month, birthDate.Day);
        if (birthDate > asOf || birthDate.AddYears(130) < asOf)
        {
            birthDate = birthDate.AddYears(-100);
        }

        var age = asOf.Year - birthDate.Year;
        if (asOf < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
