namespace MailboxBackup.Tests;
using MailboxBackup;
using static MailboxBackup.ArgumentParser;

[TestClass]
public class ArgumentParserTests
{
    [TestMethod]
    public void KeyIsRequired1()
    {
        var parser = new ArgumentParser();
        Assert.ThrowsExactly<ArgumentNullException>(() => parser.Describe(null, new[] { "-h", "-?" }, "help", "help detail", ArgumentParser.ArgumentConditions.Help));
    }

    [TestMethod]
    public void KeyIsRequired2()
    {
        var parser = new ArgumentParser();
        Assert.ThrowsExactly<ArgumentNullException>(() => parser.Describe(string.Empty, new[] { "-h", "-?" }, "help", "help detail", ArgumentParser.ArgumentConditions.Help));
    }

    [TestMethod]
    public void SwitchesRequired()
    {
        var parser = new ArgumentParser();
        Assert.ThrowsExactly<ArgumentException>(() => parser.Describe("HELP", Array.Empty<string>(), "help", "help detail", ArgumentParser.ArgumentConditions.Help));
    }

    [TestMethod]
    public void HelpTextRequired1()
    {
        var parser = new ArgumentParser();
        Assert.ThrowsExactly<ArgumentException>(() => parser.Describe("HELP", new[] { "-h", "-?" }, string.Empty, "help detail", ArgumentParser.ArgumentConditions.Help));
    }

    [TestMethod]
    public void HelpTextRequired2()
    {
        var parser = new ArgumentParser();
        Assert.ThrowsExactly<ArgumentException>(() => parser.Describe("HELP", new[] { "-h", "-?" }, "help", string.Empty, ArgumentParser.ArgumentConditions.Help));
    }

    [TestMethod]
    public void DependencyRequired1()
    {
        var parser = new ArgumentParser();
        Assert.ThrowsExactly<InvalidOperationException>(() => parser.Describe("HELP", new[] { "-h", "-?" }, "help", "help detail", ArgumentParser.ArgumentConditions.Help, new [] { new ArgumentKey("DEPENDS") }));
    }

    [TestMethod]
    public void DependencyRequired2()
    {
        var parser = new ArgumentParser();
        Assert.ThrowsExactly<InvalidOperationException>(() => parser.Describe("HELP", new[] { "-h", "-?" }, "help", "help detail", ArgumentParser.ArgumentConditions.Help, new [] { new ArgumentKey("DEPENDS") }));
    }

    [TestMethod]
    public void DependencyRequired3()
    {
        var parser = new ArgumentParser();
        parser.Describe("DEPENDS", new[] { "-o" }, "other", "other detail", ArgumentParser.ArgumentConditions.Required);
        parser.Describe("HELP", new[] { "-h", "-?" }, "help", "help detail", ArgumentParser.ArgumentConditions.Help, new [] { new ArgumentKey("DEPENDS") });
    }

    [TestMethod]
    public void DependencyRequired4()
    {
        var parser = new ArgumentParser();
        parser.Describe("DEPENDS", new[] { "-o" }, "other", "other detail", ArgumentParser.ArgumentConditions.Optional);
        parser.Describe("HELP", new[] { "-h", "-?" }, "help", "help detail", ArgumentParser.ArgumentConditions.Help, new [] { new ArgumentKey("DEPENDS") });

        var errors = parser.ParseArgs(new[] { "-h" }, out var values);
        Assert.AreEqual(1, errors.Count());
        Assert.AreEqual(ValidationErrorType.RequiredArgMissing, errors.First().ErrorType);
    }

    [TestMethod]
    public void FlagTest()
    {
        var parser = new ArgumentParser();
        parser.Describe("FLAG", new[] { "-x" }, "flag", "flag detail", ArgumentParser.ArgumentConditions.IsFlag);

        // Test 1
        {
            var errors = parser.ParseArgs(new[] { "-x" }, out var values);
            Assert.AreEqual(0, errors.Count(), "1");
            Assert.IsTrue(values.ContainsKey("FLAG"), "1");
            Assert.IsTrue(values.GetBool("FLAG"), "1");
        }
        // Test 2
        {
            var errors = parser.ParseArgs(new[] { "-x", "true" }, out var values);
            Assert.AreEqual(0, errors.Count(), "2");
            Assert.IsTrue(values.ContainsKey("FLAG"), "2");
            Assert.IsTrue(values.GetBool("FLAG"), "2");
        }
        // Test 3
        {
            var errors = parser.ParseArgs(new[] { "-x", "false" }, out var values);
            Assert.AreEqual(0, errors.Count(), "3");
            Assert.IsTrue(values.ContainsKey("FLAG"), "3");
            Assert.IsFalse(values.GetBool("FLAG"), "3");
        }
        // Test 4
        {
            var errors = parser.ParseArgs(new string[0], out var values);
            Assert.AreEqual(0, errors.Count(), "4");
            Assert.IsTrue(values.ContainsKey("FLAG"), "4");
            Assert.IsFalse(values.GetBool("FLAG"), "4");
        }
        // Test 5
        {
            var errors = parser.ParseArgs(new[] { "-x", "asdasd" }, out var values);
            Assert.AreEqual(1, errors.Count(), "5");
            Assert.IsTrue(values.ContainsKey("FLAG"), "5");
            Assert.IsTrue(values.GetBool("FLAG"), "5");
            Assert.AreEqual(ValidationErrorType.UnrecognisedSwitch, errors.First().ErrorType);
            Assert.AreEqual("asdasd", errors.First().Value);
        }
    }

