using System;
using System.Threading;
using System.Threading.Tasks;

// ============================================================
// FILE: Enums/LanguageCode.cs
// ============================================================
namespace LabWork16.Enums
{
    /// <summary>
    /// Підтримувані коди мов для нормалізації запитів.
    /// </summary>
    public enum LanguageCode
    {
        English,
        Spanish,
        French,
        German,
        Ukrainian
    }
}

// ============================================================
// FILE: Interfaces/ITranslator.cs
// ============================================================
namespace LabWork16.Interfaces
{
    using LabWork16.Enums;

    /// <summary>
    /// Цільовий інтерфейс для сучасної системи перекладу.
    /// </summary>
    public interface ITranslator
    {
        /// <summary>
        /// Асинхронно перекладає текст із підтримкою скасування.
        /// </summary>
        /// <param name="text">Текст для перекладу.</param>
        /// <param name="source">Мова оригіналу.</param>
        /// <param name="target">Цільова мова.</param>
        /// <param name="cancellationToken">Токен для скасування операції.</param>
        /// <returns>Перекладений текст.</returns>
        Task<string> TranslateAsync(string text, LanguageCode source, LanguageCode target, CancellationToken cancellationToken = default);
    }
}

// ============================================================
// FILE: Interfaces/ILegacyService.cs
// ============================================================
namespace LabWork16.Interfaces
{
    /// <summary>
    /// Інтерфейс для старого (Legacy) сервісу.
    /// Дозволяє створювати Mock-об'єкти для тестування.
    /// </summary>
    public interface ILegacyService
    {
        string TranslateEnglishToSpanish(string text);
        string TranslateEnglishToFrench(string text);
    }
}

// ============================================================
// FILE: Services/LegacyTranslationService.cs
// ============================================================
namespace LabWork16.Services
{
    using LabWork16.Interfaces;

    /// <summary>
    /// Реалізація старого сервісу. 
    /// Імітує синхронну, повільну роботу (CPU-bound або блокуючий I/O).
    /// </summary>
    public class LegacyTranslationService : ILegacyService
    {
        public string TranslateEnglishToSpanish(string text)
        {
            // Імітація важкої роботи
            Thread.Sleep(1000); 
            if (text.Equals("Hello", StringComparison.OrdinalIgnoreCase)) return "Hola";
            if (text.Equals("World", StringComparison.OrdinalIgnoreCase)) return "Mundo";
            return $"[ES: {text}]";
        }

        public string TranslateEnglishToFrench(string text)
        {
            Thread.Sleep(1000);
            if (text.Equals("Hello", StringComparison.OrdinalIgnoreCase)) return "Bonjour";
            return $"[FR: {text}]";
        }
    }
}

// ============================================================
// FILE: Adapters/TranslationAdapter.cs
// ============================================================
namespace LabWork16.Adapters
{
    using LabWork16.Enums;
    using LabWork16.Interfaces;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Адаптер, який дозволяє використовувати LegacyService через інтерфейс ITranslator.
    /// </summary>
    public class TranslationAdapter : ITranslator
    {
        private readonly ILegacyService _legacyService;

        public TranslationAdapter(ILegacyService legacyService)
        {
            _legacyService = legacyService;
        }

        public async Task<string> TranslateAsync(string text, LanguageCode source, LanguageCode target, CancellationToken cancellationToken = default)
        {
            // 1. Перевірка на скасування перед початком роботи
            cancellationToken.ThrowIfCancellationRequested();

            // 2. Обгортка синхронного коду в Task.Run.
            // Це дозволяє не блокувати UI-потік, якщо LegacyService працює довго.
            return await Task.Run(() =>
            {
                // Перевірка всередині Task (якщо задача довго чекала в черзі потоків)
                cancellationToken.ThrowIfCancellationRequested();

                if (source == LanguageCode.English)
                {
                    if (target == LanguageCode.Spanish)
                    {
                        return _legacyService.TranslateEnglishToSpanish(text);
                    }
                    
                    if (target == LanguageCode.French)
                    {
                        return _legacyService.TranslateEnglishToFrench(text);
                    }
                }

                // Розширення логіки (Pivot placeholder):
                // Тут можна було б додати логіку: якщо source != English, спробувати знайти переклад Source->English, а потім English->Target.
                
                // Якщо переклад неможливий
                throw new NotSupportedException($"Переклад з мови {source} на {target} не підтримується адаптером.");

            }, cancellationToken);
        }
    }
}

// ============================================================
// FILE: Client/ChatApplication.cs
// ============================================================
namespace LabWork16.Client
{
    using LabWork16.Enums;
    using LabWork16.Interfaces;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class ChatApplication
    {
        private readonly ITranslator _translator;

        public ChatApplication(ITranslator translator)
        {
            _translator = translator;
        }

        /// <summary>
        /// Відображає повідомлення та його переклад.
        /// </summary>
        public async Task ShowMessageAsync(string user, string text, LanguageCode langFrom, LanguageCode langTo)
        {
            Console.WriteLine($"User '{user}': \"{text}\"");
            Console.Write(" -> Переклад (очікування)... ");

            // Встановлюємо таймаут для операції (наприклад, клієнт не хоче чекати більше 1.5 сек)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1.5));

            try
            {
                // Передаємо токен скасування в адаптер
                string translated = await _translator.TranslateAsync(text, langFrom, langTo, cts.Token);
                
                // Очищаємо рядок статусу і пишемо результат
                Console.Write("\r" + new string(' ', 30) + "\r"); // hack для очистки консольного рядка
                Console.WriteLine($" -> Переклад ({langTo}): \"{translated}\"");
            }
            catch (OperationCanceledException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[Timeout] Запит скасовано: Сервіс відповідає занадто довго.");
                Console.ResetColor();
            }
            catch (NotSupportedException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Error] {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Fatal] {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine(new string('-', 40));
        }
    }
}

// ============================================================
// FILE: Program.cs
// ============================================================
namespace LabWork16
{
    using LabWork16.Adapters;
    using LabWork16.Client;
    using LabWork16.Enums;
    using LabWork16.Interfaces;
    using LabWork16.Services;
    using System;
    using System.Threading.Tasks;

    class Program
    {
        // Main тепер async Task, щоб коректно працювати з асинхронними викликами
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("--- Adapter Pattern (Production Ready) ---\n");

            // 1. Setup
            ILegacyService legacyService = new LegacyTranslationService();
            ITranslator adapter = new TranslationAdapter(legacyService);
            ChatApplication chatApp = new ChatApplication(adapter);

            // 2. Успішний переклад
            await chatApp.ShowMessageAsync("John", "Hello", LanguageCode.English, LanguageCode.Spanish);

            // 3. Переклад, що не підтримується (Exception)
            await chatApp.ShowMessageAsync("Hans", "Hallo", LanguageCode.German, LanguageCode.Spanish);

            // 4. Симуляція таймауту (Cancellation)
            // Legacy сервіс має затримку 1000мс. 
            // Якщо ми зменшимо таймаут у клієнті до, наприклад, 500мс (для тесту), отримаємо Timeout.
            // (У цьому прикладі таймаут в клієнті стоїть 1.5с, тому цей виклик пройде успішно).
            await chatApp.ShowMessageAsync("Alice", "World", LanguageCode.English, LanguageCode.French);

            Console.WriteLine("\nНатисніть Enter для завершення...");
            Console.ReadLine();
        }
    }
}
