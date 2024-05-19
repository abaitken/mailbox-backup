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
        private readonly IFileSystem fileSystem;

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
            ArgsFileSource = Help << 1
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
            ArgsFileSource = _ArgumentConditions.ArgsFileSource | ExistingFile | TypeString
        }

        private readonly struct ArgumentDescription
        {
            public ArgumentDescription(IEnumerable<string> switches, string shorttext, string helptext, _ArgumentConditions conditions, IEnumerable<string> dependsOnKeys, string defaultValue)
            {
                if (switches == null || !switches.Any())
                    throw new ArgumentException("Expected switches");

                if (string.IsNullOrEmpty(shorttext))
                    throw new ArgumentException("Expected shorttext");

                if (string.IsNullOrEmpty(helptext))
                    throw new ArgumentException("Expected helptext");

                if (dependsOnKeys == null)
                    throw new ArgumentNullException(nameof(dependsOnKeys));

                Switches = switches;
                Shorttext = shorttext;
                Helptext = helptext;
                Conditions = conditions;
                DependsOnKeys = dependsOnKeys;
                DefaultValue = defaultValue;

            }

            public IEnumerable<string> Switches { get; }
            public string Shorttext { get; }
            public string Helptext { get; }
            public _ArgumentConditions Conditions { get; }
            public IEnumerable<string> DependsOnKeys { get; }
            public string DefaultValue { get; }

        }

        internal ArgumentParser(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public ArgumentParser()
            : this(new FileSystem())
        {
            this.argumentDescriptions = new Dictionary<string, ArgumentDescription>();
        }

        public void Describe(string key, IEnumerable<string> switches, string shorttext, string helptext, ArgumentConditions conditions, IEnumerable<string> dependsOnKeys = null, string defaultValue = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Expected key");

            var adjustedConditions = conditions;
            if (!adjustedConditions.HasFlag(ArgumentConditions.Required))
                adjustedConditions |= ArgumentConditions.Optional;

            dependsOnKeys = dependsOnKeys ?? Enumerable.Empty<string>();

            if (!dependsOnKeys.All(o => argumentDescriptions.ContainsKey(o)) || (dependsOnKeys.Any() && argumentDescriptions.Count == 0))
                throw new InvalidOperationException("Dependency argument keys not found!");

            // TODO : Check conflicting conditions
            argumentDescriptions.Add(key, new ArgumentDescription(switches.ToList(), shorttext, helptext, (_ArgumentConditions)adjustedConditions, dependsOnKeys, defaultValue));
        }

        public class ArgumentValues
        {
            private readonly List<string> keys;
            private readonly Dictionary<string, string> strings;
            private readonly Dictionary<string, int> ints;
            private readonly Dictionary<string, double> reals;
            private readonly Dictionary<string, bool> bools;

            public ArgumentValues()
            {
                this.keys = new List<String>();
                this.strings = new Dictionary<string, string>();
                this.ints = new Dictionary<string, int>();
                this.reals = new Dictionary<string, double>();
                this.bools = new Dictionary<string, bool>();
            }

            private void Add<T>(string key, T value, Dictionary<string, T> store)
            {
                if (ContainsKey(key))
                {
                    store[key] = value;
                    return;
                }

                keys.Add(key);
                store.Add(key, value);
            }

            internal void Add(string key, bool value)
            {
                Add(key, value, bools);
            }

            internal void Add(string key, int value)
            {
                Add(key, value, ints);
            }

            internal void Add(string key, double value)
            {
                Add(key, value, reals);
            }

            internal void Add(string key, string value)
            {
                Add(key, value, strings);
            }

            internal bool ContainsKey(string key)
            {
                return keys.Contains(key);
            }

            public string this[string key]
            {
                get { return strings[key]; }
            }

            public string GetString(string key, string defaultValue)
            {
                if (!ContainsKey(key))
                    return defaultValue;

                return this[key];
            }

            public bool GetBool(string key)
            {
                return bools[key];
            }

            public int GetInt(string key)
            {
                return ints[key];
            }

            internal double GetReal(string key)
            {
                return reals[key];
            }
        }

        public enum ValidationErrorType
        {
            UnrecognisedSwitch,
            IncorrectType,
            FileSystemObjectNotFound,
            RequiredArgMissing,
            NoValue
        }

        public readonly struct ValidationError
        {
            public ValidationError(ValidationErrorType errorType, string value, string key)
            {
                ErrorType = errorType;
                Value = value;
                Key = key;
            }

            public ValidationErrorType ErrorType { get; }
            public string Value { get; }
            public string Key { get; }
        }

        class SpecialQueue<T>
        {
            private readonly List<T> inner;

            public SpecialQueue(IEnumerable<T> initial)
            {
                this.inner = new List<T>(initial);
            }

            public int Count
            {
                get => inner.Count;
            }

            private T UnsafeDequeue()
            {
                var result = inner[0];
                inner.RemoveAt(0);
                return result;
            }

            public T Dequeue()
            {
                if (Count == 0)
                    throw new InvalidOperationException();

                return UnsafeDequeue();
            }

            public bool TryDequeue(out T value)
            {
                if (Count == 0)
                {
                    value = default;
                    return false;
                }
                value = UnsafeDequeue();
                return true;
            }

            public void PutFront(T value)
            {
                inner.Insert(0, value);
            }

            public void PutFront(params T[] value)
            {
                inner.InsertRange(0, value);
            }

            public void PutFront(IEnumerable<T> values)
            {
                inner.InsertRange(0, values);
            }

        }

        public IEnumerable<ValidationError> ParseArgs(string[] args, out ArgumentValues argumentValues)
        {
            var result = new ArgumentValues();
            var errors = new List<ValidationError>();
            var values = new Dictionary<string, string>();
            var argQueue = new SpecialQueue<string>(args);

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
                        if(value != null)
                            argQueue.PutFront(value);
                    }

                    // Use value is present or revert to true if there is no value
                    var argValue = !booleanValue.HasValue || booleanValue.Value;

                    result.Add(argumentDescription.Key, argValue);
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


                if (conditions.HasFlag(_ArgumentConditions.ExistingDir))
                {
                    if (!fileSystem.DirectoryExists(value))
                    {
                        errors.Add(new ValidationError(ValidationErrorType.FileSystemObjectNotFound, item, argumentDescription.Key));
                        continue;
                    }
                }

                if (conditions.HasFlag(_ArgumentConditions.ExistingFile))
                {
                    if (!fileSystem.FileExists(value))
                    {
                        errors.Add(new ValidationError(ValidationErrorType.FileSystemObjectNotFound, item, argumentDescription.Key));
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
                foreach (var key in argumentDescription.Value.DependsOnKeys)
                {
                    if (!result.ContainsKey(key))
                        errors.Add(new ValidationError(ValidationErrorType.RequiredArgMissing, null, key));
                }

                if (result.ContainsKey(argumentDescription.Key))
                    continue;

                var conditions = argumentDescription.Value.Conditions;
                if (conditions.HasFlag(_ArgumentConditions.Required))
                {
                    errors.Add(new ValidationError(ValidationErrorType.RequiredArgMissing, null, argumentDescription.Key));
                    continue;
                }

                var value = argumentDescription.Value.DefaultValue;
                if(string.IsNullOrEmpty(value))
                    continue;

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

                // For string
                result.Add(argumentDescription.Key, value);
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

                var otherForms = item.Value.Switches.Skip(1).Any()
                    ? "\n" + "(Other forms: " + item.Value.Switches.Skip(1).Combine(" ") + ")"
                    : string.Empty;

                var optional = item.Value.Conditions.HasFlag(_ArgumentConditions.Optional)
                    ? "(Optional) "
                    : string.Empty;

                var defaultValue = !string.IsNullOrEmpty(item.Value.DefaultValue)
                    ? "\nDefault value: " + item.Value.DefaultValue
                    : string.Empty;

                var dependsOnList = new StringBuilder();
                foreach (var key in item.Value.DependsOnKeys)
                {
                    if (dependsOnList.Length != 0)
                        dependsOnList.Append(' ');
                    dependsOnList.Append(argumentDescriptions[key].Switches.First());
                }

                var dependsOn = dependsOnList.Length != 0
                    ? "\n" + "Depends on " + dependsOnList.ToString()
                    : string.Empty;

                var configurationKey = "\nConfiguration key: " + item.Key;

                helptext.Add(optional + item.Value.Helptext + dependsOn + otherForms + defaultValue + configurationKey);
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
                ValidationErrorType.IncorrectType => $"Incorrect value type provided for '{argumentDescription.Shorttext}' ({argumentDescription.Switches.First()})",
                ValidationErrorType.FileSystemObjectNotFound => $"File or directory for argument '{argumentDescription.Shorttext}' ({argumentDescription.Switches.First()}) not found",
                ValidationErrorType.RequiredArgMissing => $"Required argument '{argumentDescription.Shorttext}' ({argumentDescription.Switches.First()}) missing",
                ValidationErrorType.NoValue => $"Argument '{argumentDescription.Shorttext}' ({argumentDescription.Switches.First()}) has no value",
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