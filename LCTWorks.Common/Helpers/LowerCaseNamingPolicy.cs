using System.Text.Json;

namespace LCTWorks.Common.Helpers;

public class LowerCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
        => name.ToLowerInvariant();
}