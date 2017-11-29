namespace Common.Entities
{
    public class Song : Entity
    {
        public string Singer { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public string Duration { get; set; }
    }
}