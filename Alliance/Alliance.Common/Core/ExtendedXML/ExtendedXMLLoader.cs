using Alliance.Common.Core.ExtendedXML.Models;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.ExtendedXML
{
	/// <summary>
	/// Initializer for our custom XML files.
	/// </summary>
	public class ExtendedXMLLoader
	{
        /// <summary>
        /// Load our custom XML.
        /// </summary>
        public static void Init()
        {
            MBObjectManager.Instance.RegisterType<ExtendedCharacter>("CharacterExtended", "CharactersExtended", 2001, true, false);
            MBObjectManager.Instance.RegisterType<ExtendedItem>("ItemExtended", "ItemsExtended", 2002, true, false);

            bool isServer = GameNetwork.IsServer;
            if (isServer)
            {
                CopyXSDs();
                MBObjectManager.Instance.LoadXML("CharactersExtended", false);
                MBObjectManager.Instance.LoadXML("ItemsExtended", false);
            }
            else
            {
                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
                string rootPath = Path.GetFullPath(Path.Combine(assemblyPath, "..", ".."));

                string charXml = Path.Combine(rootPath, "ModuleData", "CharactersExtended", "nat_characters_ex.xml");
                string charXsd = Path.Combine(rootPath, "XmlSchemas", "CharactersExtended.xsd");

                string itemXml = Path.Combine(rootPath, "ModuleData", "ItemsExtended", "Native", "nat_items_ex.xml");
                string itemXsd = Path.Combine(rootPath, "XmlSchemas", "ItemsExtended.xsd");

                Game.Current.ObjectManager.LoadOneXmlFromFile(charXml, charXsd, false);
                Game.Current.ObjectManager.LoadOneXmlFromFile(itemXml, itemXsd, false);
            }
        }

        // Game requires the XSD to be in Mount & Blade II Bannerlord/XmlSchemas to load custom schemas
        public static void CopyXSDs()
        {
            string moduleFullPath = ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName);
            string schemaPath = Path.Combine(moduleFullPath, "XmlSchemas");

            foreach (string file in Directory.GetFiles(schemaPath))
            {
                string target = Path.GetFullPath(Path.Combine(moduleFullPath, "..", "..", "XmlSchemas", Path.GetFileName(file)));
                File.Copy(file, target, true);
            }
        }

		// Test to auto generate XML
		private static void InitializeXML()
		{
			string moduleFullPath = ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName);
			XmlDocument xmlDoc = new();
			XmlElement mpCharacters = xmlDoc.CreateElement("MPCharacters");
			List<BasicCharacterObject> gameCharacters = MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>();

			foreach (BasicCharacterObject character in gameCharacters)
			{
				XmlElement extendedCharacterNode = xmlDoc.CreateElement("CharacterExtended");

				XmlAttribute characterAttribute = xmlDoc.CreateAttribute("id");
				characterAttribute.Value = "NPCCharacter." + character.StringId;
				extendedCharacterNode.Attributes.Append(characterAttribute);

				XmlAttribute troopLimitAttribute = xmlDoc.CreateAttribute("troop_limit");
				troopLimitAttribute.Value = "1000";
				extendedCharacterNode.Attributes.Append(troopLimitAttribute);

				mpCharacters.AppendChild(extendedCharacterNode);
			}

			XmlElement CharactersExtended = xmlDoc.CreateElement("CharactersExtended");
			CharactersExtended.InnerXml = mpCharacters.InnerXml;
			xmlDoc.AppendChild(CharactersExtended);

			xmlDoc.Save(moduleFullPath + "/ModuleData/CharactersExtended.xml");
		}
	}
}
