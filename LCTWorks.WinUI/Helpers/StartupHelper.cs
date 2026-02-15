using LCTWorks.WinUI.Models;
using System;

namespace LCTWorks.WinUI.Helpers;

public static class StartupHelper
{
    public static StartupCheckResult Check()
    {
        RuntimePackageHelper.Check();
        if (RuntimePackageHelper.IsFirstRun)
        {
            return StartupCheckResult.FirstRun;
        }
        else if (RuntimePackageHelper.IsAppUpdated)
        {
            return StartupCheckResult.Updated;
        }
        var ratingData = LocalSettingsHelper.RatingPromptData ?? new RatingPromptData(DateTime.MinValue, 0);
        if (DateTime.Now - ratingData.LastPrompt >= TimeSpan.FromDays(3)
            && ratingData.LaunchesSinceLastPrompt >= 1)
        {
            return StartupCheckResult.RatingPrompt;
        }
        else if (ratingData.LaunchesSinceLastPrompt < 10)
        {
            LocalSettingsHelper.RatingPromptData = new RatingPromptData(ratingData.LastPrompt, ratingData.LaunchesSinceLastPrompt + 1);
        }

        return StartupCheckResult.None;
    }

    public static void DelayRatingPrompt()
    {
        LocalSettingsHelper.RatingPromptData = new RatingPromptData(DateTime.Today + TimeSpan.FromDays(180), 10);
    }

    public static void ResetRatingPrompt()
    {
        LocalSettingsHelper.RatingPromptData = new RatingPromptData(DateTime.Today, 0);
    }
}

public enum StartupCheckResult
{
    None,
    FirstRun,
    Updated,
    RatingPrompt,
}