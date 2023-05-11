using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot;
using System.Threading;
using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.ML;
using System.Reflection;

namespace tg_bot_rec
{
    internal class Bot_of_rec
    {
        public TelegramBotClient client;
        private string cancellationToken;
        static private List<string> listID = new List<string>();
        static private List<string> listFilm = new List<string>();
        static private string wayToFileCsv = "C:\\Users\\м\\OneDrive\\Рабочий стол\\Универ\\2 курс\\2 семестр\\Practica\\tg_bot_rec\\tg_bot_rec\\data\\ratings.csv";
        static private int count = 0;
        static private List<MovieRating> wr = new List<MovieRating>();
        static private InlineKeyboardMarkup replyKeyboardMarkup;
        static private Machine pr;


        public Bot_of_rec(string token)
        {
            pr = new Machine();
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            client = new TelegramBotClient(token);
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // разрешено получать все виды апдейтов
            };
            client.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );


            var reader = new StreamReader(@"C:\Users\м\OneDrive\Рабочий стол\Универ\2 курс\2 семестр\Practica\tg_bot_rec\tg_bot_rec\data\movies.csv");
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var value = line.Split(',');
                if (value.Length <= 3)
                {
                    listID.Add(value[0]);
                    listFilm.Add(value[1]);
                }
                else
                {
                    int i = 1;
                    string sup = "";
                    while (i == value.Length-2)
                    {
                        sup += value[i];
                        i++;
                    }
                    listID.Add(value[0]);
                    listFilm.Add(sup);
                }
            }


            replyKeyboardMarkup =
            new(new[]
            {
                     new InlineKeyboardButton[] { "Начать рекомендацию!" },
            });
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == UpdateType.Message && update.Message.Sticker == null)
            {
                var message = update.Message.Text;
                if (message.ToLower() == "/start" )
                {
                    count = 0;
                    await botClient.SendTextMessageAsync(
                        chatId: update.Message.Chat,
                        text: "Привет, " + update.Message.From.FirstName + ". Я твой телеграмм бот по рекомендации фильмов." +
                        "Поскольку мы только начинаем наше общение, напиши мне фильмы, которые ты смотрел и оцени их по 5-ти балльной шкале ");

                    return;
                }
                if (!update.Message.Text.StartsWith("/"))
                {
                    string str = message.Split('-')[0];
                    if (listFilm.Contains(str) && message.Split('-').Length >=2)
                    {
                        int score = Convert.ToInt32(message.Split('-')[1]);
                        var tagged = listFilm.Select((item, i) => new { Item = item, Index = i });
                        int index = (from pair in tagged
                                     where pair.Item == str
                                     select pair.Index).First();
                        count++;
                        wr.Add(new MovieRating
                        {
                            userId = update.Message.From.Id,
                            movieId = Convert.ToInt64(listID[index]),
                            Label = score
                        });

                        if (count < 4)
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: update.Message.Chat,
                                text: "Отлично, " + update.Message.From.FirstName + " осталось ввести " + (4 - count) + " фильма");
                        }
                        else
                        {
                            bool append = true;
                            var config = new CsvConfiguration(CultureInfo.InvariantCulture);
                            config.HasHeaderRecord = false;

                            StreamWriter streamReader = new StreamWriter(wayToFileCsv, append);
                            CsvWriter csvWriter = new CsvWriter(streamReader, config);
                            csvWriter.WriteRecords(wr);
                            streamReader.Close();


                            await botClient.SendTextMessageAsync(
                                chatId: update.Message.Chat,
                                text: "Отлично, " + update.Message.From.FirstName + ". Теперь можем переходить к рекомендации нажми на кнопку, чтобы начать.",
                                replyMarkup: replyKeyboardMarkup);
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: update.Message.Chat,
                            text: "Извини, но я не знаю такого фильма"
                            );
                    }

                }
            }
            if(update.Type == UpdateType.CallbackQuery)
            {
                if (update.CallbackQuery.Data == "Начать рекомендацию!")
                {
                    List<int> check_value = new List<int>();
                    bool check = true;
                    while (check)
                    {
                        Random rnd = new Random();
                        var idRndValue = rnd.Next(1, 9743);

                        string s = listFilm[idRndValue];
                        var tag = listFilm.Select((item, i) => new { Item = item, Index = i });
                        int idx = (from pair in tag
                                   where pair.Item == s
                                   select pair.Index).First();

                        if (pr.UseModelForSinglePrediction(pr.mlContext, pr.model, update.CallbackQuery.From.Id, Convert.ToInt32(listID[idx])) && pr.model != null && pr.mlContext != null && !(check_value.Contains(idRndValue)))
                        {
                            check_value.Add(idRndValue);
                            await botClient.SendTextMessageAsync(
                                chatId: update.CallbackQuery.Message.Chat,
                                text: update.CallbackQuery.From.FirstName + ", вот твой фильм." + s,
                                replyMarkup: replyKeyboardMarkup
                                );
                            check = false;
                        }
                    }

                }
            }
        }
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Данный Хендлер получает ошибки и выводит их в консоль в виде JSON
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }
    }
}
