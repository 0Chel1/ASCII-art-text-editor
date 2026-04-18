using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static Program;
namespace IDEOS.Systems;

public class BuildingSystem
{
    public bool push = false;
    //This method makes a mathes if the current character siquence matches with the structure.
    //If the siquence fully mathes the structure it will create the building.
    public void TryMatchStructures(char character, List<ActiveMatch> activeMatches, List<Structure> availableStructures, List<CompletedBuilding> completedBuildings, 
                                   int currentLine, int cursorPositions) {
        if (activeMatches.Count > 0)
        {
            for (int i = activeMatches.Count - 1; i >= 0; i--)
            {
                var match = activeMatches[i];
                var structure = availableStructures[match.StructureIndex];

                bool isCorrectPosition = currentLine == match.StartMapLine + match.CurrentLineInStructure && 
                     cursorPositions == match.StartCursorPos + 1 + match.CharsCountInLine;

                if (!isCorrectPosition)
                {
                    activeMatches.RemoveAt(i);
                    continue;
                }

                string currentLineInStruct = structure.Lines[match.CurrentLineInStructure];

                if (match.CharsCountInLine < currentLineInStruct.Length && character == currentLineInStruct[match.CharsCountInLine])
                {
                    match.CharsCount++;
                    match.CharsCountInLine++;

                    if (match.CharsCountInLine == currentLineInStruct.Length)
                    {
                        match.CharsCountInLine = 0;
                        match.CurrentLineInStructure++;

                        if (match.CurrentLineInStructure == structure.Lines.Length)
                        {
                            completedBuildings.Add(new CompletedBuilding
                            {
                                StructureIndex = match.StructureIndex,
                                StartLine = match.StartMapLine,
                                StartColumn = match.StartCursorPos
                            });

                            activeMatches.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    activeMatches.RemoveAt(i);
                }
            }
        }
        TryStartNewMatch(character, activeMatches, availableStructures, currentLine, cursorPositions);
    }
    //This method checks if the current character can start a new match for any structure. If it can it will add a new active match to the list.
    private void TryStartNewMatch(char character, List<ActiveMatch> activeMatches, List<Structure> availableStructures, int currentLine, int cursorPositions)
    {
        for (int i = 0; i < availableStructures.Count; i++)
        {
            var lines = availableStructures[i].Lines;
            if (lines.Length == 0 || lines[0].Length == 0) continue;

            string command = lines[0];
            if (character == command[0])
            {
                activeMatches.Add(new ActiveMatch
                {
                    StructureIndex = i,
                    CurrentLineInStructure = 0,
                    StartCursorPos = cursorPositions - 1,
                    StartMapLine = currentLine,
                    CharsCount = 1,
                    CharsCountInLine = 1
                });
            }
        }
    }
    //Draws text lines. Can color them and change the font.
    public void DrawLineWithBuildings(SpriteBatch spriteBatch, string lineText, int lineIndex, Vector2 basePosition, SpriteFont mainFont, 
        List<Structure> availableStructures, List<CompletedBuilding> completedBuildings, List<ColoredLines> coloredLines, StringBuilder[] map) {
        if (string.IsNullOrEmpty(lineText)) return;

        float currentX = basePosition.X;
        float posY = basePosition.Y - (mainFont.MeasureString(" ").Y / 2f) + (lineIndex * mainFont.LineSpacing);
        //buildingsOnLine will contain all the buildings that should be drawn on the current line.
        //We filter them by checking if their structure index is valid and if the line index is within the range of the building's start line and its height in lines.
        var buildingsOnLine = completedBuildings.Where(b => b.StructureIndex >= 0 && b.StructureIndex < availableStructures.Count)
            .Where(b =>
            {
                var s = availableStructures[b.StructureIndex];
                return lineIndex >= b.StartLine && lineIndex < b.StartLine + s.HeightInLines;
            }).OrderBy(b => b.StartColumn).ToList();

        var coloredOnThisLine = coloredLines.Where(cl => cl.StartLine == lineIndex).OrderBy(cl => cl.StartColumn).ToList();

        int currentCol = 0;

        foreach (var building in buildingsOnLine.ToList())
        {
            var structure = availableStructures[building.StructureIndex];
            int startColumn = building.StartColumn;
            //pushing command logic. Change text pushing method. 
            if (structure.Lines.Length > 0 && structure.Lines[0] == ".push")
            {
                if (push) push = false;
                else push = true;
                map[building.StartLine].Replace(map[building.StartLine].ToString().Substring(startColumn, 5).ToString(), "     ", startColumn, 5);
                completedBuildings.Remove(building);
                continue;
            }
            //coloring command logic. It checks if the line starts with .c and if it does it tries to make a colored line by checking if color typed in hex and the lenght after that.
            //Also it will color in the direction in whic the spliter in the end is facing.
            if (structure.Lines.Length > 0 && structure.Lines[0].StartsWith(".c"))
            {
                string fullLine = map[building.StartLine].ToString();

                if (startColumn + 10 < fullLine.Length)
                {
                    string r = fullLine.Substring(startColumn + 2, 2);
                    string g = fullLine.Substring(startColumn + 4, 2);
                    string b = fullLine.Substring(startColumn + 6, 2);
                    string lenStr = fullLine.Substring(startColumn + 8, 2);

                    if (int.TryParse(r, NumberStyles.HexNumber, null, out int red) && r.Any(c => c != '>' || c != '<') &&
                        int.TryParse(g, NumberStyles.HexNumber, null, out int green) && g.Any(c => c != '>' || c != '<') &&
                        int.TryParse(b, NumberStyles.HexNumber, null, out int blue) && b.Any(c => c != '>' || c != '<') &&
                        int.TryParse(lenStr, out int lengthToColor) && lenStr.Any(c => c != '>' || c != '<')) {

                        int textStartPos = startColumn + 11;

                        if (textStartPos + lengthToColor <= fullLine.Length)
                        {
                            if (fullLine[startColumn + 10] == '>')
                            {
                                map[building.StartLine].Replace(fullLine.Substring(startColumn, 11).ToString(), "", startColumn, 11);
                                foreach (ColoredLines colored in coloredLines)
                                    if (colored.StartLine == building.StartLine) colored.StartColumn = startColumn + lengthToColor;

                                string coloredText = fullLine.Substring(textStartPos, lengthToColor);
                                int newIndex = availableStructures.Count;

                                coloredLines.Add(new ColoredLines
                                {
                                    Line = coloredText,
                                    color = new Color(red, green, blue),
                                    StartLine = building.StartLine,
                                    StartColumn = startColumn
                                });
                                completedBuildings.Remove(building);
                            }
                            else if (fullLine[startColumn + 10] == '<')
                            {
                                map[building.StartLine].Replace(fullLine.Substring(startColumn, 11).ToString(), "", startColumn, 11);
                                string coloredText = fullLine.Substring(startColumn - lengthToColor, lengthToColor);
                                int newIndex = availableStructures.Count;

                                coloredLines.Add(new ColoredLines
                                {
                                    Line = coloredText,
                                    color = new Color(red, green, blue),
                                    StartLine = building.StartLine,
                                    StartColumn = startColumn - lengthToColor
                                });
                                completedBuildings.Remove(building);
                            }
                        }
                    }
                }
                continue;
            }
            //saving command logic. 
            else if (structure.Lines.Length > 0 && structure.Lines[0].StartsWith(".s"))
            {
                map[building.StartLine].Replace(map[building.StartLine].ToString().Substring(startColumn, 2).ToString(), "  ", startColumn, 2);
                FilesManagement fileManager = new FilesManagement();
                string content = "";
                foreach(var line in map)
                {
                    content += line.ToString() + "\n";
                }

                for(int i = 0;i < coloredLines.Count; i++)
                {
                    if(i < coloredLines.Count - 1)
                        content += $"{coloredLines[i].Line} {coloredLines[i].color} {coloredLines[i].StartLine} {coloredLines[i].StartColumn}\n";
                    else content += $"{coloredLines[i].Line} {coloredLines[i].color} {coloredLines[i].StartLine} {coloredLines[i].StartColumn}";
                }
                fileManager.SaveToFile(content, false);
                map[building.StartLine].Replace(map[building.StartLine].ToString().Substring(startColumn, 5).ToString(), "", startColumn, 5);
                completedBuildings.Remove(building);
                continue;
            }
            //regular text drawing.
            if (startColumn > currentCol)
            {
                int safeLength = Math.Min(startColumn - currentCol, lineText.Length - currentCol);
                if (safeLength > 0)
                {
                    string normal = lineText.Substring(currentCol, safeLength);
                    spriteBatch.DrawString(mainFont, normal, new Vector2(currentX, posY), Color.White);
                    currentX += mainFont.MeasureString(normal).X;
                    currentCol = startColumn;
                }
            }

            int relLine = lineIndex - building.StartLine;
            if (relLine >= 0 && relLine < structure.Lines.Length)
            {
                string replaceText = structure.Lines[relLine]; 
                int take = Math.Min(replaceText.Length, lineText.Length - startColumn);

                if (take > 0)
                {
                    string part = replaceText.Substring(0, take);
                    SpriteFont f = structure.Font ?? mainFont;

                    spriteBatch.DrawString(f, part, new Vector2(currentX, posY), structure.color);

                    currentX += f.MeasureString(part).X;
                    currentCol += take;
                }
            }
        }
        //Draw colored lines.
        foreach (var colored in coloredOnThisLine)
        {
            int startColumn = colored.StartColumn;
            if (string.IsNullOrEmpty(colored.Line)) continue;
            if (startColumn > currentCol)
            {
                int safeLength = Math.Min(colored.StartColumn - currentCol, lineText.Length - currentCol);
                if (safeLength > 0)
                {
                    string normal = lineText.Substring(currentCol, safeLength);
                    spriteBatch.DrawString(mainFont, normal, new Vector2(currentX, posY), Color.White);
                    currentX += mainFont.MeasureString(normal).X;
                    currentCol = startColumn;
                }
            }

            string replaceText = colored.Line;
            int take = Math.Min(replaceText.Length, lineText.Length - startColumn);
            if (take > 0)
            {
                string part = replaceText.Substring(0, take);

                spriteBatch.DrawString(mainFont, part, new Vector2(currentX, posY), colored.color);

                currentX += mainFont.MeasureString(part).X;
                currentCol += take;
            }
        }

        if (currentCol < lineText.Length)
        {
            spriteBatch.DrawString(mainFont, lineText.Substring(currentCol), new Vector2(currentX, posY), Color.White);
        }
    }

    public class Structure
    {
        public string[] Lines { get; set; } = Array.Empty<string>();
        public Color color { get; set; }
        public SpriteFont Font { get; set; }
        public int HeightInLines => Lines?.Length ?? 0;
    }

    public class CompletedBuilding
    {
        public int StructureIndex { get; set; }
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
    }

    public class ColoredLines
    {
        public string Line { get; set; }
        public Color color { get; set; }
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
    }

    public class ActiveMatch
    {
        public int StructureIndex { get; set; }
        public int CurrentLineInStructure { get; set; }
        public int StartCursorPos { get; set; }
        public int StartMapLine { get; set; }
        public int CharsCountInLine { get; set; }
        public int CharsCount { get; set; }
    }
}
