using IDEOS.Input;
using IDEOS.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static IDEOS.Systems.BuildingSystem;

namespace IDEOS;

public class MainProg : Core
{
    private SpriteFont font;
    public int maxLines = 27, maxCharacters = 117;
    int Yoffset = 415;
    int cursorPosition = 0, currentLine = 0;

    //Lists
    StringBuilder[] map; //map of the characters
    public List<Structure> availableStructures = new List<Structure>();
    public List<ActiveMatch> activeMatches = new List<ActiveMatch>();
    public List<CompletedBuilding> completedBuildings = new List<CompletedBuilding>();
    public List<ColoredLines> coloredLines = new List<ColoredLines>();


    public BuildingSystem buildingSys = new BuildingSystem();
    public FilesManagement fileManager = new FilesManagement();

    public bool cursorVisible = true;
    float cursorTimer = 0f, waitBeforeFastErase = 0f, eraseInterval = 0f; //text cursor blinking

    public MainProg() : base("ASCII art editor", 1640, 860, false)
    {
        IsMouseVisible = true;
        Window.TextInput += TextInputHandler;
    }

    protected override void Initialize()
    {
        map = Enumerable.Range(0, maxLines).Select(_ => new StringBuilder("")).ToArray();
        currentLine = maxLines / 2;
        cursorPosition = maxCharacters / 2;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        font = Content.Load<SpriteFont>("MainFont");
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string filePath = Path.Combine(programFiles, "ASCII editor", "saves", "default.txt");
        if (File.Exists(filePath))
        {
            string[] content = fileManager.LoadFromFile();

            for (int i = 0; i < map.Length; i++) map[i] = new StringBuilder(content[i]);

            for (int i = map.Length; i < content.Length; i++)
            {
                var parts = content[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 7) continue;

                string lineText = parts[0];

                int ParseComponent(string token)
                {
                    var idx = token.IndexOf(':');
                    var numStr = idx >= 0 ? token[(idx + 1)..] : token;
                    return int.TryParse(numStr, out var v) ? v : 0;
                }

                int r = ParseComponent(parts[1]);
                int g = ParseComponent(parts[2]);
                int b = ParseComponent(parts[3]);

                if (!int.TryParse(parts[5], out int startLine)) continue;
                if (!int.TryParse(parts[6], out int startColumn)) continue;

                coloredLines.Add(new ColoredLines
                {
                    Line = lineText,
                    color = new Color(r, g, b),
                    StartLine = startLine,
                    StartColumn = startColumn
                });
            }
        }
        else foreach (StringBuilder a in map) a.Append(' ', maxCharacters); //if file default.txt doesn't exist, fill the map with empty spaces

        availableStructures.Add(new Structure { Lines = new[] { ".c" } }); //color the line of text

        availableStructures.Add(new Structure { Lines = new[] { ".push" } }); //change text pushing method

        availableStructures.Add(new Structure { Lines = new[] { ".s" } }); //save everything to file

        availableStructures.Add(new Structure
        {
            Lines = new[] { "special attack" },
            color = Color.Blue,
            Font = font
        });

        availableStructures.Add(new Structure
        {
            Lines = new[] { "xo", "ox" },
            color = Color.LimeGreen,
            Font = font
        });
    }

    protected override void Update(GameTime gameTime)
    {
        cursorTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (cursorTimer >= 0.5f)
        {
            cursorVisible = !cursorVisible;
            foreach(var building in completedBuildings.ToList())
            {
                if (map[building.StartLine].ToString().Substring(building.StartColumn, availableStructures[building.StructureIndex].Lines[0].Length).All(c => char.IsWhiteSpace(c)))
                {
                    completedBuildings.Remove(building);
                }
            }

            for(int i = 0; i < coloredLines.Count; i++)
            {
                if (map[coloredLines[i].StartLine].ToString().Substring(coloredLines[i].StartColumn, coloredLines[i].Line.Length).All(c => char.IsWhiteSpace(c)))
                {
                    coloredLines.Remove(coloredLines[i]);
                }
            }
            cursorTimer = 0f;
        }
        //moves text cursor to the left or right
        if (Input.Keyboard.WasKeyJustPressed(Keys.Left)) cursorPosition = Math.Max(0, cursorPosition - 1);
        if (Input.Keyboard.WasKeyJustPressed(Keys.Right)) cursorPosition = Math.Min(map[currentLine].Length, cursorPosition + 1);
        //moves text cursor up or down
        if (Input.Keyboard.WasKeyJustPressed(Keys.Down)) currentLine++;
        else if (Input.Keyboard.WasKeyJustPressed(Keys.Up)) currentLine--;
        //erase characters. If being held, it will earse them faster
        if (Input.Keyboard.WasKeyJustPressed(Keys.Back) && cursorPosition > 0)
        {
            map[currentLine][cursorPosition - 1] = ' ';
            cursorPosition--;
        }
        else if (Input.Keyboard.IsKeyDown(Keys.Back) && cursorPosition > 0)
        {
            waitBeforeFastErase += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if(waitBeforeFastErase >= 0.5f)
            {
                eraseInterval += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if(eraseInterval >= 0.05f)
                {
                    map[currentLine][cursorPosition - 1] = ' ';
                    cursorPosition--;
                    eraseInterval = 0f;
                }
            }
        }
        else if (Input.Keyboard.WasKeyJustReleased(Keys.Back))
        {
            waitBeforeFastErase = 0f;
            eraseInterval = 0f;
        }
        //places text cursor where the mouse clicks
        if (Input.Mouse.WasButtonJustPressed(MouseButton.Left))
        {
            Vector2 mousePos = new Vector2(Input.Mouse.X, Input.Mouse.Y);
            Vector2 basePosition = new Vector2(0, (Window.ClientBounds.Height / 2f) - Yoffset);
            int clickedLine = -1;
            float minDistanceY = float.MaxValue;
            for (int i = 0; i < map.Length; i++)
            {
                string lineText = map[i].Length == 0 ? " " : map[i].ToString();
                Vector2 textSize = font.MeasureString(lineText);
                float posY = basePosition.Y - (textSize.Y / 2) + (i * font.LineSpacing);
                float lineCenterY = posY + (font.LineSpacing / 2f);
                float distY = Math.Abs(mousePos.Y - lineCenterY);
                if (distY < minDistanceY)
                {
                    minDistanceY = distY;
                    clickedLine = i;
                }
            }

            if (clickedLine >= 0 && clickedLine < map.Length)
            {
                currentLine = clickedLine;
                string lineText = map[currentLine].ToString();
                if (string.IsNullOrEmpty(lineText))
                {
                    cursorPosition = 0;
                    return;
                }

                float linePosX = basePosition.X;
                float localMouseX = mousePos.X - linePosX;
                cursorPosition = GetCharIndexAtPosition(font, lineText, localMouseX);
            }
        }

        if (currentLine < 0) currentLine = 0;
        else if (currentLine > maxLines) currentLine = maxLines;

        base.Update(gameTime);
    }

