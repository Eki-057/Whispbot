using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Whispbot.Databases;
using YellowMacaroni.Discord.Core;
using YellowMacaroni.Discord.Extentions;

namespace Whispbot
{
    public static partial class Tools
    {
        public static string Process(this string content)
        {
            return Strings.Process(content);
        }

        public static class Strings
        {
            public static Dictionary<string, Emoji> Emojis = [];
            public static Dictionary<Language, Dictionary<string, string>> LanguageStrings = [];

            private static HttpClient _client = new();

            public static string Process(string content, Language language = 0)
            {
                MatchCollection matches = Regex.Matches(content, @"\{([^}]+)\}");
                Dictionary<string, string>? thisLanguage = LanguageStrings.GetValueOrDefault(language);

                foreach (Match match in matches)
                {
                    string key = match.Groups[1].Value.ToLower();
                    if (key.StartsWith("emoji."))
                    {
                        string emojiName = key.Replace("emoji.", "");
                        Emoji? emoji = Emojis.GetValueOrDefault(emojiName);
                        if (emoji is not null)
                        {
                            content = content.Replace(match.Value, emoji.ToString());
                        }
                    } 
                    else if (key == "dt")
                    {
                        double s = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        content = content.Replace(match.Value, $"<t:{s}:d> <t:{s}:T>");
                    }
                    else if (key.StartsWith("string."))
                    {
                        if (thisLanguage is not null)
                        {
                            string? value = thisLanguage.GetValueOrDefault(key.Replace("string.", ""));
                            if (value is not null)
                            {
                                content = content.Replace(match.Value, value);
                            }
                        }
                    }
                }

                return content;
            }

            public static async Task GetEmojis()
            {
                string? token = Environment.GetEnvironmentVariable("RESOURCE_TOKEN");
                if (token is null) return;

                string? clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
                if (clientId is null) return;

                try
                {
                    _client.DefaultRequestHeaders.Add("Authorization", $"Bot {token}");

                    HttpResponseMessage result = await _client.GetAsync($"https://discord.com/api/v10/applications/{clientId}/emojis");
                    if (result.IsSuccessStatusCode)
                    {
                        EmojisResponse? data = JsonConvert.DeserializeObject<EmojisResponse>(await result.Content.ReadAsStringAsync());
                        Dictionary<string, Emoji>? emojis = data?.items.ToDictionary(e => e.name?.ToLower() ?? "", e => e);
                        if (emojis is null) return;
                        Emojis = emojis;
                    }
                }
                catch { }
            }

            public static void GetLanguages()
            {
                while (!Postgres.IsConnected()) Thread.Sleep(100);
                List<DBLanguage>? languages = Postgres.Select<DBLanguage>($"SELECT * FROM languages");
                if (languages is null || languages.Count == 0) return;
                LanguageStrings = languages.GroupBy(l => l.language).ToDictionary(g => g.Key, g => g.ToDictionary(l => l.key, l => l.content));
            }

            public class DBLanguage
            {
                public string key = "";
                public Language language;
                public string content = "";
            }

            public struct EmojisResponse
            {
                public List<Emoji> items;
            }

            public static Dictionary<Language, (string, string, string)> Languages = new()
            {
                { Language.EnglishUK, ("en-GB", "English, UK", "English UK") },
                { Language.EnglishUS, ("en-US", "English, US", "English US") },
                { Language.French, ("fr-FR", "French", "Français") },
                { Language.German, ("de", "German", "Deutsch") },
                { Language.Spanish, ("es-ES", "Spanish", "Español") },
                { Language.SpanishLatinAmerican, ("es-419", "Spanish, LATAM", "Español, LATAM") },
                { Language.Italian, ("it", "Italian", "Italiano") },
                { Language.Thai, ("th", "Thai", "ไทย") },
                { Language.Dutch, ("nl", "Dutch", "Nederlands") },
                { Language.Polish, ("pl", "Polish", "Polski") },
                { Language.Indonesian, ("id", "Indonesian", "Bahasa Indonesia") },
                { Language.Danish, ("da", "Danish", "Dansk") },
                { Language.Croatian, ("hr", "Croatian", "Hrvatski") },
                { Language.Lithuanian, ("lt", "Lithuanian", "Lietuviškai") },
                { Language.Hungarian, ("hu", "Hungarian", "Magyar") },
                { Language.Norwegian, ("no", "Norwegian", "Norsk") },
                { Language.PortugueseBrazilian, ("pt-BR", "Portuguese, Brazilian", "Português do Brasil") },
                { Language.RomanianRomania, ("ro", "Romanian, Romania", "Română") },
                { Language.Finish, ("fi", "Finish", "Suomi") },
                { Language.Swedish, ("sv-SE", "Swedish", "Svenska") },
                { Language.Vietnamese, ("vi", "Vietnamese", "Tiếng Việt") },
                { Language.Turkish, ("tr", "Turkish", "Türkçe") },
                { Language.Czech, ("cs", "Czech", "Čeština") },
                { Language.Greek, ("el", "Greek", "Ελληνικά") },
                { Language.Bulgarian, ("bg", "Bulgarian", "български") },
                { Language.Russian, ("ru", "Russian", "Pусский") },
                { Language.Ukrainian, ("uk", "Ukrainian", "Українська") },
                { Language.Hindi, ("hi", "Hindi", "हिन्दी") },
                { Language.ChineseChina, ("zh-CN", "Chinese, China", "中文") },
                { Language.ChineseTaiwan, ("zh-TW", "Chinese, Taiwan", "繁體中文") },
                { Language.Japanese, ("ja", "Japanese", "日本語") },
                { Language.Korean, ("ko", "Korean", "한국어") },
            };

            public enum Language
            {
                EnglishUK,
                EnglishUS,
                French,
                German,
                Spanish,
                SpanishLatinAmerican,
                Italian,
                Thai,
                Dutch,
                Polish,
                Indonesian,
                Danish,
                Croatian,
                Lithuanian,
                Hungarian,
                Norwegian,
                PortugueseBrazilian,
                RomanianRomania,
                Finish,
                Swedish,
                Vietnamese,
                Turkish,
                Czech,
                Greek,
                Bulgarian,
                Russian,
                Ukrainian,
                Hindi,
                ChineseChina,
                ChineseTaiwan,
                Japanese,
                Korean
            }
        }
    }
}
