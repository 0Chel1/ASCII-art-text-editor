using IDEOS.Input;
using IDEOS.Systems;
using static IDEOS.Systems.BuildingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IDEOS;

public class Game1 : Core
{
    //Behold, the messy code. I relly like compact code.
    private SpriteFont font;
    public int currentLine = 4, maxLines = 9, maxCharacters = 91;
    int cursorPositions = 0, currentCharPosInLine = 0;
    StringBuilder[] map = Enumerable.Range(0, 9).Select(_ => new StringBuilder("")).ToArray();
    public List<Structure> availableStructures = new List<Structure>();
    public List<ActiveMatch> activeMatches = new List<ActiveMatch>();
    public List<CompletedBuilding> completedBuildings = new List<CompletedBuilding>();
    public BuildingSystem buildingSys = new BuildingSystem();
    bool cursorVisible = true;
    float cursorTimer = 0f, waitBeforeFastErase = 0f, eraseInterval = 0f;

    public Game1() : base("IDEOS", 1280, 720, false)
    {
        IsMouseVisible = true;
        Window.TextInput += TextInputHandler;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        font = Content.Load<SpriteFont>("MainFont");
        foreach (StringBuilder a in map) a.Clear().Append(' ', maxCharacters);

        availableStructures.Add(new Structure
        {
            Lines = new[] { "xo", "ox" },
            color = Color.LimeGreen
        });

        availableStructures.Add(new Structure
        {
            Lines = new[] { "###", "#0#", "###" },
            color = Color.Orange
        });
    }

    protected override void Update(GameTime gameTime)
    {
        cursorTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (cursorTimer >= 0.5f)
        {
            cursorVisible = !cursorVisible;
            cursorTimer = 0f;
        }
        if (Input.Keyboard.WasKeyJustPressed(Keys.Left)) cursorPositions = Math.Max(0, cursorPositions - 1);
        if (Input.Keyboard.WasKeyJustPressed(Keys.Right)) cursorPositions = Math.Min(map[currentLine].Length, cursorPositions + 1);

        if (Input.Keyboard.WasKeyJustPressed(Keys.Down)) currentLine++;
        else if (Input.Keyboard.WasKeyJustPressed(Keys.Up)) currentLine--;

        if (Input.Keyboard.WasKeyJustPressed(Keys.Back) && cursorPositions > 0)
        {
            map[currentLine][cursorPositions - 1] = ' ';
            cursorPositions--;
        }
        else if (Input.Keyboard.IsKeyDown(Keys.Back) && cursorPositions > 0)
        {
            waitBeforeFastErase += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if(waitBeforeFastErase >= 0.5f)
            {
                eraseInterval += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if(eraseInterval >= 0.05f)
                {
                    for (int i = 0; i < completedBuildings.Count; i++)
                    { //A looooooong way if
                        if (currentLine == completedBuildings[i].StartLine + availableStructures[completedBuildings[i].StructureIndex].Lines.Length && cursorPositions == completedBuildings[i].StartColumn + availableStructures[completedBuildings[i].StructureIndex].Lines.Sum(line => line.Length))
                            completedBuildings.Remove(completedBuildings[i]);
                    }
                    map[currentLine][cursorPositions - 1] = ' ';
                    cursorPositions--;
                    eraseInterval = 0f;
                }
            }
        }
        else if (Input.Keyboard.WasKeyJustReleased(Keys.Back))
        {
            waitBeforeFastErase = 0f;
            eraseInterval = 0f;
        }

        if (Input.Mouse.WasButtonJustPressed(MouseButton.Left))
        {
            Vector2 mousePos = new Vector2(Input.Mouse.X, Input.Mouse.Y);
            Vector2 basePosition = new Vector2(0, (GraphicsDevice.Viewport.Height / 2f) - maxLines * 15);
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
                    cursorPositions = 0;
                    return;
                }

                float linePosX = basePosition.X;
                float localMouseX = mousePos.X - linePosX;
                cursorPositions = GetCharIndexAtPosition(font, lineText, localMouseX);
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
        if (args.Key != Keys.Back) //Stair
        {
            if (cursorPositions < maxCharacters)
            {
                if (cursorPositions < map[currentLine].Length)
                {
                    if (map[currentLine][cursorPositions + 1] != ' ')
                    {
                        for (int i = 0; i < map[currentLine].Length - cursorPositions; i++)
                        {
                            if (map[currentLine][cursorPositions + i] != ' ') continue;
                            else
                            {
                                map[currentLine].Remove(cursorPositions + i, 1);
                                map[currentLine].Insert(cursorPositions, character);
                                break;
                            }
                        }
                    }
                    else if (map[currentLine][cursorPositions + 1] == ' ') map[currentLine][cursorPositions] = character;
                }
                cursorTimer = 0.5f;
                cursorPositions++;
            }

            buildingSys.TryMatchStructures(character, activeMatches, availableStructures, completedBuildings, currentLine, cursorPositions);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(0.12f, 0.12f, 0.12f, 1f));

        SpriteBatch.Begin();
        Vector2 basePosition = new Vector2(0, (GraphicsDevice.Viewport.Height / 2f) - maxLines * 15);

        for (int lineIndex = 0; lineIndex < map.Length; lineIndex++)
        {
            string lineText = map[lineIndex].ToString().TrimEnd();
            if (string.IsNullOrWhiteSpace(lineText)) lineText = " ";
            Vector2 lineSize = font.MeasureString(lineText);
            float posY = basePosition.Y - (lineSize.Y / 2f) + (lineIndex * font.LineSpacing);
            Vector2 position = new Vector2(basePosition.X, posY);
            SpriteBatch.DrawString(font, lineText, position, Color.White);
            buildingSys.DrawBuildings(availableStructures, completedBuildings, lineText, lineIndex, basePosition, font, SpriteBatch);
        }

        if (cursorVisible)
        {
            string lineText = map[currentLine].ToString();
            string leftPart = cursorPositions <= lineText.Length ? lineText.Substring(0, cursorPositions) : lineText;
            Vector2 leftSize = font.MeasureString(leftPart);
            Vector2 cursorDrawPos = new Vector2(basePosition.X + leftSize.X, basePosition.Y - (font.LineSpacing / 2) + (currentLine * font.LineSpacing));
            SpriteBatch.DrawString(font, "|", cursorDrawPos, Color.White);
        }
        //Debug info
        SpriteBatch.DrawString(font, $"Line: {currentLine}  CursorPos: {cursorPositions}", new Vector2(10, 10), Color.Yellow);
        SpriteBatch.DrawString(font, $"Buildings: {completedBuildings.Count}  ActiveMatches: {activeMatches.Count}", new Vector2(10, 40), Color.Yellow);
        SpriteBatch.End(); //The end?

        base.Draw(gameTime);
    }
}