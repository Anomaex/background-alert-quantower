namespace BackgroundAlert
{
    public class Alert
    {
        public string Id { get; set; }
        public string Symbol { get; set; }
        public bool IsUnder { get; set; }
        public double Price { get; set; }
        public string AfterExecution { get; set; }
    }
}
