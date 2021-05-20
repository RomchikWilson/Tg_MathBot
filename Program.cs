using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InlineQueryResults;


namespace TelegramBot
{
    class Program
    {
        private static string token { get; set; } = "1893908734:AAFjV2ht3yuuPWYVph2GO-xvLe478y28pO8"; //Токен Телеграм бота
        private static AnEquationAndAnswers anEquationAndAnswers; //Создатель уравнений
        private static TelegramBotClient client; //Слушатель Телеграм бота  

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;

            anEquationAndAnswers = new AnEquationAndAnswers();

            client = new TelegramBotClient(token);
            client.OnMessage += OnMessageHandlet;
            client.OnCallbackQuery += OnInlineButtonHandlet;

            client.StartReceiving();
            Console.WriteLine("Telegram Bot — START");

            Console.ReadLine();

            client.StopReceiving();
        }

        private static async void OnMessageHandlet(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            string[] answers;
            int indexCorrectAnswer;
            InlineKeyboardButton[] ikb;
            InlineKeyboardMarkup ikm;

            ReplyKeyboardMarkup rkm = new ReplyKeyboardMarkup();

            switch (msg.Text)
            {
                case "/start":
                    Console.WriteLine($"Входящее: ({msg.Chat.Username}) — НОВЫЙ");

                    rkm.Keyboard = new KeyboardButton[][]
                        {
                            new KeyboardButton[]
                                {
                                    new KeyboardButton("🏁 Начать игру")
                                }
                        };

                    await client.SendTextMessageAsync(msg.Chat.Id, @"Добро пожаловать!
Этот бот позволит Вам немного напрячь свой мозг несложной математикой.
Попробуй если не струсил...", replyMarkup: rkm);

                    break;

                case "🔄 Обновить пример":

                    await client.DeleteMessageAsync(msg.Chat.Id, msg.MessageId - 1);
                    await client.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);

                    anEquationAndAnswers.FormAnEquationAndAnswers(); //Создаётся новое уравнение и ответы

                    answers = anEquationAndAnswers.GetAnswers();
                    indexCorrectAnswer = anEquationAndAnswers.GetIndexCorrectAnswer();

                    ikb = new InlineKeyboardButton[3];

                    for (int i = 0; i < answers.Length; i++)
                    {
                        ikb[i] = InlineKeyboardButton.WithCallbackData(answers[i].ToString(), (i == indexCorrectAnswer) ? $"True|{answers[indexCorrectAnswer]}" : $"False|{answers[indexCorrectAnswer]}|{answers[i]}");
                    }

                    ikm = new InlineKeyboardMarkup(ikb);
                    await client.SendTextMessageAsync(msg.Chat.Id, anEquationAndAnswers.GetEquation(), replyMarkup: ikm);

                    break;

                case "🏁 Начать игру":

                    anEquationAndAnswers.FormAnEquationAndAnswers(); //Создаётся новое уравнение и ответы

                    answers = anEquationAndAnswers.GetAnswers();
                    indexCorrectAnswer = anEquationAndAnswers.GetIndexCorrectAnswer();

                    ikb = new InlineKeyboardButton[3];

                    for (int i = 0; i < answers.Length; i++)
                    {
                        ikb[i] = InlineKeyboardButton.WithCallbackData(answers[i].ToString(), (i == indexCorrectAnswer) ? $"True|{answers[indexCorrectAnswer]}" : $"False|{answers[indexCorrectAnswer]}|{answers[i]}");
                    }

                    ikm = new InlineKeyboardMarkup(ikb);

                    rkm.Keyboard = new KeyboardButton[][]
                    {
                        new KeyboardButton[]
                            {
                                new KeyboardButton("🔄 Обновить пример")
                            }
                    };
                    await client.SendTextMessageAsync(msg.Chat.Id, "Хорошей игры 🎲", replyMarkup: rkm);

                    await client.SendTextMessageAsync(msg.Chat.Id, anEquationAndAnswers.GetEquation(), replyMarkup: ikm);

                    break;

                default:
                    return;
            }
        }

