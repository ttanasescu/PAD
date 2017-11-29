namespace Common.Entities
{
    public class Book : Entity
    {
        public string Author { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
    }
}