    [TestMethod]
    public void IntTest()
    {
        var parser = new ArgumentParser();
        parser.Describe("INT", new[] { "-i" }, "int", "int detail", ArgumentParser.ArgumentConditions.TypeInteger);

        // Test 1
        {
            var errors = parser.ParseArgs(new[] { "-i" }, out var values);
            Assert.AreEqual(1, errors.Count(), "1");
            Assert.IsFalse(values.ContainsKey("INT"), "1");
            Assert.AreEqual(ValidationErrorType.NoValue, errors.First().ErrorType, "1");
        }
        // Test 2
        {
            var errors = parser.ParseArgs(new[] { "-i", "42" }, out var values);
            Assert.AreEqual(0, errors.Count(), "2");
            Assert.IsTrue(values.ContainsKey("INT"), "2");
            Assert.AreEqual(42, values.GetInt("INT"), "2");
        }
        // Test 3
        {
            var errors = parser.ParseArgs(new[] { "-i", "6.9" }, out var values);
            Assert.AreEqual(1, errors.Count(), "3");
            Assert.IsFalse(values.ContainsKey("INT"), "3");
            Assert.AreEqual(ValidationErrorType.IncorrectType, errors.First().ErrorType, "3");
            Assert.AreEqual("6.9", errors.First().Value, "3");
        }
        // Test 4
        {
            var errors = parser.ParseArgs(new[] { "-i", "sdafsd" }, out var values);
            Assert.AreEqual(1, errors.Count(), "4");
            Assert.IsFalse(values.ContainsKey("INT"), "4");
            Assert.AreEqual(ValidationErrorType.IncorrectType, errors.First().ErrorType, "4");
            Assert.AreEqual("sdafsd", errors.First().Value, "4");
        }
    }

    [TestMethod]
    public void RealTest()
    {
        var parser = new ArgumentParser();
        parser.Describe("REAL", new[] { "-r" }, "real", "real detail", ArgumentParser.ArgumentConditions.TypeReal);

        // Test 1
        {
            var errors = parser.ParseArgs(new[] { "-r" }, out var values);
            Assert.AreEqual(1, errors.Count(), "1");
            Assert.IsFalse(values.ContainsKey("REAL"), "1");
            Assert.AreEqual(ValidationErrorType.NoValue, errors.First().ErrorType, "1");
        }
        // Test 2
        {
            var errors = parser.ParseArgs(new[] { "-r", "42" }, out var values);
            Assert.AreEqual(0, errors.Count(), "2");
            Assert.IsTrue(values.ContainsKey("REAL"), "2");
            Assert.AreEqual(42.0, values.GetReal("REAL"), "2");
        }
        // Test 3
        {
            var errors = parser.ParseArgs(new[] { "-r", "6.9" }, out var values);
            Assert.AreEqual(0, errors.Count(), "3");
            Assert.IsTrue(values.ContainsKey("REAL"), "3");
            Assert.AreEqual(6.9, values.GetReal("REAL"), "3");
        }
        // Test 4
        {
            var errors = parser.ParseArgs(new[] { "-r", "sdafsd" }, out var values);
            Assert.AreEqual(1, errors.Count(), "4");
            Assert.IsFalse(values.ContainsKey("REAL"), "4");
            Assert.AreEqual(ValidationErrorType.IncorrectType, errors.First().ErrorType, "4");
            Assert.AreEqual("sdafsd", errors.First().Value, "4");
        }
    }

