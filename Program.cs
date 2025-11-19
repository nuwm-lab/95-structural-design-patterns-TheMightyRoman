using System;
using System.Threading.Tasks;

namespace LabWork16_Refactored
{
    // ============================================================
    // 1. ТИПИ ДАНИХ (Enums)
    // Використання Enum замість string запобігає помилкам регістру
    // та нормалізує роботу з мовами.
    // ============================================================
    public enum LanguageCode
    {
        English,
        Spanish,
        French,
        German,
        Unsupported // Для тестів
    }

    // ============================================================
    // 2. TARGET INTERFACE (Цільовий інтерфейс)
    // Оновлено до асинхронної версії.
    // ============================================================
    public interface ITranslator
    {
        /// <summary>
        /// Асинхронно перекладає текст.
        /// </summary>
        /// <exception cref="NotSupportedException">Якщо пара мов не підтримується.</exception>
        Task<string> TranslateAsync(string text, LanguageCode source, LanguageCode target);
    }

    // ============================================================
    // 3. ADAPTEE (Адаптуємий клас - Legacy)
    // Виділили інтерфейс для можливості Mock-тестування
    // ============================================================
    public interface ILegacyService
    {
        string TranslateEnglishToSpanish(string text);
        string TranslateEnglishToFrench(string text);
    }

    public class LegacyTranslationService : ILegacyService
    {
        // Метод більше нічого не пише в консоль, тільки повертає результат.
        public string TranslateEnglishToSpanish(string text)
        {
            // Імітація логіки перекладу
            if (text.Equals("Hello", StringComparison.OrdinalIgnoreCase)) return "Hola";
            if (text.Equals("World", StringComparison.OrdinalIgnoreCase)) return "Mundo";
            return $"[ES: {text}]";
        }

        public string TranslateEnglishToFrench(string text)
        {
            if (text.Equals("Hello", StringComparison.OrdinalIgnoreCase)) return "Bonjour";
            return $"[FR: {text}]";
        }
    }

    // ============================================================
    // 4. ADAPTER (Адаптер)
    // ============================================================
    public class TranslationAdapter : ITranslator
    {
        private readonly ILegacyService _legacyService;

        public TranslationAdapter(ILegacyService legacyService)
        {
            _legacyService = legacyService;
        }

        public async Task<string> TranslateAsync(string text, LanguageCode source, LanguageCode target)
        {
            // Емуляція асинхронної роботи (запит до мережі/API)
            // Це робить "real-time" більш правдоподібним
            await Task.Delay(500); 

            // Логіка адаптації
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

            // Правильна обробка помилок через Exception
            throw new NotSupportedException($"Переклад з {source} на {target} наразі не підтримується старою системою.");
        }
    }

    // ============================================================
    // 5. CLIENT (Клієнт)
    // Відповідає за UI (Console.WriteLine) та обробку помилок
    // ============================================================
    public class ChatApplication
    {
        private readonly ITranslator _translator;

        public ChatApplication(ITranslator translator)
        {
            _translator = translator;
        }

        public async Task ShowMessageAsync(string user, string text, LanguageCode langFrom, LanguageCode langTo)
        {
            Console.WriteLine($"User '{user}': \"{text}\"");
            Console.Write(" -> Переклад... "); // Індикація процесу

            try
            {
                string translated = await _translator.TranslateAsync(text, langFrom, langTo);
                
                // Перезаписуємо рядок статусу результатом
                Console.WriteLine($"Done: \"{translated}\"");
            }
            catch (NotSupportedException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Помилка: {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Невідома помилка: {ex.Message}");
            }
            
            Console.WriteLine(new string('-', 30));
        }
    }

    // ============================================================
    // MAIN PROGRAM
    // ============================================================
    class Program
    {
        // Main тепер теж async Task
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Патерн Адаптер (Refactored): Async & Error Handling\n");

            // 1. Setup (Dependency Injection)
            ILegacyService legacyService = new LegacyTranslationService();
            ITranslator adapter = new TranslationAdapter(legacyService);
            ChatApplication chatApp = new ChatApplication(adapter);

            // 2. Успішні кейси
            await chatApp.ShowMessageAsync("John", "Hello", LanguageCode.English, LanguageCode.Spanish);
            await chatApp.ShowMessage("Alice", "World", LanguageCode.English, LanguageCode.Spanish);
            await chatApp.ShowMessage("Bob", "Hello", LanguageCode.English, LanguageCode.French);

            // 3. Кейс з помилкою (Exception Handling)
            // Спроба перекладу на німецьку, яку легасі-код не знає
            await chatApp.ShowMessageAsync("Admin", "Critical Error", LanguageCode.English, LanguageCode.German);

            Console.WriteLine("\nРоботу завершено.");
            Console.ReadLine();
        }
    }
}
