// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiouxManager.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Media;

    using EdNetApi.Common;
    using EdNetApi.Information;
    using EdNetApi.Journal;

    using JetBrains.Annotations;

    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;
    using Newtonsoft.Json.Schema.Generation;

    internal class SiouxManager
    {
        private const string CountValue = "count";
        private const string OrdinalCountValue = "ordinalcount";

        private readonly SiouxData _siouxData;

        private InformationManager _informationManager;

        public SiouxManager()
        {
            GenerateSiouxDataTokensFile();

            const string FilePath = "SiouxData.txt";
            List<string> errorMessages;
            if (!LoadSiouxData(FilePath, out _siouxData, out errorMessages))
            {
                errorMessages.Insert(0, $"Failed to load SiouxData at {FilePath}");
                throw new ApplicationException(string.Join(Environment.NewLine, errorMessages));
            }
        }

        public event EventHandler<SiouxEventArgs> SiouxEventReceived;

        public void Start()
        {
            Stop();

            var appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolderPath = Path.Combine(appDataFolderPath, Assembly.GetEntryAssembly().GetName().Name);
            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }

            _informationManager = new InformationManager(appFolderPath, true);
            _informationManager.JournalEntryRead += OnJournalEntryRead;

            // Start will read and cache all historical events and then return
            // If this is the first time then it might take a few minutes
            _informationManager.Start();

            const string Text = "Hello Cmdr {commander}!\nCurrent ship: {ship}\nCurrent star system: {starSystem}";
            var inlines = GetMessageParts(null, false, Text);
            SiouxEventReceived.Raise(this, new SiouxEventArgs("SIOUX Online", inlines, 10));
        }

        public void Stop()
        {
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

        private static bool LoadSiouxData(string filePath, out SiouxData siouxData, out List<string> errorMessages)
        {
            if (!File.Exists(filePath))
            {
                siouxData = null;
                errorMessages = new List<string> { "File not found" };
                return false;
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

            var tokenRegex = new Regex(@"(?<token>\{(?<tokenName>[A-Za-z]*)\})", RegexOptions.Compiled);
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

            var messageParts = new List<SiouxMessagePart>();
            var index = 0;
            foreach (var token in tokens)
            {
                var tokenName = token.Groups["tokenName"].Value.ToLowerInvariant();

                if (token.Index > index)
                {
                    messageParts.Add(new SiouxMessagePart { Text = format.Substring(index, token.Index - index) });
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

                    messageParts.Add(new SiouxMessagePart { Text = countString, Foreground = Brushes.ForestGreen });
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
                        messageParts.Add(new SiouxMessagePart { Text = value, Foreground = Brushes.ForestGreen });
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
                    messageParts.Add(new SiouxMessagePart { Text = value, Foreground = Brushes.ForestGreen });
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
                messageParts.Add(new SiouxMessagePart { Text = format.Substring(index) });
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
            var statisticsData = new StatisticsData
                                     {
                                         Commander =
                                             filterOnCurrentCommander
                                                 ? _informationManager.CurrentCommander.Commander
                                                 : null,
                                         Event = journalEntry.Event
                                     };
            var statisticsDataProperties = statisticsData.GetType().GetProperties().ToList();

            // Remove properties that are not allowed to be overridden
            statisticsDataProperties.RemoveWhere(prop => prop.Name == nameof(StatisticsData.Commander));
            statisticsDataProperties.RemoveWhere(prop => prop.Name == nameof(StatisticsData.Event));
            statisticsDataProperties.RemoveWhere(prop => prop.Name == nameof(StatisticsData.Count));

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

        private void OnJournalEntryRead(object sender, JournalEntryEventArgs eventArgs)
        {
            var journalEntry = eventArgs.JournalEntry;
            var siouxEvent = _siouxData.Events.FirstOrDefault(e => e.Type == journalEntry.Event);
            if (siouxEvent == null)
            {
                return;
            }

            var inlines = GetMessageParts(journalEntry, _siouxData.FilterOnCurrentCommander, siouxEvent.Format);
            var displayDuration = siouxEvent.DisplayDuration != 0
                                      ? siouxEvent.DisplayDuration
                                      : _siouxData.DefaultDisplayDuration;

            var capitalRegex = new Regex(@"(?<!\A)[A-Z]");
            var header = capitalRegex.Replace(journalEntry.Event.ToString(), match => " " + match.Value);

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