    [TestMethod]
    public void BoolTest()
    {
        var parser = new ArgumentParser();
        parser.Describe("BOOL", new[] { "-b" }, "bool", "bool detail", ArgumentParser.ArgumentConditions.TypeBoolean);

        // Test 1
        {
            var errors = parser.ParseArgs(new[] { "-b" }, out var values);
            Assert.AreEqual(1, errors.Count(), "1");
            Assert.IsFalse(values.ContainsKey("BOOL"), "1");
            Assert.AreEqual(ValidationErrorType.NoValue, errors.First().ErrorType, "1");
        }
        // Test 2
        {
            var errors = parser.ParseArgs(new[] { "-b", "true" }, out var values);
            Assert.AreEqual(0, errors.Count(), "2");
            Assert.IsTrue(values.ContainsKey("BOOL"), "2");
            Assert.IsTrue(values.GetBool("BOOL"), "2");
        }
        // Test 3
        {
            var errors = parser.ParseArgs(new[] { "-b", "false" }, out var values);
            Assert.AreEqual(0, errors.Count(), "3");
            Assert.IsTrue(values.ContainsKey("BOOL"), "3");
            Assert.IsFalse(values.GetBool("BOOL"), "3");
        }
        // Test 4
        {
            var errors = parser.ParseArgs(new[] { "-b", "sdafsd" }, out var values);
            Assert.AreEqual(1, errors.Count(), "4");
            Assert.IsFalse(values.ContainsKey("BOOL"), "4");
            Assert.AreEqual(ValidationErrorType.IncorrectType, errors.First().ErrorType, "4");
            Assert.AreEqual("sdafsd", errors.First().Value, "4");
        }
    }


    [TestMethod]
    public void OptionsTest()
    {
        var parser = new ArgumentParser();
        parser.Describe("OPT", new[] { "-o" }, "option", "option detail", ArgumentParser.ArgumentConditions.Options, null, null, null, new[] { "A", "B", "C" });

        // Test 1
        {
            var errors = parser.ParseArgs(new[] { "-o" }, out var values);
            Assert.AreEqual(1, errors.Count(), "1");
            Assert.IsFalse(values.ContainsKey("OPT"), "1");
            Assert.AreEqual(ValidationErrorType.NoValue, errors.First().ErrorType, "1");
        }
        // Test 2
        {
            var errors = parser.ParseArgs(new[] { "-o", "A" }, out var values);
            Assert.AreEqual(0, errors.Count(), "2");
            Assert.IsTrue(values.ContainsKey("OPT"), "A");
            Assert.AreEqual("A", values["OPT"]);
        }
        // Test 3
        {
            var errors = parser.ParseArgs(new[] { "-o", "D" }, out var values);
            Assert.AreEqual(1, errors.Count(), "3");
            Assert.IsFalse(values.ContainsKey("OPT"), "3");
            Assert.AreEqual(ValidationErrorType.UnknownOption, errors.First().ErrorType);
            Assert.AreEqual("D", errors.First().Value);
        }
        // Test 4
        {
            var errors = parser.ParseArgs(new string[0], out var values);
            Assert.AreEqual(0, errors.Count(), "4");
            Assert.IsFalse(values.ContainsKey("OPT"), "4");
        }
    }

    [TestMethod]
    public void ConflictingTest()
    {
        var parser = new ArgumentParser();
        var ARG_THIS = parser.Describe("THIS", new[] { "--this" }, "This", "This detail", ArgumentParser.ArgumentConditions.IsFlag, null, new[] { new ArgumentKey("THAT") });
        var ARG_THAT = parser.Describe("THAT", new[] { "--that" }, "That", "That detail", ArgumentParser.ArgumentConditions.IsFlag, null, new[] { ARG_THIS });

        // Test 1
        {
            var errors = parser.ParseArgs(new[] { "--this" }, out var values);
            Assert.AreEqual(0, errors.Count(), "1");
            Assert.IsTrue(values.ContainsKey("THIS"), "1");
        }
        // Test 2
        {
            var errors = parser.ParseArgs(new[] { "--that" }, out var values);
            Assert.AreEqual(0, errors.Count(), "2");
            Assert.IsTrue(values.ContainsKey("THAT"), "2");
        }
        // Test 3
        {
            var errors = parser.ParseArgs(new[] { "--this" , "--that" }, out var values);
            Assert.AreEqual(2, errors.Count(), "3");
            Assert.AreEqual(ValidationErrorType.ConflictingArgPresent, errors.First().ErrorType, "3");
            Assert.AreEqual(ValidationErrorType.ConflictingArgPresent, errors.Skip(1).First().ErrorType, "3");
        }
        // Test 4
        {
            var errors = parser.ParseArgs(new[] { "--that", "--this" }, out var values);
            Assert.AreEqual(2, errors.Count(), "4");
            Assert.AreEqual(ValidationErrorType.ConflictingArgPresent, errors.First().ErrorType, "4");
            Assert.AreEqual(ValidationErrorType.ConflictingArgPresent, errors.Skip(1).First().ErrorType, "4");
        }
    }
}