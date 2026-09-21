using NUnit.Framework;
using Unity.Netcode;

namespace DocumentationCodeSamples
{
    internal class CommandLineOptionsDocsTests
    {
        #region DefineAndRead
        private const string k_OverrideArg = "-argName";

        private bool ParseCommandLineOptions(out string command)
        {
            if (CommandLineOptions.TryGetArg(k_OverrideArg, out var argValue))
            {
                command = argValue;
                return true;
            }
            command = default;
            return false;
        }
        #endregion

        private string CommandLineUsage()
        {
            #region Usage
            if (ParseCommandLineOptions(out var command))
            {
                // Your logic here
            }
            #endregion

            return command;
        }

        [Test]
        public void TestCommandLineUsage()
        {
            // This is a compile test.
            var succeeded = ParseCommandLineOptions(out var command);
            Assert.NotNull(succeeded);

            var output = CommandLineUsage();
            Assert.AreEqual(command, output);
        }
    }
}
