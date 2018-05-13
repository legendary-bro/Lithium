﻿using System;
using System.IO;
using Lithium.Services;
using Newtonsoft.Json;
using Serilog;

namespace Lithium.Models
{
    public class Config
    {
        [JsonIgnore] public static readonly string Appdir = AppContext.BaseDirectory;


        public static string ConfigPath = Path.Combine(AppContext.BaseDirectory, "setup/config.json");

        public string DefaultPrefix { get; set; } = "=";
        public string BotToken { get; set; } = "Token";
        public bool AutoRun { get; set; }

        public void Save(string dir = "setup/config.json")
        {
            var file = Path.Combine(Appdir, dir);
            File.WriteAllText(file, ToJson());
        }

        public static Config Load(string dir = "setup/config.json")
        {
            var file = Path.Combine(Appdir, dir);
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(file));
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static void CheckExistence()
        {
            bool auto;
            try
            {
                auto = Load().AutoRun;
            }
            catch
            {
                auto = false;
            }

            if (auto)
            {
            }
            else
            {
                Logger.LogInfo("Run (Y for run, N for setup Config)");

                Logger.LogInfo("Y or N: ");
                var res = Console.ReadKey();
                if (res.KeyChar == 'N' || res.KeyChar == 'n')
                    File.Delete("setup/config.json");

                if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
                    Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));
            }


            if (!File.Exists(ConfigPath))
            {
                var cfg = new Config();

                Logger.LogInfo(
                    @"Please enter a prefix for the bot eg. '+' (do not include the '' outside of the prefix)");
                Console.Write("Prefix: ");
                cfg.DefaultPrefix = Console.ReadLine();

                Log.Information(
                    @"After you input your token, a config will be generated at 'setup/config.json'");
                Console.Write("Token: ");
                cfg.BotToken = Console.ReadLine();

                Logger.LogInfo("Would you like to AutoRun the bot from now on? Y/N");
                var type2 = Console.ReadKey();
                if (type2.KeyChar == 'y' || type2.KeyChar == 'Y')
                    cfg.AutoRun = true;
                else
                    cfg.AutoRun = false;

                cfg.Save();
            }

            Logger.LogInfo("Config Loaded!");
            Logger.LogInfo($"Prefix: {Load().DefaultPrefix}");
            Logger.LogInfo($"Token Length: {Load().BotToken.Length} (should be 59)");
            Logger.LogInfo($"Autorun: {Load().AutoRun}");
        }
    }
}