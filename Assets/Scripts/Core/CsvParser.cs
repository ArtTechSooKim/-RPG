using System.Collections.Generic;
using System.Text;

namespace WordRPG.Core
{
    // 엑셀/구글 시트에서 내보낸 CSV 파서. 따옴표로 감싼 필드 안의 쉼표·줄바꿈·"" 이스케이프를 처리한다
    public static class CsvParser
    {
        public static List<List<string>> Parse(string text)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(text)) return rows;

            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            int i = 0;

            // UTF-8 BOM 제거 (엑셀 저장 시 붙음)
            if (text[0] == '﻿') i = 1;

            for (; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(field.ToString());
                        field.Clear();
                        AddRowIfNotBlank(rows, row);
                        row = new List<string>();
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            row.Add(field.ToString());
            AddRowIfNotBlank(rows, row);
            return rows;
        }

        private static void AddRowIfNotBlank(List<List<string>> rows, List<string> row)
        {
            foreach (var value in row)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    rows.Add(row);
                    return;
                }
            }
        }
    }
}
