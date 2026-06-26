using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DijkstraSubway.Models;
using DijkstraSubway.Services;

namespace DijkstraSubway
{
    public class StationItem
    {
        public string StationName { get; set; } = string.Empty;
        public string LineText { get; set; } = string.Empty;
        public Color LineColor { get; set; }
        public bool IsTransfer { get; set; }
        public bool ShowArrow { get; set; }
    }

    public partial class MainWindow : Window
    {
        private SubwayDataLoader _dataLoader;
        private List<Station> _allStations;
        private List<StationDistance> _stationDistances;

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
            SetupEventHandlers();
        }

        private void LoadData()
        {
            _dataLoader = new SubwayDataLoader();

            var result = SubwayDataLoader.LoadSubwayData("subway_data.csv");
            if (result.stations == null || result.distances == null)
            {
                MessageBox.Show("subway_data.csv 파일을 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            _allStations = result.stations;
            _stationDistances = result.distances;

            PopulateLineListBoxes();
        }

        private void SetupEventHandlers()
        {
        }

        private void PopulateLineListBoxes()
        {
            var lineListBoxes = new Dictionary<int, ListBox>
         {
          { 1, StartLine1ListBox }, { 2, StartLine2ListBox }, { 3, StartLine3ListBox },
          { 4, StartLine4ListBox }, { 5, StartLine5ListBox }, { 6, StartLine6ListBox },
            { 7, StartLine7ListBox }, { 8, StartLine8ListBox }, { 9, StartLine9ListBox }
     };

            var endLineListBoxes = new Dictionary<int, ListBox>
    {
         { 1, EndLine1ListBox }, { 2, EndLine2ListBox }, { 3, EndLine3ListBox },
         { 4, EndLine4ListBox }, { 5, EndLine5ListBox }, { 6, EndLine6ListBox },
                { 7, EndLine7ListBox }, { 8, EndLine8ListBox }, { 9, EndLine9ListBox }
            };

            for (int line = 1; line <= 9; line++)
            {
                List<string> stations;

                if (line == 1)
                {
                    // 1호선: 본선 + 경인선(11) 모두 포함
                    stations = _allStations
                .Where(s => s.Line == 1 || s.Line == 11)
                    .Select(s => s.ToString())
                       .ToList();
                }
                else if (line == 2)
                {
                    // 2호선: 본선 + 성수지선(21) + 신정지선(22) 모두 포함
                    stations = _allStations
              .Where(s => s.Line == 2 || s.Line == 21 || s.Line == 22)
             .Select(s => s.ToString())
            .ToList();
                }
                else if (line == 5)
                {
                    // 5호선: 본선 + 하남선(51) 모두 포함
                    stations = _allStations
              .Where(s => s.Line == 5 || s.Line == 51)
                     .Select(s => s.ToString())
               .ToList();
                }
                else
                {
                    stations = _allStations.Where(s => s.Line == line).Select(s => s.ToString()).ToList();
                }

                if (lineListBoxes.ContainsKey(line))
                    lineListBoxes[line].ItemsSource = stations;
                if (endLineListBoxes.ContainsKey(line))
                    endLineListBoxes[line].ItemsSource = stations;
            }
        }

        private void StartLineListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                StartStationTextBox.Text = listBox.SelectedItem.ToString();
            }
        }

