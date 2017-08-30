// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiouxManager.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Managers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Windows.Media;

    using EdNetApi.Common;
    using EdNetApi.Information;
    using EdNetApi.Journal;

    using EdSioux.Common;
    using EdSioux.Models;

    using JetBrains.Annotations;

    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;
    using Newtonsoft.Json.Schema.Generation;

    using Squirrel;

    internal class SiouxManager
    {
        private const string CountValue = "count";
        private const string OrdinalCountValue = "ordinalcount";

        private readonly SiouxData _siouxData;
        private readonly List<string> _gameStatisticsNames;
        private readonly Timer _onTheHourTimer;
        private readonly string _appFolderPath;

        private InformationManager _informationManager;

        public SiouxManager()
        {
            GenerateSiouxDataTokensFile();

            var appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _appFolderPath = Path.Combine(appDataFolderPath, Assembly.GetEntryAssembly().GetName().Name);
            if (!Directory.Exists(_appFolderPath))
            {
                Directory.CreateDirectory(_appFolderPath);
            }

            List<string> errorMessages;
            if (!LoadSiouxData(out _siouxData, out errorMessages))
            {
                errorMessages.Insert(0, $"Failed to load SiouxData at {_appFolderPath}");
                throw new ApplicationException(string.Join(Environment.NewLine, errorMessages));
            }

            _gameStatisticsNames = typeof(GameStatistics).GetProperties().Select(prop => prop.Name.ToLowerInvariant())
                .ToList();

            _onTheHourTimer = new Timer(
                state =>
                    {
                        var siouxEvent = _siouxData.Events.FirstOrDefault(e => e.Type == JournalEventType.GamePlayed);
                        RaiseSiouxEventReceived(siouxEvent, "SIOUX Update", null);
                    },
                null,
                Timeout.Infinite,
                Timeout.Infinite);

            using (var updateManager = UpdateManager.GitHubUpdateManager("https://github.com/mbedatpro/edsioux"))
            {
                updateManager.Result.UpdateApp().Wait();
            }
        }

        public event EventHandler<SiouxEventArgs> SiouxEventReceived;

        public void Start()
        {
            Stop();

            _informationManager = new InformationManager(_appFolderPath, true);
            _informationManager.JournalEntryRead += OnJournalEntryRead;
            _informationManager.JournalEntryException += OnJournalEntryException;

            // Start will read and cache all historical events and then return
            // If this is the first time then it might take a few minutes
            _informationManager.Start();

            const string Text =
                "Hello Cmdr {commander:Name}!\nCurrent ship: {ship}\nCurrent star system: {starSystem}\nSessions played: {SessionsPlayed}\nTime played: {TotalTimePlayed}";
            var inlines = GetMessageParts(null, false, Text);
            SiouxEventReceived.Raise(this, new SiouxEventArgs("SIOUX Online", inlines, 15));

            var msToNextHour = (((60 - DateTime.UtcNow.Minute) * 60) - DateTime.UtcNow.Second) * 1000;
            _onTheHourTimer.Change(msToNextHour, 60 * 60 * 1000);
        }

        public void Stop()
        {
            _onTheHourTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if (_informationManager == null)
            {
                return;
            }

            _informationManager.JournalEntryRead -= OnJournalEntryRead;
            _informationManager.Stop();
            _informationManager.Dispose();
            _informationManager = null;
        }

        private static string GetOrdinalIndicator(int value)
        {
            var digit = value % 10;
            switch (digit)
            {
                case 1:
                    return "st";
                case 2:
                    return "nd";
                case 3:
                    return "rd";
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    return "th";

                ////case 0:
                default:
                    return string.Empty;
            }
        }

        private static string GetPropertyValue(object source, PropertyInfo property)
        {
            if (property == null || source == null)
            {
                return string.Empty;
            }

            if (!property.PropertyType.IsEnum)
            {
                return property.GetValue(source).ToString();
            }

            var enumValue = (Enum)property.GetValue(source);
            var description = enumValue.Description();
            return !string.IsNullOrEmpty(description) ? description : enumValue.ToString();
        }

        private bool LoadSiouxData(out SiouxData siouxData, out List<string> errorMessages)
        {
            const string Filename = "SiouxData.txt";
            var filePath = Path.Combine(_appFolderPath, Filename);

            if (!File.Exists(filePath))
            {
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("EdSioux.Resources.SiouxData.txt"))
                {
                    if (resource != null)
                    {
                        using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            resource.CopyTo(file);
                        }
                    }
                }
            }

            JObject jObject;
            try
            {
                var json = File.ReadAllText(filePath);
                jObject = JObject.Parse(json);
            }
            catch (Exception exception)
            {
                siouxData = null;
                errorMessages = new List<string> { exception.GetBaseException().Message };
                return false;
            }

            var schemaGenerator = new JSchemaGenerator();
            schemaGenerator.GenerationProviders.Add(new StringEnumGenerationProvider());
            var schema = schemaGenerator.Generate(typeof(SiouxData), false);

            IList<string> schemaErrorMessages;
            if (!jObject.IsValid(schema, out schemaErrorMessages))
            {
                siouxData = null;
                errorMessages = schemaErrorMessages.ToList();
                return false;
            }

            siouxData = jObject.ToObject<SiouxData>();
            errorMessages = null;
            return true;
        }

        private void GenerateSiouxDataTokensFile()
        {
            var builder = new StringBuilder();
            builder.AppendLine("-------------------------------");
            builder.AppendLine("Tokens available for all events");
            builder.AppendLine("-------------------------------");
            builder.AppendLine("Count        (example output: 1, 2, 3, etc.)");
            builder.AppendLine("OrdinalCount (example output: 1st, 2nd, 3rd, etc.)");
            typeof(CurrentData).GetProperties().Select(prop => prop.Name).ToList()
                .ForEach(line => builder.AppendLine(line));
            builder.AppendLine();

            builder.AppendLine("---------------------");
            builder.AppendLine("Event specific tokens");
            builder.AppendLine("---------------------");

            var journalTypes = typeof(JournalEntry).Assembly.GetTypes()
                .Where(type => type.IsClass && type.Namespace == "EdNetApi.Journal.JournalEntries").ToList();
            foreach (var journalType in journalTypes)
            {
                if (journalType.BaseType?.Name != nameof(JournalEntry))
                {
                    continue;
                }

                var eventType = (JournalEventType)journalType.GetField("EventConst").GetValue(null);
                builder.AppendLine($"{eventType} - {eventType.Description()}");

                var properties = journalType.GetProperties(
                        BindingFlags.DeclaredOnly | BindingFlags.GetProperty | BindingFlags.Instance
                        | BindingFlags.Public)
                    .ToList();
                properties.Remove(properties.Single(prop => prop.Name == nameof(JournalEntry.Event)));
                properties.Remove(properties.Single(prop => prop.Name == nameof(JournalEntry.Timestamp)));
                foreach (var property in properties)
                {
                    var descriptionAttribute = (DescriptionAttribute)property
                        .GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
                    var description = descriptionAttribute?.Description.Trim();
                    description = !string.IsNullOrEmpty(description) ? " - " + description : string.Empty;
                    builder.AppendLine($"  {property.Name}{description}");
                }
            }

            var siouxDataTokens = builder.ToString();
            File.WriteAllText("SiouxDataTokens.txt", siouxDataTokens);
        }

        private Brush GetBrushFromToken(Match token)
        {
            if (token.Groups["colorType"].Success)
            {
                ColorType colorType;
                if (Enum.TryParse(token.Groups["colorType"].Value, true, out colorType))
                {
                    return ColorManager.Brushes[colorType];
                }
            }

            return ColorManager.Brushes[_siouxData.DefaultTokenColorType];
        }

        private IEnumerable<SiouxMessagePart> GetMessageParts(
            [CanBeNull] JournalEntry journalEntry,
            bool filterOnCurrentCommander,
            string format)
        {
            var currentData = new CurrentData
            {
                Commander = _informationManager.CurrentCommander.Commander,
                Ship = _informationManager.CurrentShip.Ship,
                StarSystem = _informationManager.CurrentLocation.StarSystem,
                StationName = _informationManager.CurrentLocation.StationName,
                Body = _informationManager.CurrentLocation.Body,
                BodyType = _informationManager.CurrentLocation.BodyType
            };
            var currentDataProperties = currentData.GetType().GetProperties().ToList();

            var tokenRegex = new Regex(
                @"(?<token>\{(?<tokenName>[A-Za-z]*)(?<colorToken>:(?<colorType>[A-Za-z]*))?\})",
                RegexOptions.Compiled);
            var tokens = tokenRegex.Matches(format).Cast<Match>().Distinct(new TokenMatchComparer()).ToList();
            var tokenNames = tokens.Select(token => token.Groups["tokenName"].Value.ToLowerInvariant()).ToList();

            var journalEntryProperties = journalEntry?.GetType().GetProperties().ToList();

            var count = -1;
            if (tokenNames.Any(tokenName => tokenName.Equals(CountValue) || tokenName.Equals(OrdinalCountValue)))
            {
                count = GetStatisticsCount(
                    tokenNames,
                    filterOnCurrentCommander,
                    journalEntry,
                    journalEntryProperties,
                    currentData,
                    currentDataProperties);
            }

            var defaultBrush = ColorManager.Brushes[_siouxData.DefaultTextColorType];

            GameStatistics gameStatistics = null;
            var messageParts = new List<SiouxMessagePart>();
            var index = 0;
            foreach (var token in tokens)
            {
                var tokenName = token.Groups["tokenName"].Value.ToLowerInvariant();
                var brush = GetBrushFromToken(token);

                if (token.Index > index)
                {
                    messageParts.Add(
                        new SiouxMessagePart
                        {
                            Text = format.Substring(index, token.Index - index),
                            Foreground = defaultBrush
                        });
                }

                string value = null;

                //// Alternative solution if the ordinal indicator should not be colored
                ////
                ////if (tokenName.Equals(CountValue) || tokenName.Equals(OrdinalCountValue))
                ////{
                ////    value = count >= 0 ? count.ToString() : string.Empty;
                ////    messageParts.Add(new SiouxMessagePart { Text = value, Foreground = Brushes.ForestGreen });
                ////    if (tokenName.Equals(OrdinalCountValue))
                ////    {
                ////        messageParts.Add(new SiouxMessagePart { Text = GetOrdinalIndicator(count) });
                ////    }
                ////    index = token.Index + token.Length;
                ////    continue;
                ////}
                if (tokenName.Equals(CountValue) || tokenName.Equals(OrdinalCountValue))
                {
                    var countString = string.Empty;
                    if (count >= 0)
                    {
                        countString = tokenName.Equals(CountValue)
                                          ? count.ToString()
                                          : $"{count}{GetOrdinalIndicator(count)}";
                    }

                    messageParts.Add(new SiouxMessagePart { Text = countString, Foreground = brush });
                    index = token.Index + token.Length;
                    continue;
                }

                if (_gameStatisticsNames.Contains(tokenName))
                {
                    gameStatistics = gameStatistics ?? _informationManager.GetGamePlayedStatistics();

                    string text;
                    switch (tokenName)
                    {
                        case "sessionsplayed":
                            text = gameStatistics.SessionsPlayed.ToString();
                            break;
                        case "totaltimeplayed":
                            text =
                                $"{gameStatistics.TotalTimePlayed.Days} days {gameStatistics.TotalTimePlayed.Hours} hours {gameStatistics.TotalTimePlayed.Minutes} minutes";
                            break;
                        case "currentsessionplayed":
                            text =
                                $"{gameStatistics.CurrentSessionPlayed?.Days} days {gameStatistics.CurrentSessionPlayed?.Hours} hours {gameStatistics.CurrentSessionPlayed?.Minutes} minutes";
                            break;
                        default:
                            // TODO: Handle by sending feedback
                            text = "ERROR";
                            break;
                    }

                    messageParts.Add(new SiouxMessagePart { Text = text, Foreground = brush });
                    index = token.Index + token.Length;
                    continue;
                }

                if (journalEntryProperties != null)
                {
                    var journalProperty = journalEntryProperties.FirstOrDefault(
                        prop => prop.Name.Equals(tokenName, StringComparison.OrdinalIgnoreCase));
                    value = GetPropertyValue(journalEntry, journalProperty);
                    if (!string.IsNullOrEmpty(value))
                    {
                        messageParts.Add(new SiouxMessagePart { Text = value, Foreground = brush });
                        index = token.Index + token.Length;
                        continue;
                    }
                }

                var currentDataProperty =
                    currentDataProperties.FirstOrDefault(
                        prop => prop.Name.Equals(tokenName, StringComparison.OrdinalIgnoreCase));
                if (currentDataProperty != null)
                {
                    value = GetPropertyValue(currentData, currentDataProperty);
                    messageParts.Add(new SiouxMessagePart { Text = value, Foreground = brush });
                    index = token.Index + token.Length;
                    continue;
                }

                if (value == null)
                {
                    messageParts.Add(
                        new SiouxMessagePart
                        {
                            Text = $"(no value found for {tokenName})",
                            Foreground = Brushes.OrangeRed
                        });
                }

                index = token.Index + token.Length;
            }

            if (index < format.Length)
            {
                messageParts.Add(new SiouxMessagePart { Text = format.Substring(index), Foreground = defaultBrush });
            }

            return messageParts;
        }

        private int GetStatisticsCount(
            List<string> tokenNames,
            bool filterOnCurrentCommander,
            JournalEntry journalEntry,
            List<PropertyInfo> journalEntryProperties,
            CurrentData currentData,
            List<PropertyInfo> currentDataProperties)
        {
            var statisticsData = new EventStatisticsData
            {
                Commander =
                                             filterOnCurrentCommander
                                                 ? _informationManager.CurrentCommander.Commander
                                                 : null,
                Event = journalEntry.Event
            };
            var statisticsDataProperties = statisticsData.GetType().GetProperties().ToList();

            // Remove properties that are not allowed to be overridden
            statisticsDataProperties.RemoveWhere(prop => prop.Name == nameof(EventStatisticsData.Commander));
            statisticsDataProperties.RemoveWhere(prop => prop.Name == nameof(EventStatisticsData.Event));
            statisticsDataProperties.RemoveWhere(prop => prop.Name == nameof(EventStatisticsData.Count));

            foreach (var statisticsDataProperty in statisticsDataProperties)
            {
                if (!tokenNames.Any(
                        tokenName => tokenName.Equals(statisticsDataProperty.Name, StringComparison.OrdinalIgnoreCase)))
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

            var count = _informationManager.GetEventStatisticsSum(statisticsData);
            return count;
        }

        private void OnJournalEntryException(object sender, ThreadExceptionEventArgs eventArgs)
        {
            // TODO:
        }

        private void OnJournalEntryRead(object sender, JournalEntryEventArgs eventArgs)
        {
            var journalEntry = eventArgs.JournalEntry;
            var siouxEvent = _siouxData.Events.FirstOrDefault(e => e.Type == journalEntry.Event);
            if (siouxEvent == null)
            {
                return;
            }

            var capitalRegex = new Regex(@"(?<!\A)[A-Z]");
            var header = capitalRegex.Replace(journalEntry.Event.ToString(), match => " " + match.Value);

            RaiseSiouxEventReceived(siouxEvent, header, journalEntry);
        }

        private void RaiseSiouxEventReceived(SiouxEvent siouxEvent, string header, JournalEntry journalEntry)
        {
            if (siouxEvent == null)
            {
                return;
            }

            var inlines = GetMessageParts(journalEntry, _siouxData.FilterOnCurrentCommander, siouxEvent.Format);
            var displayDuration = siouxEvent.DisplayDuration != 0
                                      ? siouxEvent.DisplayDuration
                                      : _siouxData.DefaultDisplayDuration;

            SiouxEventReceived.Raise(this, new SiouxEventArgs(header, inlines, displayDuration));
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
    }
}