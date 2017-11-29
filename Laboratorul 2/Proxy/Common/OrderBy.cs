namespace Common
{
    public class OrderBy
    {
        public string Property { get; set; }
        public bool Descending { get; set; }

        public OrderBy(string property, bool descending = false)
        {
            Property = property;
            Descending = descending;
        }

        public OrderBy()
        {
        }
    }
}