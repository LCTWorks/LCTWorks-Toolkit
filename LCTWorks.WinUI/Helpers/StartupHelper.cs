using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Models;
using Sentry.Protocol;

namespace LCTWorks.WinUI.Helpers;

public static class StartupHelper
{
    public static async Task<StartupCheckResult> CheckAsync()
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
            //var dialogResult = await dialog.ShowRatePromptDialogAsync();
            //if (dialogResult == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary
            //    && await HandleStoreRatingDialog())
            //{
            //    //All good, the user rated the app
            //    var notification = App.GetService<IUINotificationService>();
            //    notification.ShowMessage("UIMessage_ThanksForRating".GetTextLocalized());
            //    local.SaveSetting(RatingPromptSettingsKey, new RatingData(DateTime.Today + TimeSpan.FromDays(180), 10));
            //}
            //else
            //{
            //    //The user didn't rate the app, was a connection error or rejected the dialog prompt.
            //    //Delay the next prompt as much as possible:
            //    local.SaveSetting(RatingPromptSettingsKey, new RatingData(DateTime.Today, 0));
            //}
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