namespace DijkstraSubway.Models
{
    // 동적 정보를 위한 클래스
    // 역 간 거리 정보
    public class StationDistance
    {
        public int Line { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Distance { get; set; }
    }
}
