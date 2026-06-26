using System;
using System.Collections.Generic;
using System.Linq;
using DijkstraSubway.Models;

namespace DijkstraSubway.Services
{
    public class DijkstraAlgorithm
    {
        private const int INF = 1000000;
        private const int MaxVertices = 500;

        private int[] _distance;
        private bool[] _found;
        private int[] _parent;
        private int[][] _weights;

        public List<int> FindPath(List<Station> stations, int startId, int endId, List<StationDistance> distances)
        {
            InitializeGraph(stations, distances);

            // 광명, 서동탄은 시작/도착역이 아니면 경유하지 않도록 처리
            DisableTerminalStations(stations, startId, endId);

            Dijkstra(stations.Count, startId, endId);

            return ReconstructPath(endId);
        }

        public int GetDistance(int targetId)
        {
            return _distance[targetId];
        }

        private void DisableTerminalStations(List<Station> stations, int startId, int endId)
        {
            // 종착역(광명, 서동탄)은 시작/도착역이 아니면 경유하지 않도록 설정
            string[] terminalStations = { "광명", "서동탄" };

            for (int i = 0; i < stations.Count; i++)
            {
                if (i != startId && i != endId && terminalStations.Contains(stations[i].Name))
                {
                    // 이 역으로 들어오는/나가는 모든 연결 차단
                    for (int j = 0; j < stations.Count; j++)
                    {
                        _weights[i][j] = INF;
                        _weights[j][i] = INF;
                    }
                }
            }
        }

        private void InitializeGraph(List<Station> stations, List<StationDistance> distances)
        {
            int n = stations.Count;
            _distance = new int[n];
            _found = new bool[n];
            _parent = new int[n];
            _weights = new int[n][];

            for (int i = 0; i < n; i++)
            {
                _weights[i] = new int[n];
            }

            // 초기화
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                        _weights[i][j] = 0;
                    else
                        _weights[i][j] = INF;
                }
            }

