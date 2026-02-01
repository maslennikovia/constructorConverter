using System.Diagnostics;
using System.Text.Json;

namespace constructor.ifcopenshellconverter
{
    public class ConversionMetadata
    {
        public string Organization { get; set; } = "My Organization";
        public string Author { get; set; } = "STEP Converter";
        public string Application { get; set; } = "Custom IfcOpenShell Converter";
        public string Version { get; set; } = "1.0";
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
        

        public bool ConvertStepToIfc(string stepFilePath, string ifcFilePath, ConversionMetadata metadata = null)
        {
            if (!File.Exists(stepFilePath))
                throw new FileNotFoundException($"STEP файл не найден: {stepFilePath}");

            if (metadata == null)
                metadata = new ConversionMetadata();

            try
            {
                // Сериализуем метаданные в JSON
                string metadataJson = JsonSerializer.Serialize(new
                {
                    organization = metadata.Organization,
                    author = metadata.Author,
                    application = metadata.Application,
                    version = metadata.Version,
                    custom_properties = metadata.CustomProperties
                });

                // Формируем аргументы для Python скрипта
                string arguments = $"\"{_scriptPath}\" \"{stepFilePath}\" \"{ifcFilePath}\" \"{metadataJson}\"";

                var startInfo = new ProcessStartInfo
                {
                    FileName = _pythonExePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = new Process())
                {
                    process.StartInfo = startInfo;

                    // Обработка вывода
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            Console.WriteLine($"Python: {e.Data}");
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            Console.WriteLine($"Python Error: {e.Data}");
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit(300000); // Таймаут 5 минут

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при конвертации: {ex.Message}");
                return false;
            }
        }
    }
}
