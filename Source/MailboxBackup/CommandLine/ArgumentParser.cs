using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Org.BouncyCastle.Asn1;

namespace MailboxBackup
{
    class ArgumentParser
    {
        private readonly Dictionary<string, ArgumentDescription> argumentDescriptions;
        private readonly ConflictingModel conflictingArguments;
        private readonly IFileSystem fileSystem;
        private readonly IEnvironment environment;

        class ConflictingModel
        {
            private readonly Dictionary<string, List<string>> store;

            public ConflictingModel()
            {
                store = new Dictionary<string, List<string>>();
            }

            internal void Add(ArgumentKey key, IEnumerable<ArgumentKey> conflictsWith)
            {
                var keys = (from item in conflictsWith select item.Value).ToList();

                AddConflict(key.Value, keys);

                foreach (var item in keys)
                {
                    AddConflict(item, new[] { key.Value });
                }
            }

            private void AddConflict(string key, IEnumerable<string> conflictsWith)
            {
                if (!store.ContainsKey(key))
                    store.Add(key, new List<string>());

                var keys = store[key];

                foreach (var item in conflictsWith)
                {
                    if (!keys.Contains(item))
                        keys.Add(item);
                }
            }

            internal IEnumerable<string> ConflictsWith(string key)
            {
                if (!store.ContainsKey(key))
                    return Enumerable.Empty<string>();

                return store[key];
            }
        }

        /// <summary>
        /// Internal values... to be combined with public values
        /// </summary>
        [Flags]
        private enum _ArgumentConditions
        {
            None = 0,
            Optional = 1,
            Required = Optional << 1,
            HasValue = Required << 1,
            TypeInteger = HasValue << 1,
            TypeReal = TypeInteger << 1,
            TypeString = TypeReal << 1,
            TypeBoolean = TypeString << 1,
            IsFlag = TypeBoolean << 1,
            ExistingDir = IsFlag << 1,
            ExistingFile = ExistingDir << 1,
            Help = ExistingFile << 1,
            ArgsFileSource = Help << 1,
            Options = ArgsFileSource << 1,
            ArgsEnvSource = Options << 1
        }

        [Flags]
        public enum ArgumentConditions
        {
            None = _ArgumentConditions.None,
            Optional = _ArgumentConditions.Optional,
            Required = _ArgumentConditions.Required,
            HasValue = _ArgumentConditions.HasValue,
            TypeInteger = HasValue | _ArgumentConditions.TypeInteger,
            TypeReal = HasValue | _ArgumentConditions.TypeReal,
            TypeString = HasValue | _ArgumentConditions.TypeString,
            TypeBoolean = HasValue | _ArgumentConditions.TypeBoolean,
            IsFlag = _ArgumentConditions.TypeBoolean | _ArgumentConditions.IsFlag,
            ExistingDir = _ArgumentConditions.ExistingDir,
            ExistingFile = _ArgumentConditions.ExistingFile,
            Help = _ArgumentConditions.Help | IsFlag,
            ArgsFileSource = _ArgumentConditions.ArgsFileSource | ExistingFile | TypeString,
            Options = _ArgumentConditions.Options | TypeString,
            ArgsEnvSource = _ArgumentConditions.ArgsEnvSource | IsFlag
        }

        public readonly struct ArgumentKey
        {
            private readonly string _key;

            public ArgumentKey(string key)
            {
                if (string.IsNullOrEmpty(key))
                    throw new ArgumentNullException(nameof(key), "Expected key");

                _key = key;
            }

            internal string Value => _key;
        }

        private interface IArgumentDescription
        {
            _ArgumentConditions Conditions { get; }
            string DefaultValue { get; }
            IEnumerable<ArgumentKey> DependsOn { get; }
            IEnumerable<ArgumentKey> ConflictsWith { get; }
            string Helptext { get; }
            IEnumerable<string> Options { get; }
            string Shorttext { get; }
            IEnumerable<string> Switches { get; }
        }

