using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IDEOS.Systems;

public class FilesManagement
{
    public void SaveToFile(string content, bool append = false)
    {
        try
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = Path.Combine(programFiles, "ASCII editor", "saves");
            string filePath = Path.Combine(folderPath, "default.txt");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (append)
            {
                //Adds text in the end of the file(does not rewrite the file).
                File.AppendAllText(filePath, content + Environment.NewLine);
            }
            else
            {
                //Fully rewrite the file
                File.WriteAllText(filePath, content);
            }

            Debug.WriteLine("Файл успешно сохранён: " + filePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Ошибка при сохранении файла: " + ex.Message);
        }
    }

    public string[] LoadFromFile()
    {
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(programFiles, "ASCII editor", "saves");
        string filePath = Path.Combine(folderPath, "default.txt");
        string[] a = File.ReadAllLines(filePath);
        return a;
    }
}
