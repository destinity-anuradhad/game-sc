using LanArena.Application.Abstractions;

namespace LanArena.Application.Features.PublicSite.RegisterCod4Team;

public sealed class RegisterCod4TeamHandler(IPublicTournamentStore store)
{
    public Task<string> Handle(RegisterCod4TeamRequest request, CancellationToken cancellationToken = default)
    {
        var roster = new[] { request.MainPlayerOne, request.MainPlayerTwo, request.MainPlayerThree, request.MainPlayerFour, request.MainPlayerFive };
        var realNames = new[] { request.MainPlayerOneRealName, request.MainPlayerTwoRealName, request.MainPlayerThreeRealName, request.MainPlayerFourRealName, request.MainPlayerFiveRealName };
        var epfNumbers = new[] { request.EpfNoOne, request.EpfNoTwo, request.EpfNoThree, request.EpfNoFour, request.EpfNoFive }
            .Select(epf => epf.Trim().ToUpperInvariant()).ToArray();
        var shortName = request.ShortName.Trim().ToUpperInvariant();
        if (roster.Any(string.IsNullOrWhiteSpace) || realNames.Any(string.IsNullOrWhiteSpace) || epfNumbers.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Enter the real name, in-game name and EPF number for all five main players.");
        if (shortName.Length is < 2 or > 4 || shortName.Any(character => !char.IsLetterOrDigit(character)))
            throw new ArgumentException("Use 2–4 letters or numbers for the team short name.");
        if (epfNumbers.Any(epf => epf.Any(character => !char.IsLetterOrDigit(character) && character != '-')))
            throw new ArgumentException("Use letters, numbers or hyphens for EPF numbers.");
        if (epfNumbers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != epfNumbers.Length)
            throw new ArgumentException("Each main player must have a different EPF number.");

        var substituteName = request.Substitute?.Trim();
        var substituteRealName = request.SubstituteRealName?.Trim();
        var substituteEpfNo = request.SubstituteEpfNo?.Trim().ToUpperInvariant();
        var substituteProvided = !string.IsNullOrWhiteSpace(substituteName) || !string.IsNullOrWhiteSpace(substituteEpfNo);
        if (substituteProvided && (string.IsNullOrWhiteSpace(substituteName) || string.IsNullOrWhiteSpace(substituteRealName) || string.IsNullOrWhiteSpace(substituteEpfNo)))
            throw new ArgumentException("Enter the substitute's real name, in-game name and EPF number, or leave the substitute fields blank.");
        if (!string.IsNullOrWhiteSpace(substituteEpfNo) && substituteEpfNo.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
            throw new ArgumentException("Use letters, numbers or hyphens for the substitute EPF number.");
        if (!string.IsNullOrWhiteSpace(substituteEpfNo) && epfNumbers.Contains(substituteEpfNo, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Each player must have a different EPF number.");

        var guids = epfNumbers.Select((epf, index) => $"{shortName}_{epf}_{index + 1}").ToArray();

        var names = roster.Append(substituteName).Where(name => !string.IsNullOrWhiteSpace(name));
        if (names.Select(name => name!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count())
            throw new ArgumentException("Each roster nickname must be unique within the team.");
        var substituteGuid = substituteProvided ? $"{shortName}_{substituteEpfNo}_SUB" : null;

        return store.RegisterCod4Team(request with
        {
            TeamName = request.TeamName.Trim(),
            ShortName = shortName,
            CaptainName = request.CaptainName.Trim(),
            Phone = request.Phone.Trim(),
            MainPlayerOne = request.MainPlayerOne.Trim(), MainPlayerTwo = request.MainPlayerTwo.Trim(),
            MainPlayerThree = request.MainPlayerThree.Trim(), MainPlayerFour = request.MainPlayerFour.Trim(),
            MainPlayerFive = request.MainPlayerFive.Trim(),
            MainPlayerOneRealName = request.MainPlayerOneRealName.Trim(), MainPlayerTwoRealName = request.MainPlayerTwoRealName.Trim(),
            MainPlayerThreeRealName = request.MainPlayerThreeRealName.Trim(), MainPlayerFourRealName = request.MainPlayerFourRealName.Trim(),
            MainPlayerFiveRealName = request.MainPlayerFiveRealName.Trim(),
            EpfNoOne = epfNumbers[0], EpfNoTwo = epfNumbers[1], EpfNoThree = epfNumbers[2],
            EpfNoFour = epfNumbers[3], EpfNoFive = epfNumbers[4],
            GuidOne = guids[0], GuidTwo = guids[1], GuidThree = guids[2], GuidFour = guids[3], GuidFive = guids[4],
            Substitute = substituteName, SubstituteRealName = substituteRealName, SubstituteEpfNo = substituteEpfNo, SubstituteGuid = substituteGuid
        }, cancellationToken);
    }
}
