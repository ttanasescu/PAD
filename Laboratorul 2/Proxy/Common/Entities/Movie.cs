namespace Common.Entities
{
    public class Movie : Entity
    {
        public string Director { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public long Grossing { get; set; }
    }
}