        private readonly struct ArgumentDescription : IArgumentDescription
        {
            public ArgumentDescription(IEnumerable<string> switches, string shorttext, string helptext, _ArgumentConditions conditions, IEnumerable<ArgumentKey> dependsOn, IEnumerable<ArgumentKey> conflictsWith, string defaultValue, IEnumerable<string> options)
            {
                if (switches == null || !switches.Any())
                    throw new ArgumentException("Expected switches");

                if (string.IsNullOrEmpty(shorttext))
                    throw new ArgumentException("Expected shorttext");

                if (string.IsNullOrEmpty(helptext))
                    throw new ArgumentException("Expected helptext");

                if (dependsOn == null)
                    throw new ArgumentNullException(nameof(dependsOn));

                if (conflictsWith == null)
                    throw new ArgumentNullException(nameof(conflictsWith));

                Switches = switches;
                Shorttext = shorttext;
                Helptext = helptext;
                Conditions = conditions;
                DependsOn = dependsOn;
                ConflictsWith = conflictsWith;
                DefaultValue = defaultValue;
                Options = options;
            }

            public IEnumerable<string> Switches { get; }
            public string Shorttext { get; }
            public string Helptext { get; }
            public _ArgumentConditions Conditions { get; }
            public IEnumerable<ArgumentKey> DependsOn { get; }
            public IEnumerable<ArgumentKey> ConflictsWith { get; }
            public string DefaultValue { get; }
            public IEnumerable<string> Options { get; }
        }

        internal ArgumentParser(IFileSystem fileSystem, IEnvironment environment)
        {
            this.fileSystem = fileSystem;
            this.environment = environment;
            this.argumentDescriptions = new Dictionary<string, ArgumentDescription>();
            this.conflictingArguments = new ConflictingModel();
        }

        public ArgumentParser()
            : this(new FileSystem(), new Environment())
        {
        }

        public ArgumentKey Describe(string key, IEnumerable<string> switches, string shorttext, string helptext, ArgumentConditions conditions, IEnumerable<ArgumentKey> dependsOn = null, IEnumerable<ArgumentKey> conflictsWith = null, string defaultValue = null, IEnumerable<string> options = null)
        {
            return Describe(new ArgumentKey(key), switches, shorttext, helptext, conditions, dependsOn, conflictsWith, defaultValue, options);
        }

        public ArgumentKey Describe(ArgumentKey key, IEnumerable<string> switches, string shorttext, string helptext, ArgumentConditions conditions, IEnumerable<ArgumentKey> dependsOn = null, IEnumerable<ArgumentKey> conflictsWith = null, string defaultValue = null, IEnumerable<string> options = null)
        {
            var adjustedConditions = conditions;
            if (!adjustedConditions.HasFlag(ArgumentConditions.Required))
                adjustedConditions |= ArgumentConditions.Optional;

            dependsOn = dependsOn ?? Enumerable.Empty<ArgumentKey>();
            conflictsWith = conflictsWith ?? Enumerable.Empty<ArgumentKey>();

            if (!dependsOn.All(o => argumentDescriptions.ContainsKey(o.Value)) || (dependsOn.Any() && argumentDescriptions.Count == 0))
                throw new InvalidOperationException("Dependency argument keys not found!");

            // NOTE : Allow conflict keys before the argument has been described
            //if (!conflictsWith.All(o => argumentDescriptions.ContainsKey(o.Value)) || (conflictsWith.Any() && argumentDescriptions.Count == 0))
            //    throw new InvalidOperationException("Conflicting argument keys not found!");

            // TODO : Check more conflicting conditions
            if (adjustedConditions.HasFlag(ArgumentConditions.Options))
            {
                if (options == null || !options.Any())
                    throw new InvalidOperationException("Options condition must have option values");

                if (!string.IsNullOrEmpty(defaultValue) && !options.Contains(defaultValue))
                    throw new InvalidOperationException("Options condition default value must be present in valid options");
            }

            conflictingArguments.Add(key, conflictsWith);
            argumentDescriptions.Add(key.Value, new ArgumentDescription(switches.ToList(), shorttext, helptext, (_ArgumentConditions)adjustedConditions, dependsOn, conflictsWith, defaultValue, options));
            return key;
        }