            // 같은 호선의 인접한 역들 (11: 경인선, 21: 성수지선, 22: 신정지선, 51: 하남선 포함)
            int[] allLines = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 21, 22, 51 };
            foreach (int line in allLines)
            {
                // 같은 호선의 역들을 Order 순서로 정렬
                var lineStations = stations
                 .Select((station, index) => (station, index))
              .Where(x => x.station.Line == line)
                   .OrderBy(x => x.station.Order)
                    .ToList();

                if (lineStations.Count == 0)
                    continue;

                // 인접한 역들 연결
                for (int i = 0; i < lineStations.Count - 1; i++)
                {
                    int currIdx = lineStations[i].index;
                    int nextIdx = lineStations[i + 1].index;

                    double dist = GetStationDistance(distances, line, stations[nextIdx].Name);
                    if (dist > 0)
                    {
                        int weight = (int)(dist * 10 + 0.5);
                        _weights[currIdx][nextIdx] = weight;

                        // 6호선: 응암~구산 구간은 한 방향만 (응암→구산만 가능)
                        if (line == 6)
                        {
                            var currStation = stations[currIdx];
                            var nextStation = stations[nextIdx];
                            if ((currStation.Name.Contains("응암") && nextStation.Name.Contains("역촌")) ||
                          (currStation.Name.Contains("역촌") && nextStation.Name.Contains("불광")) ||
                            (currStation.Name.Contains("불광") && nextStation.Name.Contains("독바위")) ||
                             (currStation.Name.Contains("독바위") && nextStation.Name.Contains("연신내")) ||
                     (currStation.Name.Contains("연신내") && nextStation.Name.Contains("구산")))
                            {
                                // 한 방향만 연결 (역방향 제거)
                            }
                            else
                            {
                                _weights[nextIdx][currIdx] = weight;
                            }
                        }
                        else
                        {
                            _weights[nextIdx][currIdx] = weight;
                        }
                    }
                    else
                    {
                        _weights[currIdx][nextIdx] = 20;

                        // 6호선: 응암~구산 구간은 한 방향만
                        if (line == 6)
                        {
                            var currStation = stations[currIdx];
                            var nextStation = stations[nextIdx];
                            if ((currStation.Name.Contains("응암") && nextStation.Name.Contains("역촌")) ||
                         (currStation.Name.Contains("역촌") && nextStation.Name.Contains("불광")) ||
                             (currStation.Name.Contains("불광") && nextStation.Name.Contains("독바위")) ||
                                (currStation.Name.Contains("독바위") && nextStation.Name.Contains("연신내")) ||
                          (currStation.Name.Contains("연신내") && nextStation.Name.Contains("구산")))
                            {
                                // 한 방향만 연결 (역방향 제거)
                            }
                            else
                            {
                                _weights[nextIdx][currIdx] = 20;
                            }
                        }
                        else
                        {
                            _weights[nextIdx][currIdx] = 20;
                        }
                    }
                }

                // 순환선 처리
                if (line == 2 && lineStations.Count > 0)
                {
                    // 2호선: 전체가 순환선
                    int firstIdx = lineStations[0].index;
                    int lastIdx = lineStations[lineStations.Count - 1].index;

                    double dist = GetStationDistance(distances, line, stations[firstIdx].Name);
                    if (dist > 0)
                    {
                        int weight = (int)(dist * 10 + 0.5);
                        _weights[lastIdx][firstIdx] = weight;
                        _weights[firstIdx][lastIdx] = weight;
                    }
                    else
                    {
                        _weights[lastIdx][firstIdx] = 20;
                        _weights[firstIdx][lastIdx] = 20;
                    }
                }

                // 6호선: 구산에서 응암으로의 한 방향 순환
                if (line == 6 && lineStations.Count > 0)
                {
                    var ungam = lineStations.FirstOrDefault(x => x.station.Name.Contains("응암"));
                    var gusan = lineStations.FirstOrDefault(x => x.station.Name.Contains("구산"));

                    if (ungam.station != null && gusan.station != null && ungam.index != 0 && gusan.index != 0)
                    {
                        int ungamIdx = ungam.index;
                        int gusanIdx = gusan.index;

                        double dist = GetStationDistance(distances, line, "응암");
                        if (dist > 0)
                        {
                            int weight = (int)(dist * 10 + 0.5);
                            // 구산 → 응암으로만 한 방향 순환
                            _weights[gusanIdx][ungamIdx] = weight;
                        }
                        else
                        {
                            // 구산 → 응암으로만 한 방향 순환
                            _weights[gusanIdx][ungamIdx] = 20;
                        }
                    }
                }

            }

            // 환승역
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (stations[i].Name == stations[j].Name && stations[i].Line != stations[j].Line)
                    {
                        _weights[i][j] = 0;
                        _weights[j][i] = 0;
                    }
                }
            }

            // 1호선 분기역 연결: 광명↔금천구청, 서동탄↔병점
            ConnectBranchStation(stations, distances, "금천구청", "광명", 1);
            ConnectBranchStation(stations, distances, "병점", "서동탄", 1);
        }

        private void ConnectBranchStation(List<Station> stations, List<StationDistance> distances, string mainStation, string branchStation, int line)
        {
            var main = stations.Select((s, i) => (s, i)).FirstOrDefault(x => x.s.Name == mainStation && x.s.Line == line);
            var branch = stations.Select((s, i) => (s, i)).FirstOrDefault(x => x.s.Name == branchStation && x.s.Line == line);

            if (main.s != null && branch.s != null)
            {
                double dist = GetStationDistance(distances, line, branchStation);
                if (dist > 0)
                {
                    int weight = (int)(dist * 10 + 0.5);
                    _weights[main.i][branch.i] = weight;
                    _weights[branch.i][main.i] = weight;
                }
                else
                {
                    _weights[main.i][branch.i] = 20;
                    _weights[branch.i][main.i] = 20;
                }
            }
        }

        private double GetStationDistance(List<StationDistance> distances, int line, string name)
        {
            var distance = distances.FirstOrDefault(d => d.Line == line && d.Name == name);
            return distance?.Distance ?? 0;
        }

        private void Dijkstra(int n, int start, int end)
        {
            for (int i = 0; i < n; i++)
            {
                _distance[i] = INF;
                _found[i] = false;
                _parent[i] = -1;
            }

            _distance[start] = 0;
            var pq = new PriorityQueue<(int vertex, int dist), int>();
            pq.Enqueue((start, 0), 0);

            while (pq.Count > 0)
            {
                var (u, d) = pq.Dequeue();

                if (_found[u])
                    continue;

                _found[u] = true;

                for (int w = 0; w < n; w++)
                {
                    if (!_found[w] && _weights[u][w] < INF)
                    {
                        if (_distance[u] + _weights[u][w] < _distance[w])
                        {
                            _distance[w] = _distance[u] + _weights[u][w];
                            _parent[w] = u;
                            pq.Enqueue((w, _distance[w]), _distance[w]);
                        }
                    }
                }
            }
        }

        private List<int> ReconstructPath(int end)
        {
            var path = new List<int>();
            int current = end;

            while (current != -1)
            {
                path.Add(current);
                current = _parent[current];
            }

            path.Reverse();
            return path;
        }
    }
}