        private void EndLineListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                EndStationTextBox.Text = listBox.SelectedItem.ToString();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string startText = StartStationTextBox.Text?.Trim();
            string endText = EndStationTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(startText) || string.IsNullOrEmpty(endText))
            {
                ShowErrorResult("출발점과 도착점을 모두 입력하세요.");
                return;
            }

            var startStation = ExtractStationInfo(startText);
            var endStation = ExtractStationInfo(endText);

            int startId = FindStationId(startStation.name, startStation.line);
            int endId = FindStationId(endStation.name, endStation.line);

            if (startId == -1)
            {
                ShowErrorResult($"오류: '{startStation.name}' 역을 찾을 수 없습니다.");
                return;
            }

            if (endId == -1)
            {
                ShowErrorResult($"오류: '{endStation.name}' 역을 찾을 수 없습니다.");
                return;
            }

            if (startId == endId)
            {
                ShowErrorResult("출발점과 도착점이 동일합니다.");
                return;
            }

            try
            {
                var dijkstra = new DijkstraAlgorithm();
                var path = dijkstra.FindPath(_allStations, startId, endId, _stationDistances);

                if (path == null || path.Count == 0)
                {
                    ShowErrorResult("경로를 찾을 수 없습니다.");
                    return;
                }

                int totalDistance = dijkstra.GetDistance(endId);
                ShowPathResult(_allStations[startId], _allStations[endId], totalDistance, path);
            }
            catch (Exception ex)
            {
                ShowErrorResult($"오류 발생: {ex.Message}");
            }
        }

        private (string name, int line) ExtractStationInfo(string text)
        {
            int parenIndex = text.IndexOf('(');
            if (parenIndex > 0)
            {
                string name = text.Substring(0, parenIndex).Trim();
                string lineText = text.Substring(parenIndex + 1).Replace("호선)", "").Replace(")", "").Trim();

                // 1-1, 2-1, 2-2, 5-1 형식 처리 (경인선, 성수지선, 신정지선, 하남선)
                if (lineText == "1-1")
                {
                    return (name, 11);
                }
                else if (lineText == "2-1")
                {
                    return (name, 21);
                }
                else if (lineText == "2-2")
                {
                    return (name, 22);
                }
                else if (lineText == "5-1")
                {
                    return (name, 51);
                }
                else if (int.TryParse(lineText, out int line))
                {
                    return (name, line);
                }
            }
            return (text.Trim(), -1);
        }

        private int FindStationId(string name, int line)
        {
            for (int i = 0; i < _allStations.Count; i++)
            {
                if (_allStations[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (line == -1 || _allStations[i].Line == line)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        //        private string FormatResult(Station start, Station end, int distance, List<int> path)
        //        {
        //            var result = "==========================================\n";
        //         result += "     최단경로 검색 결과\n";
        //            result += "==========================================\n";
        //            result += $"출발점: {start.Name} ({start.Line}호선)\n";
        //  result += $"도착점: {end.Name} ({end.Line}호선)\n";
        //   result += $"최단거리: {distance / 10.0:F1} km\n\n";

        //    var filteredPath = new List<int>();
        //    for (int i = 0; i < path.Count; i++)
        //{
        //       if (filteredPath.Count == 0 ||
        //    _allStations[path[i]].Name != _allStations[filteredPath[filteredPath.Count - 1]].Name ||
        //   _allStations[path[i]].Line != _allStations[filteredPath[filteredPath.Count - 1]].Line)
        //             {
        //        filteredPath.Add(path[i]);
        //       }
        //     }

        //                     result += "경로: ";
        //              for (int i = 0; i < filteredPath.Count; i++)
        //                {
        //              var station = _allStations[filteredPath[i]];
        //               result += $"{station.Name}({station.Line}호선)";
        //              if (i < filteredPath.Count - 1)
        //             {
        //                    result += " → ";
        //                  }
        //                  }
        //                 result += "\n==========================================\n";

        //                        return result;
        //                    }

        private void ShowErrorResult(string message)
        {
            ResultHeader.Text = "[알림]";
            ResultSummary.Text = message;
            ResultItems.ItemsSource = null;
            AnimateResultPanel();
        }

        private void ShowPathResult(Station start, Station end, int distance, List<int> path)
        {
            var filteredPath = new List<int>();
            for (int i = 0; i < path.Count; i++)
            {
                if (filteredPath.Count == 0 ||
           _allStations[path[i]].Name != _allStations[filteredPath[filteredPath.Count - 1]].Name ||
                   _allStations[path[i]].Line != _allStations[filteredPath[filteredPath.Count - 1]].Line)
                {
                    filteredPath.Add(path[i]);
                }
            }

            var items = new List<StationItem>();
            for (int i = 0; i < filteredPath.Count; i++)
            {
                var station = _allStations[filteredPath[i]];
                bool isTransfer = false;

                if (i > 0)
                {
                    var prevStation = _allStations[filteredPath[i - 1]];
                    if (station.Name == prevStation.Name && station.Line != prevStation.Line)
                    {
                        isTransfer = true;
                    }
                }

                items.Add(new StationItem
                {
                    StationName = station.Name,
                    LineText = GetLineDisplayText(station.Line),
                    LineColor = GetLineColor(station.Line),
                    IsTransfer = isTransfer,
                    ShowArrow = i < filteredPath.Count - 1
                });
            }

            int transferCount = CountTransfers(filteredPath);

            ResultHeader.Text = "최단경로 검색 결과";
            ResultSummary.Text = $"총 거리: {distance / 10.0:F1} km  |  정류장: {filteredPath.Count}개  |  환승: {transferCount}회";
            ResultItems.ItemsSource = items;
            AnimateResultPanel();
        }

        private int CountTransfers(List<int> path)
        {
            int transfers = 0;
            for (int i = 1; i < path.Count; i++)
            {
                if (_allStations[path[i]].Line != _allStations[path[i - 1]].Line)
                {
                    transfers++;
                }
            }
            return transfers;
        }

        private string GetLineDisplayText(int line)
        {
            return line switch
            {
                11 => "1-1호선",  // 경인선
                21 => "2-1호선",  // 성수지선
                22 => "2-2호선",  // 신정지선
                51 => "5-1호선",  // 하남선
                _ => $"{line}호선"
            };
        }

        private Color GetLineColor(int line)
        {
            return line switch
            {
                1 => Color.FromRgb(0, 82, 164),      // #0052A4
                11 => Color.FromRgb(0, 82, 164),     // #0052A4 (경인선 - 1호선 색상)
                2 => Color.FromRgb(0, 168, 77),   // #00A84D
                21 => Color.FromRgb(0, 168, 77),     // #00A84D (성수지선 - 2호선 색상)
                22 => Color.FromRgb(0, 168, 77),     // #00A84D (신정지선 - 2호선 색상)
                3 => Color.FromRgb(239, 124, 28),    // #EF7C1C
                4 => Color.FromRgb(0, 165, 222),     // #00A5DE
                5 => Color.FromRgb(153, 108, 172),   // #996CAC
                51 => Color.FromRgb(153, 108, 172),  // #996CAC (하남선 - 5호선 색상)
                6 => Color.FromRgb(205, 124, 47),    // #CD7C2F
                7 => Color.FromRgb(116, 127, 0),     // #747F00
                8 => Color.FromRgb(230, 24, 108),    // #E6186C
                9 => Color.FromRgb(189, 176, 146),   // #BDB092
                _ => Color.FromRgb(128, 128, 128)
            };
        }

        private void AnimateResultPanel()
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            ResultPanel.BeginAnimation(OpacityProperty, fadeIn);
        }
    }
}