        public IEnumerable<ValidationError> ParseArgs(string[] args, out ArgumentValues argumentValues)
        {
            var result = new ArgumentValues();
            var errors = new List<ValidationError>();
            var values = new Dictionary<string, string>();
            var argQueue = new SpecialQueue<string>(args);
            var useEnv = false;

            while (argQueue.Count != 0)
            {
                var item = argQueue.Dequeue();

                var argumentDescriptionLookup = argumentDescriptions.FirstOrNull(o => o.Value.Switches.Contains(item));

                if (!argumentDescriptionLookup.HasValue)
                {
                    errors.Add(new ValidationError(ValidationErrorType.UnrecognisedSwitch, item, null));
                    continue;
                }
                var argumentDescription = argumentDescriptionLookup.Value;

                if (!argQueue.TryDequeue(out var value))
                {
                    value = null;
                }

                var conditions = argumentDescription.Value.Conditions;

                // A flag can exist on it's own or with a true/false (--flag true|false)
                if (conditions.HasFlag(_ArgumentConditions.IsFlag))
                {
                    // If there is no value, treat as it has no value
                    var booleanValue = (value == null) ? null : ToBoolean(value);

                    // If no valid value, restore value to front of queue
                    if (!booleanValue.HasValue)
                    {
                        if (value != null)
                            argQueue.PutFront(value);
                    }

                    // Use value is present or revert to true if there is no value
                    var argValue = !booleanValue.HasValue || booleanValue.Value;

                    result.Add(argumentDescription.Key, argValue);
                    
                    if (conditions.HasFlag(_ArgumentConditions.ArgsEnvSource))
                    {
                        useEnv = true;
                    }
                    continue;
                }

                if (conditions.HasFlag(_ArgumentConditions.HasValue))
                {
                    if (value == null)
                    {
                        errors.Add(new ValidationError(ValidationErrorType.NoValue, item, argumentDescription.Key));
                        continue;
                    }
                }

                if (conditions.HasFlag(_ArgumentConditions.TypeInteger))
                {
                    if (!int.TryParse(value, out int argValue))
                    {
                        errors.Add(new ValidationError(ValidationErrorType.IncorrectType, value, argumentDescription.Key));
                        continue;
                    }

                    result.Add(argumentDescription.Key, argValue);
                    continue;
                }

                if (conditions.HasFlag(_ArgumentConditions.TypeReal))
                {
                    if (!double.TryParse(value, out double argValue))
                    {
                        errors.Add(new ValidationError(ValidationErrorType.IncorrectType, value, argumentDescription.Key));
                        continue;
                    }

                    result.Add(argumentDescription.Key, argValue);
                    continue;
                }

                if (conditions.HasFlag(_ArgumentConditions.TypeBoolean))
                {
                    var argValue = ToBoolean(value);
                    if (!argValue.HasValue)
                    {
                        errors.Add(new ValidationError(ValidationErrorType.IncorrectType, value, argumentDescription.Key));
                        continue;
                    }

                    result.Add(argumentDescription.Key, argValue.Value);
                    continue;
                }

                // Assumed to be a String...
                if (conditions.HasFlag(_ArgumentConditions.Options))
                {
                    if (!argumentDescription.Value.Options.Contains(value))
                    {
                        errors.Add(new ValidationError(ValidationErrorType.UnknownOption, value, argumentDescription.Key));
                        continue;
                    }
                }

                if (conditions.HasFlag(_ArgumentConditions.ExistingDir))
                {
                    if (!fileSystem.DirectoryExists(value))
                    {
                        errors.Add(new ValidationError(ValidationErrorType.FileSystemObjectNotFound, value, argumentDescription.Key));
                        continue;
                    }
                }

                if (conditions.HasFlag(_ArgumentConditions.ExistingFile))
                {
                    if (!fileSystem.FileExists(value))
                    {
                        errors.Add(new ValidationError(ValidationErrorType.FileSystemObjectNotFound, value, argumentDescription.Key));
                        continue;
                    }
                }

                result.Add(argumentDescription.Key, value);

                if (conditions.HasFlag(_ArgumentConditions.ArgsFileSource))
                {
                    using Stream configStream = fileSystem.Read(value);
                    using var jsonDoc = JsonDocument.Parse(configStream);
                    foreach (var obj in jsonDoc.RootElement.EnumerateObject())
                    {
                        if (!argumentDescriptions.ContainsKey(obj.Name))
                            continue;

                        var argSubject = argumentDescriptions[obj.Name];
                        argQueue.PutFront(argSubject.Switches.First(), obj.Value.ToString());
                    }
                    configStream.Close();
                    continue;
                }
            }

            // Check for missing required arguments and add values for arguments with default values
            foreach (var argumentDescription in argumentDescriptions)
            {
                foreach (var key in argumentDescription.Value.DependsOn)
                {
                    if (!result.ContainsKey(key.Value))
                        errors.Add(new ValidationError(ValidationErrorType.RequiredArgMissing, null, key.Value));
                }

                foreach (var key in conflictingArguments.ConflictsWith(argumentDescription.Key))
                {
                    if (result.UserGiven(key) && result.UserGiven(argumentDescription.Key))
                        errors.Add(new ValidationError(ValidationErrorType.ConflictingArgPresent, null, key));
                }

                if (result.ContainsKey(argumentDescription.Key))
                    continue;

                var conditions = argumentDescription.Value.Conditions;
                if (conditions.HasFlag(_ArgumentConditions.Required))
                {
                    errors.Add(new ValidationError(ValidationErrorType.RequiredArgMissing, null, argumentDescription.Key));
                    continue;
                }

                var value = (useEnv)
                    ? environment.GetVariable(argumentDescription.Key, argumentDescription.Value.DefaultValue)
                    : argumentDescription.Value.DefaultValue;

                if (string.IsNullOrEmpty(value))
                {
                    if (conditions.HasFlag(_ArgumentConditions.IsFlag))
                    {
                        result.Add(argumentDescription.Key, false, false);
                        continue;
                    }
                    continue;
                }

                if (conditions.HasFlag(_ArgumentConditions.TypeInteger))
                {
                    if (!int.TryParse(value, out int argValue))
                    {
                        errors.Add(new ValidationError(ValidationErrorType.IncorrectType, value, argumentDescription.Key));
                        continue;
                    }

                    result.Add(argumentDescription.Key, argValue);
                    continue;
                }

                if (conditions.HasFlag(_ArgumentConditions.TypeReal))
                {
                    if (!double.TryParse(value, out double argValue))
                    {
                        errors.Add(new ValidationError(ValidationErrorType.IncorrectType, value, argumentDescription.Key));
                        continue;
                    }

                    result.Add(argumentDescription.Key, argValue, false);
                    continue;
                }

                if (conditions.HasFlag(_ArgumentConditions.TypeBoolean))
                {
                    var argValue = ToBoolean(value);
                    if (!argValue.HasValue)
                    {
                        errors.Add(new ValidationError(ValidationErrorType.IncorrectType, value, argumentDescription.Key));
                        continue;
                    }

                    result.Add(argumentDescription.Key, argValue.Value, false);
                    continue;
                }

                // For string
                result.Add(argumentDescription.Key, value, false);
            }

            argumentValues = result;
            return errors.Distinct().ToList();
        }

