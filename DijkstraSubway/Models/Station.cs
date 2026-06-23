namespace DijkstraSubway.Models
{
    public class Station
 {
   public int Id { get; set; }
   public string Name { get; set; } = string.Empty;
        public int Line { get; set; }
        public int Order { get; set; }

  public override string ToString() => $"{Name} ({Line}È£¼±)";
    }
}
