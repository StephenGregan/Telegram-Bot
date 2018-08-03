using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot
{
#pragma warning disable 4014
#pragma warning disable RECS022
    class Bot
    {
        static void Main()
        {
            while (true)
            {
                try
                {
                    MainLoop().Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Main loop exit error - ", ex);
                    Thread.Sleep(30000);
                }
            }
        }

        static async Task MainLoop()
        {
            var telegramKey = ConfigurationManager.AppSettings["TelegramKey"];

            var bot = new TelegramBotClient(telegramKey);
            var me = await bot.GetMeAsync();
            Console.WriteLine(me.Username, + " started at " + DateTime.Now);

            var offset = 0;
            while (true)
            {
                var updates = new Update[0];
                try
                {
                    updates = await bot.GetUpdatesAsync(offset);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine(ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while getting updates - " + ex);
                }
                foreach (var update in updates)
                {
                    offset = update.Id + 1;
                    ProcessUpdate();
                }
                await Task.Delay(1000);
            }
        }

        static async void ProcessUpdate(ITelegramBotClient bot, Update update, User me)
        {
            var wunderGroundKey = ConfigurationManager.AppSettings["WundergroundKey"];
            var bingKey = ConfigurationManager.AppSettings["BingKey"];
            var wolfframAppID = ConfigurationManager.AppSettings["WoldframAppId"];

            try
            {
                var httpClient = new ProHttpClient();
                var text = update.Message.Text;
                var replyText = string.Empty;
                var replytextMarkdown = string.Empty;
                var replyImage = string.Empty;
                var replyImageCaption = string.Empty;
                var replyDocument = string.Empty;

                if (text != null && (text.StartsWith("/", StringComparison.Ordinal) || text.StartsWith("!", StringComparison.Ordinal)))
                {
                    Console.WriteLine(update.Message.Chat.Id + " < " + update.Message.From.Username + " - " + text);

                    if (text.StartsWith("!", StringComparison.Ordinal))
                    {
                        text = "/" + text.Substring(1);
                    }

                    text = text.Replace("@" + me.Username, "");

                    string command;
                    string body;
                    if (text.StartsWith("/s/", StringComparison.Ordinal))
                    {
                        command = "/s";
                        body = text.Substring(2);
                    }
                    else
                    {
                        command = text.Split(' ')[0];
                        body = text.Replace(command, "").Trim();
                    }
                    var stringBuilder = new StringBuilder();

                    switch (command.ToLowerInvariant())
                    {
                        case "/beer":
                            if (body == string.Empty)
                            {
                                replyText = "Usage: /beer <Name of beer>";
                                break;
                            }
                            await bot.SendChatActionAsync(update.Message.Chat.Id, Telegram.Bot.Types.Enums.ChatAction.Typing);
                            var beerSearch = httpClient.DownloadString("http://beeradvocate.com/search/?q=" + HttpUtility.UrlEncode(body) + "&qt=beer").Result.Replace("\r", "").Replace("\n", "");

                            var firstBeer = Regex.Match(beerSearch, @"<div id=""ba-content"">.*?<ul>.*?<li>.*?<a href=""(.*?)"">").Groups[1].Value.Trim();
                            if (firstBeer == string.Empty)
                            {
                                replyText = "Could not find a matching beer " + body;
                                break;
                            }
                            var beer = httpClient.DownloadString("http://beeradvocate.com" + firstBeer).Result.Replace("\r", "").Replace("\n", "");
                            var beerName = Regex.Match(beer, @"<title>(.*?)</title>").Groups[1].Value.Replace(" | BeerAdvocate", string.Empty).Trim();
                            beer = Regex.Match(beer, @"<div id=""ba-content"">.*?<div>(.*?)<div style=""clear:both;"">").Groups[1].Value.Trim();
                            replyImage = Regex.Match(beer, @"img src=""(.*?)""").Groups[1].Value.Trim();
                            replyImageCaption = "http://beeradvocate.com" + firstBeer;
                            var beerScore = Regex.Match(beer, @"<span class=""BAscore_big ba-score"">(.*?)</span>").Groups[1].Value.Trim();
                            var beerScoreText = Regex.Match(beer, @"<span class=""ba-score_text>(.*?)</span>").Groups[1].Value.Trim();
                            var beerBroScore = Regex.Match(beer, @"<span class=""BAscore_big ba-bro_score"">(>*?)</span>").Groups[1].Value.Trim();
                            var beerBroScoreText = Regex.Match(beer, @"<b class=""ba-bro_text"">(>*?)</b>").Groups[1].Value.Trim();
                            var beerHads = Regex.Match(beer, @"<span class=""ba-ratings"">(.*?)</span>").Groups[1].Value.Trim();
                            var beerAvg = Regex.Match(beer, @"<span class=""ba-ravg>(>*?)</span>").Groups[1].Value.Trim();
                            var beerStyle = Regex.Match(beer, @"<b>Style:</b>.*?<b>(.*?)</b>").Groups[1].Value.Trim();
                            var beerAbv = beer.Substring(beer.IndexOf("(ABV):", StringComparison.Ordinal) + 10, 7).Trim();
                            var beerDexcription = Regex.Match(beer, @"<b>Notes / Commercial Description:</b>(>*?)</div>").Groups[1].Value.Replace("|", "").Trim();
                            stringBuilder.Append(beerName.Replace("|", "- " + beerStyle + " by") + "\r\nscore: " + beerScore + " (" + beerScoreText + ") | bros: " +
                                beerBroScore + " (" + beerBroScoreText + ") | Avg: " + beerAvg + " (" + beerHads + " hads)\r\nABV: " + beerAbv + " | ");
                            stringBuilder.Append(HttpUtility.HtmlDecode(beerDexcription).Replace("<br>", " ").trim());
                            break;

                        case "/cat":
                            replyImage = "http://thecatapi.com/appi/images/get?format=src&type=jpg,png";
                            break;
                        case "/doge":
                            replyImage = "http://dogr.io/wow/" + body.Replace(",", "/").Replace(" ", "") + ".png";
                            break;
                        case "/echo":
                            replyText = body;
                            break;
                        case "/fat":
                            if (body == string.Empty)
                            {
                                replyText = "Usage: /fat <Name of food>";
                                break;
                            }
                            await bot.SendChatActionAsync(update.Message.Chat.Id, Telegram.Bot.Types.Enums.ChatAction.Typing);
                            var search = httpClient.DownloadString("http://calorieking.com/foods/search.php?keywords=" + body).Result.Replace("\r", "").Replace("\n", "");

                            var firstUrl = Regex.Match(search, @"<a class=""food-search-result-name"" href=""([\w:\/\-\._]*)""").Groups[1].Value.Trim();
                            if (firstUrl == string.Empty)
                            {
                                replyText = "Unable to find a food matching name: " + body;
                                break;
                            }
                            var food = httpClient.DownloadString(firstUrl).Result.Replace("\r", "").Replace("\n", "per ");


                            var label = string.Empty;
                            var protein = 0.0;
                            var carbs = 0.0;
                            var fat = 0.0;
                            var fiber = 0.0;
                            stringBuilder.Append(Regex.Match(food, @"<title>(.*?)\ \|.*</title>").Groups[1].Value.Replace("Calories in ", "").Trim() + " ");
                            stringBuilder.Append(Regex.Match(food, @"<select name=""units"">*?>(.*?)<\/option>", RegexOptions.IgnoreCase).Groups[1].Value.Trim() + "\r\n");
                            foreach (Match fact in Regex.Matches(food, @"<td class=""(calories|label\amount)"">([a-zA-Z0-9\ &;<>=\/\""\.]*)<\/td>"))
                            {
                                switch (fact.Groups[1].Value.Trim().ToLowerInvariant())
                                {
                                    case "calories":
                                        stringBuilder.Append("Calories: " + fact.Groups[2].Value.Replace("Calories&nbsp;<span class=\"amount\">", "").Replace("</span>", "") + ", ");
                                        break;
                                    case "label":
                                        label = fact.Groups[2].Value.Trim();
                                        break;
                                    case "amount":
                                        stringBuilder.Append(label + ": " + fact.Groups[2].Value + ", ");
                                        switch (label.ToLowerInvariant())
                                        {
                                            case "protein":
                                                protein = Convert.ToDouble(fact.Groups[2].Value.Replace("mg", "").Replace("g", "").Replace("&lt;", "").Replace("&gt;", ""));
                                                break;
                                            case "total carbs":
                                                carbs = Convert.ToDouble(fact.Groups[2].Value.Replace("mg", "").Replace("g", "").Replace("&lt;", "").Replace("&gt;", ""));
                                                break;
                                            case "total fat":
                                                fat = Convert.ToDouble(fact.Groups[2].Value.Replace("mg", "").Replace("g", "").Replace("&lt;", "").Replace("&gt;", ""));
                                                break;
                                            case "dietary fiber":
                                                fiber = Convert.ToDouble(fact.Groups[2].Value.Replace("mg", "").Replace("g", "").Replace("&lt;", "").Replace("&gt;", ""));
                                                break;
                                        }
                                        break;
                                }
                            }
                            stringBuilder.Append("WW pointsPlus: " + Math.Round((protein / 10.9375) + (carbs / 9.2105) + (fat / 3.8889) - (fiber / 12.5), 1));
                            break;

                        case "forecast":
                            if (body.Length < 2)
                            {
                                body = "Cincinnati, OH";
                            }
                            await bot.SendChatActionAsync(update.Message.Chat.Id, ChatAction.Typing);
                            dynamic dfor = JObject.Parse(httpClient.DownloadString("https://api.wunderground.com/api/" + wunderGroundKey + "/forcast/q/" + body + ".json").Result);
                            if (dfor.forecast == null || dfor.forecast.txt_forecast == null)
                            {
                                replyText = "Trixie is disappointed " + body + "";
                                break;
                            }
                            for (var ifor = 0; ifor < Enumerable.Count(dfor.forecast.txt_forecast.forecastday) - 1; ifor++)
                            {
                                stringBuilder.AppendLine(dfor.forecast.txt_forecast.forecastday[ifor].title.ToString() + ": " + dfor.forecast.txt_forecast.forecastday[ifor].fcttext.ToString());
                            }
                            break;

                        case "/help":
                            replyText = "";
                            break;
                        case "/image":
                        case "/img":
                            if (body == string.Empty)
                            {
                                replyText = "Usage/ image <Description of image to find>";
                                break;
                            }
                            await bot.SendChatActionAsync(update.Message.Chat.Id, ChatAction.Typing);
                            httpClient.AutherizationHeader = "Basic " + bingKey;
                            dynamic dimg = JObject.Parse(httpClient.DownloadString("https://api.datamarket.azure.com/Data.ashx/Bing/Search/Image?Market=%27en-US%27&Adult=%27Moderate%27&Query=%27" + HttpUtility.UrlEncode(body) + "%27&$format=json&$top=3").Result);
                            httpClient.AutherizationHeader = string.Empty;
                            if (dimg.d == null || dimg.d.results == null || Enumerable.Count(dimg.d.results) < 1)
                            {
                                replyText = "";
                                break;
                            }
                            break;
                    }
                }
            }
            catch (System.Net.WebException ex)
            {
                Console.WriteLine("Unable to download " + ex.HResult + " " + ex.Message);
                await bot.SendTextMessageAsync(update.Message.Chat.Id, "Please try again later");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error - ", ex);
            }
        }

        static async void DelayedMessage(ITelegramBotClient bot, long chatId, string message, int minutesToWait)
        {
            await Task.Delay(minutesToWait * 60000);
            await bot.SendTextMessageAsync(chatId, message);
        }
    }
}
