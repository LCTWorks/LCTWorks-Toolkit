using System;

namespace LCTWorks.WinUI.Models;

public record RatingPromptData(DateTime LastPrompt, int LaunchesSinceLastPrompt);