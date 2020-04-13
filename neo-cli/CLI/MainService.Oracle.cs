using Neo.ConsoleService;

namespace Neo.CLI
{
    partial class MainService
    {
        /// <summary>
        /// Process "start oracle" command
        /// </summary>
        [ConsoleCommand("start oracle", Category = "Oracle Commands")]
        private void OnStartOracleCommand()
        {
            if (NoWallet()) return;
            ShowPrompt = false;
            NeoSystem.StartOracle(CurrentWallet);
        }
    }
}
