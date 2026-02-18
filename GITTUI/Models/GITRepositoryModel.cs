namespace GITTUI.Models
{
    internal class GITRepositoryModel
    {
        public required string Name { get; set; }
        public required string Owner { get; set; }
        public required string Url { get; set; }


        public string Description { get; set; } = string.Empty;


        public override string ToString() => $" {Name} ({Owner})";
    }
}
