namespace apiforchat.Models
{
    public class Group
    {

        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Members { get; set; } 
        public string AdminId { get; set; } 
    }
}