        private static bool? ToBoolean(string value)
        {
            if (value.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                return true;
            if (value.Equals("false", StringComparison.CurrentCultureIgnoreCase))
                return false;
            return null;
        }

        internal void DisplayHelp(int maxWidth)
        {
            var switches = new List<string>();
            var helptext = new List<string>();

            foreach (var item in argumentDescriptions)
            {
                switches.Add(item.Value.Switches.First() + GetSwitchValueText(item.Value.Conditions));

                var optional = item.Value.Conditions.HasFlag(_ArgumentConditions.Optional)
                    ? "(Optional) "
                    : string.Empty;

                var otherForms = item.Value.Switches.Skip(1).Any()
                    ? "\n" + "(Other forms: " + item.Value.Switches.Skip(1).Combine(" ") + ")"
                    : string.Empty;

                var defaultValue = !string.IsNullOrEmpty(item.Value.DefaultValue)
                    ? "\nDefault value: " + item.Value.DefaultValue
                    : string.Empty;

                var options = item.Value.Conditions.HasFlag(_ArgumentConditions.Options)
                    ? "\nOptions: " + item.Value.Options.Combine(" ")
                    : string.Empty;

                var dependsOnList = new StringBuilder();
                foreach (var key in item.Value.DependsOn)
                {
                    if (dependsOnList.Length != 0)
                        dependsOnList.Append(' ');
                    dependsOnList.Append(argumentDescriptions[key.Value].Switches.First());
                }

                var dependsOn = dependsOnList.Length != 0
                    ? "\n" + "Depends on " + dependsOnList.ToString()
                    : string.Empty;

                var conflictsWithList = new StringBuilder();
                foreach (var key in item.Value.ConflictsWith)
                {
                    if (conflictsWithList.Length != 0)
                        conflictsWithList.Append(' ');
                    conflictsWithList.Append(argumentDescriptions[key.Value].Switches.First());
                }

                var conflictsWith = conflictsWithList.Length != 0
                    ? "\n" + "Conflicts with " + conflictsWithList.ToString()
                    : string.Empty;

                var configurationKey = "\nConfiguration key: " + item.Key;

                helptext.Add(optional + item.Value.Helptext + dependsOn + conflictsWith + otherForms + defaultValue + options + configurationKey);
            }

            var maxSwitchWidth = switches.Max(o => o.Length);

            int switchColumnWidth = maxSwitchWidth + 4;
            string switchIndent = " ";
            int helpTextColumnWidth = maxWidth - switchColumnWidth - switchIndent.Length - 1;
            string emptySwitchColumn = new string(' ', switchColumnWidth);
            for (int i = 0; i < argumentDescriptions.Count; i++)
            {
                var helpTextLines = helptext[i].Split('\n');

                for (int j = 0; j < helpTextLines.Length; j++)
                {
                    var switchColumn = j == 0
                        ? switchIndent + switches[i].PadRight(switchColumnWidth)
                        : switchIndent + emptySwitchColumn;

                    var helpText = helpTextLines[j];
                    var actualLines = helpText.SplitStringIntoLengths(helpTextColumnWidth);
                    foreach (var line in actualLines)
                    {
                        Console.WriteLine(switchColumn + line);
                        switchColumn = switchIndent + emptySwitchColumn;
                    }
                }

                Console.WriteLine();
            }
        }

        private static string GetSwitchValueText(_ArgumentConditions conditions)
        {
            if (conditions.HasFlag(_ArgumentConditions.IsFlag))
                return string.Empty;

            if (conditions.HasFlag(_ArgumentConditions.Options))
                return " OPTION";

            if (conditions.HasFlag(_ArgumentConditions.ExistingDir))
                return " DIR";

            if (conditions.HasFlag(_ArgumentConditions.ExistingFile))
                return " FILE";

            if (conditions.HasFlag(_ArgumentConditions.TypeString))
                return " TEXT";

            if (conditions.HasFlag(_ArgumentConditions.TypeInteger))
                return " ###";

            if (conditions.HasFlag(_ArgumentConditions.TypeReal))
                return " #.#";

            if (conditions.HasFlag(_ArgumentConditions.TypeBoolean))
                return " true|false";

            return " ???";
        }

        private string FormatError(ValidationErrorType errorType, string key, string value)
        {
            if (errorType == ValidationErrorType.UnrecognisedSwitch)
                return $"Unrecognised switch '{value}'";

            var argumentDescription = argumentDescriptions[key];
            return errorType switch
            {
                ValidationErrorType.IncorrectType => $"Incorrect value type '{value}' provided for '{argumentDescription.Shorttext}' ({argumentDescription.Switches.First()})",
                ValidationErrorType.FileSystemObjectNotFound => $"File or directory '{value}' for argument '{argumentDescription.Shorttext}' ({argumentDescription.Switches.First()}) not found",
                ValidationErrorType.RequiredArgMissing => $"Required argument '{argumentDescription.Shorttext}' ({argumentDescription.Switches.First()}) missing",
                ValidationErrorType.ConflictingArgPresent => $"Argument '{argumentDescription.Shorttext}' ({argumentDescription.Switches.First()}) conflicts with other arguments",
                ValidationErrorType.NoValue => $"Argument '{argumentDescription.Shorttext}' ({argumentDescription.Switches.First()}) has no value",
                ValidationErrorType.UnknownOption => $"Unexpected option '{value}' provided for argument '{argumentDescription.Shorttext}' ({argumentDescription.Switches.First()})",
                _ => throw new ArgumentOutOfRangeException(nameof(errorType)),
            };
        }

        internal void ReportErrors(IEnumerable<ValidationError> argumentErrors)
        {
            foreach (var item in argumentErrors)
            {
                var line = FormatError(item.ErrorType, item.Key, item.Value);
                Console.WriteLine(line);
            }

            var help = argumentDescriptions.FirstOrNull(o => o.Value.Conditions.HasFlag(_ArgumentConditions.Help));
            if (!help.HasValue)
                return;

            Console.WriteLine();
            Console.WriteLine($"Use '{help.Value.Value.Switches.First()}' to display more help.");
        }
    }
}