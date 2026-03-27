using System.Collections.Generic;
using System.Linq;

namespace Agents
{
    public struct PlayerControlDto
    {
        public string Name;
        public string BindMap;
        public string[] Devices;
        
        public PlayerControlDto(string playerName, Dictionary<string, List<string>> binds, List<string> devices)
        {
            Name = playerName;
            Devices = devices.ToArray();
            BindMap = string.Join(";", binds.Select(kvp => $"{kvp.Key}:{string.Join(",", kvp.Value)}"));
        }
    }
}
