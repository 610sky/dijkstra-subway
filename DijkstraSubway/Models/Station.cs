namespace DijkstraSubway.Models
{
    public class Station
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Line { get; set; }
        public int Order { get; set; }

        public override string ToString()
        {
            string lineDisplay = Line switch
            {
                11 => "1-1",  // 경인선
                21 => "2-1",  // 성수지선
                22 => "2-2",  // 신정지선
                51 => "5-1",  // 하남선
                _ => Line.ToString()
            };
            return $"{Name} ({lineDisplay}호선)";
        }
    }
}
