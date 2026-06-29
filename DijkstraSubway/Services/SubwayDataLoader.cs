using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DijkstraSubway.Models;

namespace DijkstraSubway.Services
{
    public class SubwayDataLoader
    {
        // csv파일에서 역 정보 로드
        public static (List<Station> stations, List<StationDistance> distances) LoadSubwayData(string filename)
        {
            var stations = new List<Station>();
            var distances = new List<StationDistance>();

            string filePath = FindFile(filename);

            // 파일을 못찾으면
            if (filePath == null)
            {
                throw new FileNotFoundException($"파일을 찾을 수 없습니다: {filename}");
            }

            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                // 파일 인코딩
                var lines = File.ReadAllLines(filePath, System.Text.Encoding.GetEncoding("EUC-KR"));
                // 열 이름
                bool firstLine = true;

                foreach (var line in lines)
                {
                    // 열 이름 스킵
                    if (firstLine)
                    {
                        firstLine = false;
                        continue;
                    }

                    // 여백 스킵
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // csv파일 불완정성 검사
                    var fields = SplitCsv(line);
                    if (fields.Length < 4)
                        continue;

                    string codeStr = fields[0].Trim();
                    string name = fields[1].Trim();
                    string lineStr = fields[2].Trim();
                    string distStr = fields[3].Trim();

                    if (string.IsNullOrEmpty(codeStr) || string.IsNullOrEmpty(name))
                        continue;

                    if (!int.TryParse(codeStr, out int code))
                        continue;

                    if (!double.TryParse(distStr, out double dist))
                        dist = 0;

                    // 호선 번호 추출
                    string lineNumStr = ExtractLineNumber(lineStr);
                    if (!int.TryParse(lineNumStr, out int lineNum))
                        continue;

                    // 역명 괄호 안의 내용 제거
                    string normalizedName = NormalizeName(name);

                    var station = new Station
                    {
                        Name = normalizedName,
                        Line = lineNum,
                        Order = code
                    };
                    stations.Add(station);

                    var distance = new StationDistance
                    {
                        Line = lineNum,
                        Name = normalizedName,
                        Distance = dist
                    };
                    distances.Add(distance);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"파일 로드 실패: {ex.Message}", ex);
            }

            return (stations, distances);
        }

        // 파일에 대한 경로찾기
        private static string? FindFile(string filename)
        {
            string[] paths = new[]
 {
   filename,
  Path.Combine("..", filename),
       Path.Combine("..", "..", filename),
   Path.Combine("..", "..", "..", filename),
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename)
       };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }

            return null;
        }

        private static string[] SplitCsv(string line)
        {
            var result = new List<string>();
            var current = "";
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            result.Add(current);
            return result.ToArray();
        }

        private static string ExtractLineNumber(string lineStr)
        {
            // "01호선" 형태에서 숫자만 추출
            var numStr = System.Text.RegularExpressions.Regex.Match(lineStr, @"\d+").Value;
            return numStr;
        }

        private static string NormalizeName(string name)
        {
            // 앞쪽 공백 제거
            name = name.TrimStart();

            // 괄호 이후 내용 제거
            int parenIdx = name.IndexOf('(');
            if (parenIdx >= 0)
            {
                name = name.Substring(0, parenIdx);
            }

            // 뒤쪽 공백 제거
            name = name.TrimEnd();

            return name;
        }
    }
}
