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

Dijkstra(stations.Count, startId, endId);

     return ReconstructPath(endId);
        }

    public int GetDistance(int targetId)
      {
            return _distance[targetId];
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

   // 같은 호선의 인접한 역들
      for (int line = 1; line <= 9; line++)
           {
 int prevIdx = -1;
            for (int i = 0; i < n; i++)
          {
          if (stations[i].Line == line)
           {
   if (prevIdx != -1)
  {
               double dist = GetStationDistance(distances, line, stations[i].Name);
         if (dist > 0)
     {
          int weight = (int)(dist * 10 + 0.5);
          _weights[prevIdx][i] = weight;
        _weights[i][prevIdx] = weight;
            }
          else
              {
     _weights[prevIdx][i] = 20;
_weights[i][prevIdx] = 20;
   }
               }
             prevIdx = i;
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
