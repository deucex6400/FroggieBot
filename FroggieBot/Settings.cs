using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FroggieBot
{
    public class Settings
    {
        public string LoopringApiKey { get; set; }
        public string LoopringPrivateKey { get; set; }
        public string LoopringAddress { get; set; }
        public int LoopringAccountId { get; set; }
        public string LoopringTokenAddress { get; set; }
        public string Exchange { get; set; }
        public string MetamaskPrivateKey { get; set; }
        public string SqlServerConnectionString { get; set; }
        public string EtherScanApiKey { get; set; }
        public ulong DiscordServerId { get; set; }
    }
}
