using System;

namespace LCTWorks.WinUI.Experimental.Models;

public record RatingPromptData(DateTime LastPrompt, int LaunchesSinceLastPrompt);