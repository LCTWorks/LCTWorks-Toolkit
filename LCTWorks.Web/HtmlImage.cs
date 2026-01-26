namespace LCTWorks.Web
{
    public record class HtmlImage
    {
        public string? Src { get; init; }
        public string? Alt { get; init; }
        public string? Title { get; init; }
        public int? Width { get; init; }
        public int? Height { get; init; }
        public string? SrcSet { get; init; }
        public string? Sizes { get; init; }
        public string? Loading { get; init; }
    }
}