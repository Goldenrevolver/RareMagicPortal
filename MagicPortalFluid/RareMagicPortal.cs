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
using ServerSync;


namespace RareMagicPortal
{
	[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
	[BepInDependency(Jotunn.Main.ModGuid)]
	[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
	internal class MagicPortalFluid : BaseUnityPlugin
	{
		public const string PluginGUID = "WackyMole.RareMagicPortal";
		public const string PluginName = "RareMagicPortal";
		public const string PluginVersion = "1.2.0";

		// Use this class to add your own localization to the game
		// https://valheim-modding.github.io/Jotunn/tutorials/localization.html
		//public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

		// setting up for configSync
		ServerSync.ConfigSync configSync = new ServerSync.ConfigSync(PluginGUID) { DisplayName = PluginName, CurrentVersion = PluginVersion, MinimumRequiredVersion = "1.2.0" };
		private readonly Harmony _harmony = new Harmony(PluginGUID);


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
		public static bool FluidYesorNo = false; //default



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

		private void Awake()
		{
			_serverConfigLocked = Config.Bind("General", "Force Server Config", true, "Force Server Config");
			_ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked); // kind of weird - It's serversync code to add the _serverConfigLocked variable as the boolean that determines if you should have a locked config or not.

			CreateConfigValues();
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
			if (!FluidYesorNo) { 
				var paul = PrefabManager.Instance.GetPrefab("portal_wood"); // this is iffy // JVL
																			//GameObject peter = GetPieces().Find((GameObject g) => Utils.GetPrefabName(g) == "portal_wood"); // better, but not instanced
				List<Piece.Requirement> requirements = new List<Piece.Requirement>();
				requirements.Add(new Piece.Requirement
				{
					m_amount = 20,
					m_resItem = ObjectDB.instance.GetItemPrefab("FineWood").GetComponent<ItemDrop>(),
					m_recover = true
				});
				requirements.Add(new Piece.Requirement
				{
					m_amount = 1,
					m_resItem = ObjectDB.instance.GetItemPrefab("PortalMagicFluid").GetComponent<ItemDrop>(),
					m_recover = true
				});
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

				paul.GetComponent<Piece>().m_resources = requirements.ToArray();
			}
		}

		private static void StartingFirsttime()
		{
			firstTime = true;

		}
		private static void StartingitemPrefab()
		{

			if (firstTime && !FluidYesorNo && PortalMagicFluidSpawn != 0)
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
		public static ConfigEntry<bool>?
		  _serverConfigLocked; // Needed bind for ServerSync's "Force Server Config" config bind.
		private static ConfigEntry<int>? _FluidAmountStart;
		private static ConfigEntry<bool>? _DisableMod;

		private void CreateConfigValues()
        {  
			_DisableMod = config("Synced With Server", "FluidYesorNo", false,
                "Disable PortalFluid requirement??");

			_FluidAmountStart = config("Synced With Server", "PortalMagicFluidSpawn", 3,
                "How much PortalMagicFluid to start with on a new character?");

		
		}

		private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);       

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }


	}

}// end of namespace class
