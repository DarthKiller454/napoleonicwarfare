using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Xml;
using TaleWorlds.ModuleManager;

namespace Alliance.Common.Extensions.SapperLogics
{
    /// <summary>
    /// Discovers sapper building prefabs from the module's Prefabs XML files.
    /// Add a top-level game_entity whose name starts with nwf_sapperbuilding_ and has a root MissionObject
    /// script to make it available for networked placement.
    /// </summary>
    public static class SapperBuildingCatalog
    {
        public const string PrefabPrefix = "nwf_sapperbuilding_";

        private static readonly object SyncRoot = new object();
        private static List<string> _prefabIds;
        private static Dictionary<string, int> _buildCosts;

        public static IReadOnlyList<string> GetPrefabIds()
        {
            lock (SyncRoot)
            {
                if (_prefabIds == null)
                {
                    LoadPrefabCatalog();
                }

                return _prefabIds.ToArray();
            }
        }

        public static bool ContainsPrefab(string prefabId)
        {
            return !string.IsNullOrWhiteSpace(prefabId) &&
                   GetPrefabIds().Contains(prefabId, StringComparer.OrdinalIgnoreCase);
        }

        public static int GetBuildCost(string prefabId)
        {
            try
            {
                lock (SyncRoot)
                {
                    if (_prefabIds == null)
                    {
                        LoadPrefabCatalog();
                    }

                    return !string.IsNullOrWhiteSpace(prefabId) &&
                           _buildCosts.TryGetValue(prefabId, out int cost) && cost > 0
                        ? cost
                        : 1;
                }
            }
            catch
            {
                return 1;
            }
        }

        private static void LoadPrefabCatalog()
        {
            var prefabCosts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                string modulePath = ModuleHelper.GetModuleFullPath(Alliance.Common.SubModule.CurrentModuleName);
                string prefabDirectory = Path.Combine(modulePath, "Prefabs");
                if (!Directory.Exists(prefabDirectory))
                {
                    _prefabIds = new List<string>();
                    _buildCosts = prefabCosts;
                    return;
                }

                foreach (string prefabFile in Directory.GetFiles(prefabDirectory, "*.xml", SearchOption.AllDirectories))
                {
                    try
                    {
                        XmlDocument document = new XmlDocument();
                        document.Load(prefabFile);

                        XmlElement root = document.DocumentElement;
                        if (root == null)
                        {
                            continue;
                        }

                        foreach (XmlNode node in root.ChildNodes)
                        {
                            if (node.NodeType != XmlNodeType.Element ||
                                !string.Equals(node.Name, "game_entity", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            string prefabId = node.Attributes?["name"]?.Value;
                            XmlNode rootMissionObjectScript = node.SelectSingleNode("./scripts/script");
                            if (!string.IsNullOrWhiteSpace(prefabId) &&
                                prefabId.StartsWith(PrefabPrefix, StringComparison.OrdinalIgnoreCase) &&
                                rootMissionObjectScript != null)
                            {
                                prefabCosts[prefabId] = GetBuildCostFromPrefabNode(node);
                            }
                        }
                    }
                    catch
                    {
                        // One malformed prefab file should not hide the rest of the catalog.
                    }
                }
            }
            catch
            {
                // Keep the menu usable (with its Cancel entry) if module files are unavailable.
            }

            _buildCosts = prefabCosts;
            _prefabIds = prefabCosts.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static int GetBuildCostFromPrefabNode(XmlNode prefabNode)
        {
            try
            {
                // Descendant selection intentionally includes tags on child collision entities,
                // as tags are commonly placed on the prefab's physical child rather than its root.
                XmlNodeList tags = prefabNode.SelectNodes(".//tags/tag");
                if (tags == null)
                {
                    return 1;
                }

                foreach (XmlNode tag in tags)
                {
                    string tagName = tag.Attributes?["name"]?.Value;
                    if (string.IsNullOrWhiteSpace(tagName) ||
                        !tagName.StartsWith("sapper_cost_", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string rawCost = tagName.Substring("sapper_cost_".Length);
                    if (int.TryParse(rawCost, NumberStyles.Integer, CultureInfo.InvariantCulture, out int cost) && cost > 0)
                    {
                        return cost;
                    }
                }
            }
            catch
            {
                // A malformed/missing cost tag falls back to the safe one-point cost.
            }

            return 1;
        }
    }
}
