using System;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using Telegram.Bot;
using CsvHelper;
using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using System.Globalization;
using System.Formats.Asn1;
using System.Collections;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using System.Net.Http;
using static System.Net.WebRequestMethods;
//using MovieRecommendation;

namespace tg_bot_rec
{
    class Prog
    {
        static async Task Main()
        {
            var token = "Your token for tg_bot";
            Bot_of_rec client = new Bot_of_rec(token);
            Console.ReadLine();
        }
    }
}
