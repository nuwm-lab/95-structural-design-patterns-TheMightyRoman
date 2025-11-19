using System;
using System.Collections.Generic;
using System.Text;

namespace LabWork16
{
    // ============================================================
    // 1. TARGET INTERFACE (Цільовий інтерфейс)
    // Це інтерфейс, який очікує клієнтський код нашої нової системи.
    // ============================================================
    public interface ITranslator
    {
        string Translate(string text, string sourceLanguage, string targetLanguage);
    }

    // ============================================================
    // 2. ADAPTEE (Адаптуємий клас - Стара система)
    // Це старий сервіс або стороння бібліотека, яка має інший інтерфейс.
    // Наприклад, вона підтримує лише конкретну пару мов і має іншу назву методу.
    // ============================================================
    public class LegacyTranslationService
    {
        // Старий метод, який вміє перекладати лише з Англійської на Іспанську
        public string TranslateEnglishToSpanish(string text)
        {
            Console.WriteLine("   [LegacyService] Processing translation...");
            // Проста імітація перекладу для демонстрації
            if (text == "Hello") return "Hola";
            if (text == "World") return "Mundo";
            return $"[Translated to Spanish: {text}]";
        }

        // Інший метод для французької
        public string TranslateEnglishToFrench(string text)
        {
             Console.WriteLine("   [LegacyService] Processing translation...");
             if (text == "Hello") return "Bonjour";
             return $"[Translated to French: {text}]";
        }
    }

    // ============================================================
    // 3. ADAPTER (Адаптер)
    // Цей клас реалізує цільовий інтерфейс ITranslator і всередині
    // використовує об'єкт LegacyTranslationService.
    // ============================================================
    public class TranslationAdapter : ITranslator
    {
        private readonly LegacyTranslationService _legacyService;

        public TranslationAdapter(LegacyTranslationService legacyService)
        {
            _legacyService = legacyService;
        }

        public string Translate(string text, string sourceLanguage, string targetLanguage)
        {
            // Адаптація викликів:
            // Якщо запит відповідає можливостям старого сервісу, викликаємо його методи.
            
            if (sourceLanguage.ToLower() == "en" && targetLanguage.ToLower() == "es")
            {
                return _legacyService.TranslateEnglishToSpanish(text);
            }
            else if (sourceLanguage.ToLower() == "en" && targetLanguage.ToLower() == "fr")
            {
                return _legacyService.TranslateEnglishToFrench(text);
            }
            else
            {
                // Якщо стара система не підтримує таку пару, можна кинути помилку або повернути заглушку
                return $"Error: Legacy service does not support translation from {sourceLanguage} to {targetLanguage}.";
            }
        }
    }

    // ============================================================
    // 4. CLIENT (Клієнт)
    // Клас, який використовує інтерфейс ITranslator
    // ============================================================
    public class ChatApplication
    {
        private readonly ITranslator _translator;

        public ChatApplication(ITranslator translator)
        {
            _translator = translator;
        }

        public void ShowMessage(string user, string text, string langFrom, string langTo)
        {
            Console.WriteLine($"\nUser '{user}' says: \"{text}\"");
            string translated = _translator.Translate(text, langFrom, langTo);
            Console.WriteLine($" > Переклад ({langTo}): \"{translated}\"");
        }
    }

    // ============================================================
    // MAIN PROGRAM
    // ============================================================
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Патерн Адаптер: Перекладач у реальному часі\n");

            // 1. Створюємо старий сервіс (Adaptee)
            LegacyTranslationService oldService = new LegacyTranslationService();

            // 2. Створюємо адаптер, який "обгортає" старий сервіс
            ITranslator adapter = new TranslationAdapter(oldService);

            // 3. Створюємо клієнтський додаток, передаючи йому адаптер
            // Клієнт думає, що працює з сучасним інтерфейсом ITranslator
            ChatApplication chatApp = new ChatApplication(adapter);

            // 4. Демонстрація роботи
            chatApp.ShowMessage("John", "Hello", "en", "es");
            chatApp.ShowMessage("Alice", "World", "en", "es");
            
            chatApp.ShowMessage("Bob", "Hello", "en", "fr");

            // Спроба непідтримуваного перекладу
            chatApp.ShowMessage("Admin", "Test", "en", "de");

            Console.WriteLine("\nНатисніть Enter для завершення...");
            Console.ReadLine();
        }
    }
}