        private static async void OnInlineButtonHandlet(object sender, CallbackQueryEventArgs e)
        {
            var msg = e.CallbackQuery.Message;
            msg.ReplyMarkup = null;

            bool properly = e.CallbackQuery.Data.Contains("True");

            string[] answersMsg = GetAnswersCorrectAndUncorrect(e.CallbackQuery.Data, properly);

            string correctAnswer = answersMsg[0];
            string answer = properly ? answersMsg[0] : answersMsg[1];

            string textMsg;

            if (properly)
            {
                textMsg = $@"✅ Правильно
Ваш ответ: {correctAnswer}";
            }
            else
            {
                textMsg = $@"❌ Неправильно
Ваш ответ: {answer}
Правильный ответ: {correctAnswer}";
            }

            await client.EditMessageTextAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId, $@"{msg.Text}
{textMsg}");

            anEquationAndAnswers.FormAnEquationAndAnswers(); //Создаётся новое уравнение и ответы

            string[] answers = anEquationAndAnswers.GetAnswers();
            int indexCorrectAnswer = anEquationAndAnswers.GetIndexCorrectAnswer();

            InlineKeyboardButton[] ikb = new InlineKeyboardButton[3];

            for (int i = 0; i < answers.Length; i++)
            {
                ikb[i] = InlineKeyboardButton.WithCallbackData(answers[i].ToString(), (i == indexCorrectAnswer) ? $"True|{answers[indexCorrectAnswer]}" : $"False|{answers[indexCorrectAnswer]}|{answers[i]}");
            }

            InlineKeyboardMarkup ikm = new InlineKeyboardMarkup(ikb);

            await client.SendTextMessageAsync(msg.Chat.Id, anEquationAndAnswers.GetEquation(), replyMarkup: ikm);
        }

        private static string[] GetAnswersCorrectAndUncorrect(string textMsg, bool properly)
        {
            char[] delimiterChars = {'|'};
            textMsg = textMsg.Replace(properly ? "True|" : "False|", "");
            
            string[] answers = textMsg.Split(delimiterChars);
            return answers;
        }
    }

    //Создание и получение уравнения и ответов
    //
    class AnEquationAndAnswers
    {
        private string[] answers; //ответы
        private string equation; //уравнение строкой
        private int indexCorrectAnswer; //индекс правильного ответа

        private static int difficultyLevel;

        public AnEquationAndAnswers() { difficultyLevel = 10; }
        public AnEquationAndAnswers(int a) { difficultyLevel = a; }

        public void FormAnEquationAndAnswers()
        {
            Random rnd = new Random();

            int number1 = rnd.Next(-difficultyLevel, difficultyLevel); //первое число
            int number2 = rnd.Next(-difficultyLevel, difficultyLevel); //второе число

            equation = number1.ToString() + ((number2 < 0) ? " - " : " + ") + Math.Abs(number2).ToString(); //уравнение строкой

            int correctAnswer = number1 + number2; //правильный ответ
            string correctAnswerStr = correctAnswer.ToString();

            indexCorrectAnswer = rnd.Next(0, 2);

            answers = new string[3];
            string newAnswer;
            bool repeat;

            for (int i = 0; i < 3; i++)
            {
                repeat = true;

                do
                {
                    newAnswer = rnd.Next(-difficultyLevel * 2, difficultyLevel * 2).ToString();

                    for (int a = 0; a < 3; a++)
                    {
                        if (answers[a] == newAnswer || correctAnswerStr == newAnswer)
                        {
                            repeat = true;
                            break;
                        }
                        else
                        {
                            repeat = false;
                        }
                    }

                }
                while (repeat);

                answers[i] = newAnswer;
            }

            answers[indexCorrectAnswer] = correctAnswer.ToString();
        }

        public string GetEquation()
        {
            return equation;
        }
        public int GetIndexCorrectAnswer()
        {
            return indexCorrectAnswer;
        }
        public string[] GetAnswers()
        {
            return answers;
        }
    }
}
