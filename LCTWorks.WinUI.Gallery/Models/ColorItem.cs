using Windows.UI;

namespace LCTWorks.Workshop.Models
{
    public record class ColorItem(string Name, Color Color, int Index)
    {
        public override string ToString() => $"{Name} {Index}: {Color}";
    }
}