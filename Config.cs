using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Whispbot
{
    public static class Config
    {
        public static bool isDev => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public class Auth
    {
        public Guid id;
        public string user_id = "";
        public string username = "";
    }
}
