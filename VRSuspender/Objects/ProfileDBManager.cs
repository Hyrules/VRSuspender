using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRSuspender.Objects
{
    public static class ProfileDBManager
    {
        private static List<TrackedProcess> listProfiles;
        static ProfileDBManager()
        {
            ListProfiles = new();
            LoadProfiles();
        }

        public static List<TrackedProcess> ListProfiles { get => listProfiles; private set => listProfiles = value; }

        private static void LoadProfiles()
        {
            string path = Path.GetDirectoryName(Environment.ProcessPath);
            try
            {
                StreamReader sr = new(path + "\\profilesdb.json");
                string json = sr.ReadToEnd();
                ListProfiles = JsonConvert.DeserializeObject<List<TrackedProcess>>(json).OrderBy(x => x.ProfileName).ToList();
                
            }
            catch (Exception)
            {
                ListProfiles = new List<TrackedProcess>();
                
            }
        }

        public static void RefreshProfiles()
        {
            LoadProfiles();
        }
    }
}