    private int GetCharIndexAtPosition(SpriteFont font, string text, float localX)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        float currentWidth = 0f;
        for (int i = 0; i < text.Length; i++)
        {
            string substring = text.Substring(0, i + 1);
            float charWidthUpToHere = font.MeasureString(substring).X;
            if (localX < (currentWidth + (charWidthUpToHere - currentWidth) / 2f)) return i;
            currentWidth = charWidthUpToHere;
        }
        return text.Length;
    }

    public void TextInputHandler(object sender, TextInputEventArgs args)
    {
        char character = args.Character;
        Keys pressedKey = args.Key;
        if (args.Key != Keys.Back)
        {
            int count = 0;
            for(int i = 0;i < maxLines; i++)
            {
                for(int j = 0;j < map[i].Length - 1; j++)
                {
                    if (j > maxCharacters) count++;
                }
                map[i].Remove(maxCharacters, count);
                count = 0;
            }

            if (cursorPosition < maxCharacters)
            {
                if (cursorPosition < map[currentLine].Length)
                {
                    if (map[currentLine][cursorPosition] != ' ')
                    {
                        for (int i = 0; i < map[currentLine].Length - cursorPosition; i++)
                        {
                            if (map[currentLine][cursorPosition + i] != ' ') continue;
                            else
                            {
                                if(buildingSys.push) map[currentLine].Remove(cursorPosition + i, 1);
                                map[currentLine].Insert(cursorPosition, character);
                                foreach (ColoredLines colored in coloredLines)
                                    if (colored.StartLine == currentLine) colored.StartColumn++;
                                break;
                            }
                        }
                    }
                    else map[currentLine][cursorPosition] = character;
                }
                else map[currentLine].Append(character);
                cursorTimer = 0.5f;
                cursorPosition++;
            }
            //matching system from BuildingSystem.cs.
            buildingSys.TryMatchStructures(character, activeMatches, availableStructures, completedBuildings, currentLine, cursorPosition);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(0.12f, 0.12f, 0.12f, 1f));

        SpriteBatch.Begin();
        Vector2 basePosition = new Vector2(0, (Window.ClientBounds.Height / 2f) - Yoffset);

        for (int lineIndex = 0; lineIndex < map.Length; lineIndex++)
        {
            string lineText = map[lineIndex].ToString().TrimEnd();
            if (string.IsNullOrWhiteSpace(lineText)) lineText = " ";
            
            buildingSys.DrawLineWithBuildings(SpriteBatch, lineText, lineIndex, basePosition, font, availableStructures, completedBuildings, coloredLines, map);
        }

        if (cursorVisible)
        {
            string lineText = map[currentLine].ToString();
            string leftPart = cursorPosition <= lineText.Length ? lineText.Substring(0, cursorPosition) : lineText;
            Vector2 leftSize = font.MeasureString(leftPart);
            Vector2 cursorDrawPos = new Vector2(basePosition.X + leftSize.X, basePosition.Y - (font.LineSpacing / 2) + (currentLine * font.LineSpacing));
            SpriteBatch.DrawString(font, "|", cursorDrawPos, Color.White);
        }
        //Debug info V
        //SpriteBatch.DrawString(font, $"Line: {currentLine}  CursorPos: {cursorPosition}", new Vector2(10, 10), Color.Yellow);
        //SpriteBatch.DrawString(font, $"Buildings: {completedBuildings.Count}  ActiveMatches: {activeMatches.Count} ColoredText {coloredLines.Count}", new Vector2(10, 40), Color.Yellow);
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}