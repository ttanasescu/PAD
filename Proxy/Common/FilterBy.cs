namespace Common
{
    public class FilterBy
    {
        public string Property { get; set; }
        public object Value { get; set; }

        public FilterBy(string property, object value)
        {
            Property = property;
            Value = value;
        }

        public FilterBy()
        {
        }
    }

    //public class GroupBy
    //{
    //    public string Property { get; set; }
    //}
}