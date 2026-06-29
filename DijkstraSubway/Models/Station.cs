namespace DijkstraSubway.Models
{
    public class Station
    {
        // 역이름
        public string Name { get; set; } = string.Empty;
        // 호선
        public int Line { get; set; }
        // 역 순서
        public int Order { get; set; }

        public override string ToString()
        {
            string lineDisplay = Line switch
            {
                11 => "1-1",  // 인천까지
                21 => "2-1",  // 신설동까지
                22 => "2-2",  // 까치산까지
                51 => "5-1",  // 하남까지
                _ => Line.ToString()
            };
            return $"{Name} ({lineDisplay}호선)";
        }
    }
}
