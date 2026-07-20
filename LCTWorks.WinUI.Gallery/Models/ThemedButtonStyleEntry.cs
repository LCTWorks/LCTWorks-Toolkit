using Microsoft.UI.Xaml;

namespace LCTWorks.Workshop.Models
{
    public class ThemedButtonStyleEntry
    {
        public string? ResourceName { get; set; }

        public Style? Style { get; set; }

        public bool ValidStyle => Style != null;
    }
}