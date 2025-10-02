#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Game.Editor
{
    /// <summary>
    /// 그리드 좌표 검증 에디터 도구
    /// Vector2Int 사용의 일관성을 검사합니다
    /// </summary>
    public static class GridCoordinateValidator
    {
        private const string SCRIPT_PATH = "Assets/Script/Game";

        [MenuItem("Tools/Grid/Validate Coordinates")]
        public static void ValidateAllCoordinates()
        {
            Debug.Log("=== Grid Coordinate Validation Started ===");
            Debug.Log($"Scanning directory: {SCRIPT_PATH}");

            if (!Directory.Exists(SCRIPT_PATH))
            {
                Debug.LogError($"Directory not found: {SCRIPT_PATH}");
                return;
            }

            var scriptFiles = Directory.GetFiles(
                SCRIPT_PATH,
                "*.cs",
                SearchOption.AllDirectories
            );

            Debug.Log($"Found {scriptFiles.Length} C# files to analyze");

            // 분석 패턴들
            var vector2IntPattern = new Regex(@"new\s+Vector2Int\s*\(\s*(\w+)\s*,\s*(\w+)\s*\)");
            var loopVariablePattern = new Regex(@"for\s*\(\s*int\s+([xy])\s*=");

            var suspiciousUsages = new List<ValidationIssue>();
            int totalVector2IntCount = 0;
            int fileCount = 0;

            foreach (var file in scriptFiles)
            {
                var fileName = Path.GetFileName(file);

                // .md 파일이나 에디터 도구 자신은 제외
                if (fileName.EndsWith(".md") || fileName == "GridCoordinateValidator.cs")
                    continue;

                var content = File.ReadAllLines(file);
                bool hasIssues = false;

                for (int lineNum = 0; lineNum < content.Length; lineNum++)
                {
                    var line = content[lineNum];

                    // Vector2Int 생성 검사
                    var matches = vector2IntPattern.Matches(line);
                    foreach (Match match in matches)
                    {
                        totalVector2IntCount++;
                        var param1 = match.Groups[1].Value;
                        var param2 = match.Groups[2].Value;

                        // 의심스러운 패턴 감지
                        bool suspicious = false;
                        string reason = "";

                        // Pattern 1: (x, y) 순서 (일반적이지만 주의 필요)
                        if (param1.ToLower() == "x" && param2.ToLower() == "y")
                        {
                            suspicious = true;
                            reason = "순서 확인 필요: Vector2Int(x, y) - x=가로(col), y=세로(row) 맞는지 확인";
                        }
                        // Pattern 2: (y, x) 순서 (반대로 쓴 경우)
                        else if (param1.ToLower() == "y" && param2.ToLower() == "x")
                        {
                            suspicious = true;
                            reason = "⚠️ 역순 사용: Vector2Int(y, x) - 의도적인지 확인 필요";
                        }
                        // Pattern 3: (row, col) - 좋은 패턴이지만 순서 확인
                        else if (param1.ToLower().Contains("row") && param2.ToLower().Contains("col"))
                        {
                            suspicious = true;
                            reason = "⚠️ Vector2Int(row, col) - 올바른 순서는 (col, row)";
                        }

                        if (suspicious)
                        {
                            suspiciousUsages.Add(new ValidationIssue
                            {
                                FilePath = file,
                                FileName = fileName,
                                LineNumber = lineNum + 1,
                                Code = line.Trim(),
                                Reason = reason,
                                Severity = GetSeverity(reason)
                            });
                            hasIssues = true;
                        }
                    }

                    // 루프 변수명 검사
                    var loopMatches = loopVariablePattern.Matches(line);
                    foreach (Match match in loopMatches)
                    {
                        var varName = match.Groups[1].Value;

                        // 다음 몇 줄에서 Vector2Int 생성이 있는지 확인
                        for (int j = lineNum; j < Mathf.Min(lineNum + 10, content.Length); j++)
                        {
                            if (content[j].Contains("new Vector2Int"))
                            {
                                suspiciousUsages.Add(new ValidationIssue
                                {
                                    FilePath = file,
                                    FileName = fileName,
                                    LineNumber = lineNum + 1,
                                    Code = line.Trim(),
                                    Reason = $"루프 변수 '{varName}' 사용 - 의미 명확히 할 것 권장 (xOffset, yOffset 등)",
                                    Severity = IssueSeverity.Warning
                                });
                                hasIssues = true;
                                break;
                            }
                        }
                    }
                }

                if (hasIssues)
                    fileCount++;
            }

            // 결과 출력
            Debug.Log($"\n📊 Validation Statistics:");
            Debug.Log($"  Total Vector2Int instances: {totalVector2IntCount}");
            Debug.Log($"  Files with issues: {fileCount}");
            Debug.Log($"  Total issues found: {suspiciousUsages.Count}");

            if (suspiciousUsages.Count > 0)
            {
                // 심각도별로 그룹화
                var criticalIssues = suspiciousUsages.Where(i => i.Severity == IssueSeverity.Critical).ToList();
                var warningIssues = suspiciousUsages.Where(i => i.Severity == IssueSeverity.Warning).ToList();
                var infoIssues = suspiciousUsages.Where(i => i.Severity == IssueSeverity.Info).ToList();

                if (criticalIssues.Count > 0)
                {
                    Debug.LogWarning($"\n🔴 CRITICAL Issues ({criticalIssues.Count}):");
                    foreach (var issue in criticalIssues)
                    {
                        Debug.LogWarning($"  {issue.FileName}:{issue.LineNumber}\n    {issue.Code}\n    → {issue.Reason}");
                    }
                }

                if (warningIssues.Count > 0)
                {
                    Debug.LogWarning($"\n🟡 Warnings ({warningIssues.Count}):");
                    foreach (var issue in warningIssues.Take(10)) // 처음 10개만 표시
                    {
                        Debug.LogWarning($"  {issue.FileName}:{issue.LineNumber}\n    {issue.Code}\n    → {issue.Reason}");
                    }
                    if (warningIssues.Count > 10)
                    {
                        Debug.LogWarning($"  ... and {warningIssues.Count - 10} more warnings");
                    }
                }

                if (infoIssues.Count > 0)
                {
                    Debug.Log($"\n🟢 Info ({infoIssues.Count}): Review recommended");
                }

                // 상세 보고서 생성
                GenerateReport(suspiciousUsages);
            }
            else
            {
                Debug.Log("✅ No coordinate issues found!");
            }

            Debug.Log("=== Grid Coordinate Validation Completed ===\n");
        }

        [MenuItem("Tools/Grid/Show Coordinate Rules")]
        public static void ShowCoordinateRules()
        {
            string rules = @"
=== Grid Coordinate System Rules ===

좌표계 정의 (카르테시안 좌표계):
  • 기준점: 좌하단(0, 0)
  • X축: 가로 방향 (좌→우 증가) = Column
  • Y축: 세로 방향 (하→상 증가) = Row

Vector2Int 구조:
  • Vector2Int.x = X축 = Column (가로 위치)
  • Vector2Int.y = Y축 = Row (세로 위치)
  • Vector2Int(x, y) = Vector2Int(column, row)

✅ 올바른 사용 예:
  • new Vector2Int(col, row)
  • GridPositionHelper.CreateXY(x, y)
  • GridPositionHelper.CreateColRow(col, row)

⚠️ 주의할 패턴:
  • new Vector2Int(x, y) - x가 가로, y가 세로 맞는지 확인
  • new Vector2Int(row, col) - 잘못된 순서!
  • 루프 변수 x, y - xOffset, yOffset 등으로 명확히

📚 헬퍼 메서드:
  • GridPositionHelper.CalculateXDistance() - 가로 거리
  • GridPositionHelper.CalculateYDistance() - 세로 거리
  • GridPositionHelper.CalculateManhattanDistance() - 맨하탄 거리

See: GridPositionHelper.cs for more utilities
";
            Debug.Log(rules);
            EditorUtility.DisplayDialog("Grid Coordinate Rules", rules, "OK");
        }

        private static void GenerateReport(List<ValidationIssue> issues)
        {
            string reportPath = "Assets/Script/Game/Editor/CoordinateValidationReport.txt";

            using (StreamWriter writer = new StreamWriter(reportPath))
            {
                writer.WriteLine("Grid Coordinate Validation Report");
                writer.WriteLine($"Generated: {System.DateTime.Now}");
                writer.WriteLine($"Total Issues: {issues.Count}\n");
                writer.WriteLine(new string('=', 80));

                foreach (var issue in issues.OrderBy(i => i.Severity).ThenBy(i => i.FileName))
                {
                    writer.WriteLine($"\n[{issue.Severity}] {issue.FileName}:{issue.LineNumber}");
                    writer.WriteLine($"Code: {issue.Code}");
                    writer.WriteLine($"Reason: {issue.Reason}");
                    writer.WriteLine(new string('-', 80));
                }
            }

            Debug.Log($"📄 Detailed report saved: {reportPath}");
            AssetDatabase.Refresh();
        }

        private static IssueSeverity GetSeverity(string reason)
        {
            if (reason.Contains("⚠️") || reason.Contains("역순") || reason.Contains("잘못된"))
                return IssueSeverity.Critical;
            if (reason.Contains("확인 필요") || reason.Contains("순서 확인"))
                return IssueSeverity.Warning;
            return IssueSeverity.Info;
        }

        private class ValidationIssue
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public int LineNumber { get; set; }
            public string Code { get; set; }
            public string Reason { get; set; }
            public IssueSeverity Severity { get; set; }
        }

        private enum IssueSeverity
        {
            Info,
            Warning,
            Critical
        }
    }
}
#endif
