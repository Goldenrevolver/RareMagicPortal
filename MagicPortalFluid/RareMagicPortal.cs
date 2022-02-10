﻿// MagicPortalFluid
// a Valheim mod created by WackyMole. Do whatever with it. - WM
// assets from https://assetstore.unity.com/packages/3d/props/interior/free-alchemy-and-magic-pack-142991
// 
// File:    MagicPortalFluid.cs
// Project: MagicPortalFluid

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.GUI;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Logger = Jotunn.Logger;
using HarmonyLib;
using RareMagicPortal;
//using PieceManager;
//using ServerSync;


namespace RareMagicPortal
{
	//extra
	[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
	[BepInDependency(Jotunn.Main.ModGuid)]
	[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
	internal class MagicPortalFluid : BaseUnityPlugin
	{
		public const string PluginGUID = "WackyMole.RareMagicPortal";
		public const string PluginName = "RareMagicPortal";
		public const string PluginVersion = "1.3.0";
        

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        //public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private AssetBundle portalmagicfluid;
		private CustomLocalization Localization;
		private static MagicPortalFluid context;
		public static ConfigEntry<bool> modEnabled;
		public static ConfigEntry<bool> isDebug;
		public static bool firstTime = false;
		public static ConfigEntry<int> nexusID;
		private static List<RecipeData> recipeDatas = new List<RecipeData>();
		private static string assetPath;
		public static int PortalMagicFluidSpawn = 3; // default
		public static bool DisablePortalJuice = false; // don't disable
		public static string TabletoAddTo;
		public static string DefaultTable = "$piece_workbench";
		public static bool piecehaslvl = false;
		public static string PiecetoLookFor = "portal_wood"; //name
		public static string PieceTokenLookFor = "$piece_portal"; //m_name
		public static int CraftingStationlvl = 1;
		public static Vector3 tempvalue;
		//public static string CraftingStationName;

		private ConfigEntry<bool> ConfigFluid;
        private ConfigEntry<int> ConfigSpawn;
		private ConfigEntry<string> ConfigTable;
		private ConfigEntry<int> ConfigTableLvl;

		[HarmonyPatch(typeof(ZNetScene), "Awake")]
		[HarmonyPriority(0)]
		private static class ZNetScene_Awake_Patch
		{
			private static void Postfix()
			{
				{
					//((MonoBehaviour)(object)context).StartCoroutine(DelayedLoadRecipes());
					LoadAllRecipeData(reload: true); // while loading on world screen
				}
			}
		}

		[HarmonyPatch(typeof(Game), "SpawnPlayer")]
		private static class Game_OnNewCharacterDone_Patch
		{
			[HarmonyPostfix]
			private static void Postfix()
			{
				{
					StartingitemPrefab();
				}
			}
		}

		[HarmonyPatch(typeof(FejdStartup), "OnNewCharacterDone")]
		private static class FejdStartup_OnNewCharacterDone_Patch
		{
			private static void Postfix()
			{
				StartingFirsttime();

			}

		}
		/*

		[HarmonyPatch(typeof(Player), "HaveRequirements")]
		[HarmonyPatch(new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
		private static class Player_HaveRequirementsLvl_Patch
		{
			[HarmonyPrefix]
			static bool Prefix(Piece piece, ref Player __instance,  out Vector3 __state )
			{	
				if (__instance.transform.position != null)
					__state = __instance.transform.position; // save position //must be assigned
				else
					__state = new Vector3(0, 0, 0); // just in case
  
				return true;

			}
			[HarmonyPostfix]
			 static bool Postfix(bool __result, Piece piece, Player.RequirementMode mode, Vector3 __state )
			{
				{
					if (__result)// Only care if true for specific piece
                    {
                        if (piece.name == PiecetoLookFor) // portal
						{
							var paulstation = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, __state);
							var paullvl = paulstation.GetLevel();
							//Jotunn.Logger.LogInfo(paullvl + " " + CraftingStationlvl + " station name " +paulstation);


							if (paullvl+1 > CraftingStationlvl) // just for testing
                            {
								piecehaslvl = true;
								return __result;
                            }
							return __result;
						}
						return __result;
                    }
					return __result; // returns result but checks lvl with piecehaslvl
				}
			}
		}
		*/

		[HarmonyPatch(typeof(Player), "PlacePiece")]
		private static class Player_MessageforPortal_Patch
        {
			[HarmonyPrefix]
			private static bool Prefix(ref Player __instance, ref Piece piece)

			{
				if (piece == null) return true;

				if (piece.name == PiecetoLookFor && !__instance.m_noPlacementCost) // portal
				{
					if (__instance.transform.position != null)
						tempvalue = __instance.transform.position; // save position //must be assigned
					else
						tempvalue = new Vector3(0, 0, 0); // shouldn't ever be called 

					var paulstation = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, tempvalue);
					var paullvl = paulstation.GetLevel();

					if (paullvl + 1 > CraftingStationlvl) // just for testing
					{
						piecehaslvl = true;
					}
					else
					{
						__instance.Message(MessageHud.MessageType.Center, "Need a Level " + CraftingStationlvl + " " + piece.m_craftingStation.name + " for placement");
						piecehaslvl = false;
						return false;
					}
				}
				return true;
			}
					
        }
		
		

		private void Awake()
		{
			CreateConfigValues();
			ReadAndWriteConfigValues();
			LoadAssets();
			itemModCreation();


			context = this;

			assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(MagicPortalFluid).Namespace);
			Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), (string)null);

