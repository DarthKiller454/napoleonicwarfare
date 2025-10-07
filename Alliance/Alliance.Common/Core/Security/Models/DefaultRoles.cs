using System;
using System.Collections.Generic;

namespace Alliance.Common.Core.Security.Models
{
    /// <summary>
    /// WIP - Roles are mostly placeholders for now.
    /// Serializable to allow direct update from the XML file on the server side.
    /// </summary>
    [Serializable]
    public class DefaultRoles
    {
        public List<Player> Commanders;

        public List<Player> Officers;

        public List<Player> Admins;

        public List<Player> Devs;

        public List<Player> Moderators;

        public List<Player> Banned;

        public List<Player> Muted;

        public DefaultRoles()
        {
            Commanders = new List<Player>();
            Officers = new List<Player>();
            Admins = new List<Player>();
            Devs = new List<Player>();
            Moderators = new List<Player>();
            Banned = new List<Player>();
            Muted = new List<Player>();
        }

        public void Init()
        {
            Commanders = new List<Player>
            {
                new Player("PUTYOURNAMEPLUSCLANTAGHERE", "2.0.0.76561198029168056")
            };
            Officers = new List<Player>
            {
                new Player("PUTYOURNAMEPLUSCLANTAGHERE", "2.0.0.76561198029168056")
            };
            Admins = new List<Player>()
            {
                new Player("PUTYOURNAMEPLUSCLANTAGHERE", "2.0.0.76561198029168056")
            };
            Devs = new List<Player>()
            {
                new Player("PUTYOURNAMEPLUSCLANTAGHERE", "2.0.0.76561198029168056")
            };
        }
    }
}