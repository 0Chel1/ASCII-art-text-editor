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
                // Добавляем текст в конец файла (не перезаписывая)
                File.AppendAllText(filePath, content + Environment.NewLine);
            }
            else
            {
                // Перезаписываем файл полностью
                File.WriteAllText(filePath, content);
            }

            Debug.WriteLine("Файл успешно сохранён: " + filePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Ошибка при сохранении файла: " + ex.Message);
        }
    }
}
