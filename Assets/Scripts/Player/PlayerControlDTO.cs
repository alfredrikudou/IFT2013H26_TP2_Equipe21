using System.Collections.Generic;
using System.Linq;

namespace Player
{
    public struct PlayerControlDTO
    {
        public string Name;
        public string BindMap;
        public string[] Devices;
        
        public PlayerControlDTO(string playerName, Dictionary<string, List<string>> binds, List<string> devices)
        {
            Name = playerName;
            Devices = devices.ToArray();
            BindMap = string.Join(";", binds.Select(kvp => $"{kvp.Key}:{string.Join(",", kvp.Value)}"));
        }
    }
}
