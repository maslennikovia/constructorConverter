using System;
using System.Threading;
using System.Threading.Tasks;
using constructor.converter.step.Models;
using constructor.converter.step.Services.Implementations;

namespace constructor.converter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Конвертер STEP в IFC ===\n");

            try
            {
                // Ввод пути исходного файла
                Console.Write("Введите путь к STEP файлу: ");
                string stepPath = Console.ReadLine()?.Trim('"').Trim();
                
                if (!File.Exists(stepPath))
                {
                    Console.WriteLine("Ошибка: Файл не найден!");
                    return;
                }

                // Ввод пути для сохранения
                Console.Write("Введите путь для сохранения IFC файла: ");
                string outputPath = Console.ReadLine()?.Trim('"').Trim();

                // Ввод параметров конвертации
                Console.Write("Введите линейное отклонение (например, 1): ");
                double linearDeflection = double.Parse(Console.ReadLine());

                Console.Write("Введите угловое отклонение (например, 0.5): ");
                double angularDeflection = double.Parse(Console.ReadLine());

                Console.WriteLine("\nНачинаю конвертацию...\n");

                // Создаем объект для отслеживания прогресса
                var progress = new Progress<ConversionProgress>();
                progress.ProgressChanged += (sender, p) =>
                {
                    UpdateProgressBar(p);
                };

                // Запускаем конвертацию
                await Task.Run(() =>
                {
                    var converter = new StepConverter();
                    converter.ConvertToIfc(stepPath, outputPath, progress, linearDeflection, angularDeflection);
                });

                Console.WriteLine("\n\n✅ Конвертация успешно завершена!");
                Console.WriteLine($"Файл сохранен: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Ошибка: {ex.Message}");
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        // Метод для обновления прогресс-бара
        static void UpdateProgressBar(ConversionProgress progress)
        {
            int barLength = 50;
            int filledLength = (int)(barLength * progress.Percentage / 100);
            
            string bar = new string('█', filledLength) + new string('░', barLength - filledLength);
            
            Console.Write($"\rПрогресс: [{bar}] {progress.Percentage:F1}% | {progress.Stage}");
            
            // Добавляем пробелы для очистки возможного предыдущего длинного текста
            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft - 1));
        }
    }
}