			AddLocalizations();


			Jotunn.Logger.LogInfo("MagicPortalFluid has loaded start assets");

		}


		// end startup

		private void LoadAssets()
		{
			portalmagicfluid = AssetUtils.LoadAssetBundleFromResources("portalmagicfluid", typeof(MagicPortalFluid).Assembly);
			//Jotunn.Logger.LogInfo($"Embedded resources: {string.Join(",", typeof(MagicPortalFluid).Assembly.GetManifestResourceNames())}");

		}

		private void UnLoadAssets()
		{
			portalmagicfluid.Unload(false);
		}

		private void itemModCreation()
		{
			var magicjuice_prefab = portalmagicfluid.LoadAsset<GameObject>("PortalMagicFluid");
			var portaljuice = new CustomItem(magicjuice_prefab, fixReference: false);
			ItemManager.Instance.AddItem(portaljuice);

		}

		// changing portals section

		// JVL changer
		private static void PortalChanger()
		{
			var peter = PrefabManager.Instance.GetPrefab("portal_wood"); // this is iffy // JVL			

			// GameObject peter = GetPieces().Find((GameObject g) => Utils.GetPrefabName(g) == "portal_wood"); //item prefab loaded from hammer												 


			List <Piece.Requirement> requirements = new List<Piece.Requirement>();
				requirements.Add(new Piece.Requirement
				{
					m_amount = 20,
					m_resItem = ObjectDB.instance.GetItemPrefab("FineWood").GetComponent<ItemDrop>(),
					m_recover = true
				});
				if (!DisablePortalJuice) { // make this more dynamic
					requirements.Add(new Piece.Requirement
					{
						m_amount = 1,
						m_resItem = ObjectDB.instance.GetItemPrefab("PortalMagicFluid").GetComponent<ItemDrop>(),
						m_recover = true
					});
				}
				requirements.Add(new Piece.Requirement
				{
					m_amount = 10,
					m_resItem = ObjectDB.instance.GetItemPrefab("GreydwarfEye").GetComponent<ItemDrop>(),
					m_recover = true
				});
				requirements.Add(new Piece.Requirement
				{
					m_amount = 2,
					m_resItem = ObjectDB.instance.GetItemPrefab("SurtlingCore").GetComponent<ItemDrop>(),
					m_recover = true
				});

			var CraftingStationforPaul = GetCraftingStation(TabletoAddTo);
			if (CraftingStationforPaul == null)
            {
				CraftingStationforPaul.m_name = DefaultTable;
            }

			//paul.GetComponent<Piece>().m_resources = requirements.ToArray();


			//var joshy = GameObject.Instantiate(p)
			//GameObject peter = GetPieces().Find((GameObject g) => Utils.GetPrefabName(g) == "portal_wood"); //item prefab loaded from hammer
			//  james =  GetRecipeDataByName("portal_wood");
			//GameObject  john =  GetPieces().Find((GameObject g) => Utils.GetPrefabName(g) == james.name);
			//john.GetComponent<Piece>().m_craftingStation = GetCraftingStation(james.craftingStation);
			//john.GetComponent<Piece>().m_resources = requirements.ToArray();
			//var risky = PrefabManager.Instance.GetPrefab("forge_ext3").AddComponent<CraftingStation>();
			//risky.m_name = "paulshome";

			Piece petercomponent = peter.GetComponent<Piece>();
			petercomponent.m_craftingStation = GetCraftingStation(CraftingStationforPaul.m_name); // sets crafting station workbench/forge /ect
			petercomponent.m_resources = requirements.ToArray();
			
			


		}

		private static void StartingFirsttime()
		{
			firstTime = true;

		}

		private static void StartingitemPrefab()
		{

			if (firstTime && PortalMagicFluidSpawn != 0)
			{
				Jotunn.Logger.LogInfo("New Starting Item Set");
				Inventory inventory = ((Humanoid)Player.m_localPlayer).m_inventory;
				inventory.AddItem("PortalMagicFluid", PortalMagicFluidSpawn, 1, 0, 0L, "");
				firstTime = false;

			}
		}


		public static void Dbgl(string str = "", bool pref = true)
		{
			if (false) // debug
			{
				Debug.Log((pref ? (typeof(MagicPortalFluid).Namespace + " ") : "") + str);
			}
		}

		public static IEnumerator DelayedLoadRecipes()
		{
			yield return null;
			LoadAllRecipeData(reload: true);
		}

		private static void LoadAllRecipeData(bool reload)
		{
			if (reload) // waits until the last seconds to reference and overwrite
			{
				PortalChanger();
			}
		}

		private static CraftingStation GetCraftingStation(string name)
		{
			if (name == "")
			{
				return null;
			}
			foreach (Recipe recipe in ObjectDB.instance.m_recipes)
			{
				if (recipe?.m_craftingStation?.m_name == name)
				{
					//Jotunn.Logger.LogMessage("got crafting station " + name);
					return recipe.m_craftingStation;
				}
			}
			return null;
		}

		private static List<GameObject> GetPieces()
		{
			List<GameObject> list = new List<GameObject>();
			if (!ObjectDB.instance)
			{
				return list;
			}
			ItemDrop itemDrop = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();
			if ((bool)itemDrop)
			{
				list.AddRange(Traverse.Create((object)itemDrop.m_itemData.m_shared.m_buildPieces).Field("m_pieces").GetValue<List<GameObject>>());
			}
			ItemDrop itemDrop2 = ObjectDB.instance.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
			if ((bool)itemDrop2)
			{
				list.AddRange(Traverse.Create((object)itemDrop2.m_itemData.m_shared.m_buildPieces).Field("m_pieces").GetValue<List<GameObject>>());
			}
			return list;
		}

		private static RecipeData GetRecipeDataByName(string name)
		{
			GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
			if (itemPrefab == null)
			{
				return GetPieceRecipeByName(name);
			}
			ItemDrop.ItemData itemData = itemPrefab.GetComponent<ItemDrop>().m_itemData;
			if (itemData == null)
			{
				Dbgl("Item data not found!");
				return null;
			}
			Recipe recipe = ObjectDB.instance.GetRecipe(itemData);
			if (!recipe)
			{
				Dbgl("Recipe not found!");
				return null;
			}
			RecipeData recipeData = new RecipeData
			{
				name = name,
				amount = recipe.m_amount,
				craftingStation = (recipe.m_craftingStation?.m_name ?? ""),
				minStationLevel = recipe.m_minStationLevel
			};
			Piece.Requirement[] resources = recipe.m_resources;
			foreach (Piece.Requirement requirement in resources)
			{
				recipeData.reqs.Add($"{Utils.GetPrefabName(requirement.m_resItem.gameObject)}:{requirement.m_amount}:{requirement.m_amountPerLevel}:{requirement.m_recover}");
			}
			return recipeData;
		}

		private static RecipeData GetPieceRecipeByName(string name)
		{
			GameObject gameObject = GetPieces().Find((GameObject g) => Utils.GetPrefabName(g) == name);
			if (gameObject == null)
			{
				Dbgl("Item " + name + " not found!");
				return null;
			}
			Piece component = gameObject.GetComponent<Piece>();
			if (component == null)
			{
				Dbgl("Item data not found!");
				return null;
			}
			RecipeData recipeData = new RecipeData
			{
				name = name,
				amount = 1,
				craftingStation = (component.m_craftingStation?.m_name ?? ""),
				minStationLevel = 1
			};
			Piece.Requirement[] resources = component.m_resources;
			foreach (Piece.Requirement requirement in resources)
			{
				recipeData.reqs.Add($"{Utils.GetPrefabName(requirement.m_resItem.gameObject)}:{requirement.m_amount}:{requirement.m_amountPerLevel}:{requirement.m_recover}");
			}
			return recipeData;
		}

		// Adds hardcoded localizations
		private void AddLocalizations()
		{
			// Create a custom Localization instance and add it to the Manager
			Localization = new CustomLocalization();
			LocalizationManager.Instance.AddLocalization(Localization);

			// Add translations for our custom skill
			Localization.AddTranslation("English", new Dictionary<string, string>
			{
				{"portalmagicfluid", "Magical Portal Fluid" }, {"portalmagicfluid_description", "Once a mythical essence, now made real with Odin's blessing"}
			});

		}

		private void CreateConfigValues()
		{
			Config.SaveOnConfigSet = true;


			// Add server config which gets pushed to all clients connecting and can only be edited by admins
			// In local/single player games the player is always considered the admin

			ConfigFluid = Config.Bind("Server config", "DisablePortalJuice", false,
							new ConfigDescription("Disable PortalFluid requirement?", null,
								new ConfigurationManagerAttributes { IsAdminOnly = true }));

			ConfigSpawn = Config.Bind("Server config", "PortalMagicFluidSpawn", 3,
				new ConfigDescription("How much PortalMagicFluid to start with on a new character?", null,
					new ConfigurationManagerAttributes { IsAdminOnly = true }));

			ConfigTable = Config.Bind("Server config", "CraftingStation_Requirement", DefaultTable,
				new ConfigDescription("Which CraftingStation is required nearby?" + System.Environment.NewLine + "Default is Workbench = $piece_workbench, forge = $piece_forge, Artisan station = $piece_artisanstation "  + System.Environment.NewLine + "Pick a valid table otherwise default is workbench", null,
					new ConfigurationManagerAttributes { IsAdminOnly = true })); // $piece_workbench , $piece_forge , $piece_artisanstation

			ConfigTableLvl = Config.Bind("Server config", "Level_of_CraftingStation_Req", 1,
				new ConfigDescription("What level of CraftingStation is required for placing Portal?", null,
					new ConfigurationManagerAttributes { IsAdminOnly = true }));



			// You can subscribe to a global event when config got synced initially and on changes
			SynchronizationManager.OnConfigurationSynchronized += (obj, attr) =>
			{
				if (attr.InitialSynchronization)
				{
					Jotunn.Logger.LogMessage("Initial Config sync event received for PortalMagic");
					PortalMagicFluidSpawn = ConfigSpawn.Value; // no update needed as first time char creation
					CraftingStationlvl = ConfigTableLvl.Value; // checked at every event
					if (DisablePortalJuice != ConfigFluid.Value || TabletoAddTo != ConfigTable.Value)
					{
						TabletoAddTo = ConfigTable.Value;
						DisablePortalJuice = ConfigFluid.Value; // to late to change spawn amount now
						PortalChanger();
						Jotunn.Logger.LogMessage("Is portal Fluid disabled?: " + DisablePortalJuice + "amount of Starting Fluid Set: "+ PortalMagicFluidSpawn);
					}
				}
				else
				{
					Jotunn.Logger.LogMessage("Config sync event received for PortalMagic");
					CraftingStationlvl = ConfigTableLvl.Value; // checked at every event
					if (DisablePortalJuice != ConfigFluid.Value || TabletoAddTo != ConfigTable.Value)
                    {
						DisablePortalJuice = ConfigFluid.Value; // too late to change spawn amount now
						PortalChanger();
						Jotunn.Logger.LogMessage("Trying to change Portal Requirements mid game");

					}
				}
			};
		}

		private void ReadAndWriteConfigValues()
		{
			DisablePortalJuice = (bool)Config["Server config", "DisablePortalJuice"].BoxedValue;
			PortalMagicFluidSpawn = (int)Config["Server config", "PortalMagicFluidSpawn"].BoxedValue;
			TabletoAddTo = (string)Config["Server config", "CraftingStation_Requirement"].BoxedValue;
			CraftingStationlvl = (int)Config["Server config", "Level_of_CraftingStation_Req"].BoxedValue;
			if (CraftingStationlvl > 10 || CraftingStationlvl < 1)
				CraftingStationlvl = 1;
			Jotunn.Logger.LogInfo("Configs changed PortalMagic");



		}

	}
	// end of namespace class

}
