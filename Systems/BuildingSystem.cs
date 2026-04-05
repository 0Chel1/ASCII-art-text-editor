using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
namespace IDEOS.Systems;

public class BuildingSystem
{
    /// <summary>
    /// Try to currentMatch char on the current cursor position on the current line with Structure lists every structures first char in their first line.
    /// </summary>
    /// <param name="character">Char on the current cursor position on the current line.</param>
    /// <param name="activeMatches">ActiveMatches list.</param>
    /// <param name="availableStructures">Structure list.</param>
    /// <param name="completedBuildings">CompletedBuilding list.</param>
    /// <param name="currentLine"></param>
    /// <param name="cursorPositions"></param>
    public void TryMatchStructures(char character, List<ActiveMatch> activeMatches, List<Structure> availableStructures, List<CompletedBuilding> completedBuildings, int currentLine, int cursorPositions)
    {
        if(activeMatches.Count == 0)
        {
            for (int i = 0; i < availableStructures.Count; i++)
            {
                if (character == availableStructures[i].Lines[0][0])
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
        else
        { 
            /*In this gigantic for cycle i tried to make possible to place buildings in random order.
            For example, if you type xo and not finish it, and then start type another xo somewhere else it will remember it.
            If you finish a new xo you started typing and then come back to the first one and try to finish it, it will work.
            But here i tried to solve the problem with more than 2 unfinished buildings. If you unfinish 2 xo and start third 
            and then come back to the second unfinished and try to finish it, it won't work. That's what i tried to fix here.
            But i not finished it. So you can.*/
            bool correctAtCurrentOne = false;
            bool editInTheMiddle = false;
            for (int k = activeMatches.Count - 1; k >= 0; k--)
            {
                var currentMatch = activeMatches[k];
                var currentMatchStructureType = availableStructures[currentMatch.StructureIndex];
                bool isCorrectLine = currentLine == currentMatch.StartMapLine + currentMatch.CurrentLineInStructure;
                bool isCorretCursorPos = cursorPositions == currentMatch.StartCursorPos + 1 + currentMatch.CharsCountInLine;
                if (isCorrectLine || isCorretCursorPos)
                    correctAtCurrentOne = true;
                else
                {
                    for(int j = activeMatches.Count - 1; j >= 0; j--)
                    {
                        isCorrectLine = currentLine == activeMatches[j].StartMapLine + activeMatches[j].CurrentLineInStructure;
                        isCorretCursorPos = cursorPositions == activeMatches[j].StartCursorPos + 1 + activeMatches[j].CharsCountInLine;
                        if (isCorrectLine || isCorretCursorPos)
                        {
                            correctAtCurrentOne = true;
                            var currentMatch2 = activeMatches[j];
                            var currentMatchStructureType2 = availableStructures[currentMatch2.StructureIndex];
                            string currentLineInStruct2 = currentMatchStructureType2.Lines[currentMatch2.CurrentLineInStructure];
                            if (currentMatch2.CharsCountInLine < currentLineInStruct2.Length && character == currentLineInStruct2[currentMatch2.CharsCountInLine])
                            {
                                currentMatch2.CharsCount++;
                                currentMatch2.CharsCountInLine++;
                                if (currentMatch2.CharsCountInLine == currentLineInStruct2.Length)
                                {
                                    currentMatch2.CharsCountInLine = 0;
                                    currentMatch2.CurrentLineInStructure++;
                                    if (currentMatch2.CurrentLineInStructure == currentMatchStructureType2.Lines.Length)
                                    {
                                        completedBuildings.Add(new CompletedBuilding
                                        {
                                            StructureIndex = currentMatch2.StructureIndex,
                                            StartLine = currentMatch2.StartMapLine,
                                            StartColumn = currentMatch2.StartCursorPos
                                        });

                                        activeMatches.RemoveAt(k);
                                        break;
                                    }
                                }
                            }
                            editInTheMiddle = true;
                            break;
                        }
                    }
                }

                if (!correctAtCurrentOne)
                {
                    for (int i = 0; i < availableStructures.Count; i++)
                    {
                        if (character == availableStructures[i].Lines[0][0])
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
                    break;
                }

                if (editInTheMiddle)
                {
                    break;
                }
                
                string currentLineInStruct = currentMatchStructureType.Lines[currentMatch.CurrentLineInStructure];
                if (currentMatch.CharsCountInLine < currentLineInStruct.Length && character == currentLineInStruct[currentMatch.CharsCountInLine])
                {
                    currentMatch.CharsCount++;
                    currentMatch.CharsCountInLine++;
                    if (currentMatch.CharsCountInLine == currentLineInStruct.Length)
                    {
                        currentMatch.CharsCountInLine = 0;
                        currentMatch.CurrentLineInStructure++;
                        if (currentMatch.CurrentLineInStructure == currentMatchStructureType.Lines.Length)
                        {
                            completedBuildings.Add(new CompletedBuilding
                            {
                                StructureIndex = currentMatch.StructureIndex,
                                StartLine = currentMatch.StartMapLine,
                                StartColumn = currentMatch.StartCursorPos
                            });

                            activeMatches.RemoveAt(k);
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draws buildings from completedBuildings list.
    /// </summary>
    /// <param name="availableStructures">Structure list.</param>
    /// <param name="completedBuildings">CompletedBuilding list.</param>
    /// <param name="lineText"></param>
    /// <param name="lineIndex"></param>
    /// <param name="basePosition"></param>
    /// <param name="font"></param>
    /// <param name="spriteBatch"></param>
    public void DrawBuildings(List<Structure> availableStructures, List<CompletedBuilding> completedBuildings, string lineText, int lineIndex, Vector2 basePosition, SpriteFont font, SpriteBatch spriteBatch)
    {
        foreach (var building in completedBuildings)
        {
            if (building.StructureIndex < 0 || building.StructureIndex >= availableStructures.Count)
                continue;

            var structure = availableStructures[building.StructureIndex];
            int relativeLine = lineIndex - building.StartLine;

            if (relativeLine < 0 || relativeLine >= structure.Lines.Length)
                continue;

            string structLineText = structure.Lines[relativeLine];
            int startCol = building.StartColumn;

            if (startCol >= lineText.Length || string.IsNullOrEmpty(structLineText))
                continue;

            int length = Math.Min(structLineText.Length, lineText.Length - startCol);
            if (length <= 0) continue;

            string partToHighlight = structLineText.Substring(0, length);

            string leftText = lineText.Substring(0, startCol);
            Vector2 leftSize = font.MeasureString(leftText);
            Vector2 lineSize = font.MeasureString(lineText);
            float posY = basePosition.Y - (lineSize.Y / 2f) + (lineIndex * font.LineSpacing);
            Vector2 highlightPos = new Vector2(basePosition.X + leftSize.X, posY);

            spriteBatch.DrawString(font, partToHighlight, highlightPos, availableStructures[building.StructureIndex].color);
        }
    }

    public class Structure
    {
        public string[] Lines { get; set; } = Array.Empty<string>();
        public Color color { get; set; }
    }

    public class CompletedBuilding
    {
        public int StructureIndex { get; set; }
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
