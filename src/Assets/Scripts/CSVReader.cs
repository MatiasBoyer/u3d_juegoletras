using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
#endif

namespace USER
{
    
    [System.Serializable]
    public class CSVReader
    {

        public readonly string[] csv_lines;
        public readonly char separator = ';';

        public List<List<string>> csv_entries = new List<List<string>>();

        public int max_rows, max_columns = 0;

        public CSVReader(string[] csvlines, char lineseparator = ';')
        {
            csv_lines = csvlines;
            separator = lineseparator;

            ReadCSV();
        }

        public void ReadCSV()
        {
            csv_entries.Clear();

            List<string> lines = csv_lines.ToList();

            int r = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Length <= 1)
                    continue;

                List<string> lentries = lines[i].Split(separator).ToList();

                csv_entries.Add(lentries);
                r++;
            }

            max_rows = r;
            max_columns = csv_entries[0].Count;
        }

        public void SaveCSV(string filePath)
        {
            string[] lines = new string[csv_entries.Count];

            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = string.Join(separator.ToString(), csv_entries[i]);
            }

            File.WriteAllLines(filePath, lines);
        }

        public void AddNewLine(List<string> content)
        {
            csv_entries.Add(content);
        }

        public string ReturnCell(int row, int column)
        {
            if(row < 0 || row >= max_rows || column < 0 || column >= max_columns)
            {
                UnityEngine.Debug.LogError($"CSVReader -> Row ({row}) or column ({column}) not in range!");
                return null;
            }

            return csv_entries[row][column];
        }

    }

#if UNITY_EDITOR
    public class CSVReaderWindow : EditorWindow
    {

        [MenuItem("Window/GAME/Show CSV Reader")]
        public static void ShowWindow()
        {
            GetWindow<CSVReaderWindow>("LOCL");
        }

        private GUIStyle centered = null;
        private CSVReader reader;

        private void OnEnable()
        {
            centered = new GUIStyle() { alignment = TextAnchor.MiddleCenter };
            centered.normal.textColor = Color.white;

            reader = GameController.instance.csvreader;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("CSV READER", centered);

            if(reader == null)
            {
                EditorGUILayout.LabelField("NO CSV LOADED!");
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show spreadsheet"))
                CSVLoc_ReaderWindow.ShowWindow();
            if (GUILayout.Button("Reload reader"))
            {
                reader.ReadCSV();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    public class CSVLoc_ReaderWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            GetWindow<CSVLoc_ReaderWindow>("Readed CSV");
        }

        private Vector2 scrollPos;

        private GUIStyle centered = null;
        private CSVReader reader;
        private void OnEnable()
        {
            centered = new GUIStyle() { alignment = TextAnchor.MiddleCenter };
            centered.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            reader = GameController.instance.csvreader;

            scrollPos =
                EditorGUILayout.BeginScrollView(scrollPos, false, false);

            EditorGUILayout.LabelField("SPREADSHEET", centered);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add new line"))
            {
                List<string> line = new List<string>();
                for (int i = 0; i < reader.max_columns; i++)
                    line.Add(";");

                reader.csv_entries.Add(line);
                reader.max_rows++;
            }
            if(GUILayout.Button("Copy CSV To Clipboard"))
            {
                TextEditor te = new TextEditor();

                List<string> totalLines = new List<string>();

                foreach(List<string> linelist in reader.csv_entries)
                {
                    totalLines.Add(string.Join(";", linelist));
                }

                te.text = string.Join("\n", totalLines);

                te.SelectAll();
                te.Copy();
            }
            EditorGUILayout.EndHorizontal();

            ShowSpreadsheet();
            EditorGUILayout.EndScrollView();
        }

        private void ShowSpreadsheet()
        {
            EditorGUILayout.BeginVertical();
            for (int y = 0; y < reader.max_rows; y++)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("X", GUILayout.MinWidth(20), GUILayout.MaxWidth(20)))
                {
                    reader.csv_entries.RemoveAt(y);
                    reader.max_rows--;
                    return;
                }
                for (int x = 0; x < reader.max_columns; x++)
                {
                    reader.csv_entries[y][x] = EditorGUILayout.TextField(reader.csv_entries[y][x]);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
    }
#endif
}