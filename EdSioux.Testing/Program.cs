// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Testing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;

    using EdNetApi.Information;
    using EdNetApi.Journal;

    using Newtonsoft.Json;

    internal class Program
    {
        private static InformationManager informationManager;

        private static SiouxData siouxData;

        private static void Main(string[] _)
        {
            siouxData = JsonConvert.DeserializeObject<SiouxData>(File.ReadAllText(@"Resources\SiouxData.txt"));

            var appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolderPath = Path.Combine(appDataFolderPath, Assembly.GetEntryAssembly().GetName().Name);
            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }

            using (informationManager = new InformationManager(appFolderPath, allowAnonymousErrorFeedback: true))
            {
                // JournalEntryRead event will only trigger for new events, after all historical events have been read
                informationManager.JournalEntryRead += OnJournalEntryRead;

                // Start will read and cache all historical events and then return
                // If this is the first time then it might take a few minutes
                informationManager.Start();

                Console.WriteLine($"Hello Cmdr {informationManager.CurrentCommander?.Commander}!");
                Console.WriteLine($"Current ship:        {informationManager.CurrentShip?.Ship}");
                Console.WriteLine($"Current star system: {informationManager.CurrentLocation?.StarSystem}");
                Console.WriteLine();
                Console.WriteLine("Waiting for new entries to be written...");
                Console.WriteLine("Press ENTER at any time to exit");
                Console.ReadLine();

                informationManager.Stop();
            }
        }

        private static string GetFormattedString(JournalEntry journalEntry, bool filterOnCurrentCommander, SiouxEvent siouxEvent)
        {
            var journalEntryProperties = journalEntry.GetType().GetProperties().ToList();

            var currentData = new CurrentData
                                  {
                                      Ship = informationManager.CurrentShip.Ship.ToString(),
                                      StarSystem = informationManager.CurrentLocation.StarSystem,
                                      StationName = informationManager.CurrentLocation.StationName,
                                      Body = informationManager.CurrentLocation.Body,
                                      BodyType = informationManager.CurrentLocation.BodyType,
                                  };
            var currentDataProperties = currentData.GetType().GetProperties().ToList();

            var tokenRegex = new Regex(@"(?<token>\{(?<tokenName>[A-Za-z]*)\})", RegexOptions.Compiled);
            var tokens = tokenRegex.Matches(siouxEvent.Format).Cast<Match>().Distinct(new TokenMatchComparer())
                .ToDictionary(
                    match => match.Groups["tokenName"].Value.ToLowerInvariant(),
                    match => match.Groups["token"].Value);

            string count = null;
            if (tokens.Keys.Any(key => key == "count"))
            {
                count = GetStatisticsCount(
                    tokens.Keys.ToList(),
                    filterOnCurrentCommander,
                    journalEntry,
                    journalEntryProperties,
                    currentData,
                    currentDataProperties).ToString();
            }

            var text = siouxEvent.Format;
            foreach (var token in tokens)
            {
                if (token.Key == "count")
                {
                    text = Regex.Replace(text, token.Value, count ?? string.Empty, RegexOptions.IgnoreCase);
                    continue;
                }

                var journalProperty =
                    journalEntryProperties.FirstOrDefault(prop => prop.Name.Equals(token.Key, StringComparison.OrdinalIgnoreCase));
                if (journalProperty != null)
                {
                    var value = (string)journalProperty.GetValue(journalEntry);
                    text = Regex.Replace(text, token.Value, value ?? string.Empty, RegexOptions.IgnoreCase);
                    continue;
                }

                var currentDataProperty =
                    currentDataProperties.FirstOrDefault(prop => prop.Name.Equals(token.Key, StringComparison.OrdinalIgnoreCase));
                if (currentDataProperty != null)
                {
                    var value = (string)currentDataProperty.GetValue(currentData);
                    text = Regex.Replace(text, token.Value, value ?? string.Empty, RegexOptions.IgnoreCase);
                    continue;
                }

                text = Regex.Replace(text, token.Value, $"(no value found for {token.Key})", RegexOptions.IgnoreCase);
            }

            return text;
        }

        private class TokenMatchComparer : IEqualityComparer<Match>
        {
            public bool Equals(Match x, Match y)
            {
                return x.Groups["tokenName"].Value.Equals(
                    y.Groups["tokenName"].Value,
                    StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(Match obj)
            {
                return obj.GetHashCode();
            }
        }

        private static int GetStatisticsCount(
            List<string> tokenKeys,
            bool filterOnCurrentCommander,
            JournalEntry journalEntry,
            List<PropertyInfo> journalEntryProperties,
            CurrentData currentData,
            List<PropertyInfo> currentDataProperties)
        {
            var statisticsData = new StatisticsData
                                     {
                                         Commander =
                                             filterOnCurrentCommander
                                                 ? informationManager.CurrentCommander.Commander
                                                 : null,
                                         Event = journalEntry.Event,
                                     };
            var statisticsDataProperties = statisticsData.GetType().GetProperties().ToList();

            // Remove properties that are not allowed to be overridden
            statisticsDataProperties.RemoveWhere(prop => prop.Name == nameof(StatisticsData.Commander));
            statisticsDataProperties.RemoveWhere(prop => prop.Name == nameof(StatisticsData.Event));
            statisticsDataProperties.RemoveWhere(prop => prop.Name == nameof(StatisticsData.Count));

            foreach (var statisticsDataProperty in statisticsDataProperties)
            {
                if (!tokenKeys.Any(key => key.Equals(statisticsDataProperty.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var journalEntryProperty =
                    journalEntryProperties.FirstOrDefault(prop => prop.Name.Equals(statisticsDataProperty.Name));
                if (journalEntryProperty != null)
                {
                    statisticsDataProperty.SetValue(statisticsData, journalEntryProperty.GetValue(journalEntry));
                    continue;
                }

                var currentDataProperty =
                    currentDataProperties.FirstOrDefault(prop => prop.Name.Equals(statisticsDataProperty.Name));
                if (currentDataProperty != null)
                {
                    statisticsDataProperty.SetValue(statisticsData, currentDataProperty.GetValue(currentData));
                }
            }

            var count = informationManager.GetEventStatisticsSum(statisticsData);
            return count;
        }

        private static void OnJournalEntryRead(object sender, JournalEntryEventArgs eventArgs)
        {
            var journalEntry = eventArgs.JournalEntry;
            var siouxEvent = siouxData.Events.FirstOrDefault(e => e.Type == journalEntry.Event);
            if (siouxEvent == null)
            {
                return;
            }

            var text = GetFormattedString(journalEntry, siouxData.FilterOnCurrentCommander, siouxEvent);
            Console.WriteLine(text);
        }
    }
}