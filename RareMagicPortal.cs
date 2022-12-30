﻿// MagicPortalFluid
// a Valheim mod created by WackyMole. Do whatever with it. - WM
// assets from https://assetstore.unity.com/packages/3d/props/interior/free-alchemy-and-magic-pack-142991
// crystal assets from https://assetstore.unity.com/packages/3d/environments/fantasy/translucent-crystals-106274
//  Thank you https://github.com/redseiko/ComfyMods/tree/main/ColorfulPortals
// 
// File:    MagicPortalFluid.cs
// Project: MagicPortalFluid
// create yml file for portal names, put example file in it and explain that it only does something with EnableCrystals and Consumable is true
// load portal names After every change
// black, yellow (normal), red, green, blue, purple, tan, gold, white
//
//
/*
 * Colors for Portals:
Black = mountain 
Yellow = normal
Red = ashlands
Green = swamp
Brown = blackforest 
Purple = mistlands
Cyan = deepnorth
Orange = plains
Brown = meadows
Gold = master/ endgame
White =endgame/ special
Portal Drink = rainbow mode? Or current white override.


// 1 Yellow // free passage - Maybe add Yellow Crystal and Key
// 2 red
/   
// 4 blue
// 5 Purple
// 6 Brown
// 7 Cyan
// 8 Orange
// 20 Gold // Can be used as mater/engame
// 21 White (Only allow free passage with PortalDrink or enablecrystals)
// 22 Black


101 // Yellow Crystal required this many items
201 Yellow Crystal Grants accesss
301 Yellow $rmp_redKey_access Key Access
999 $rmp_noaccess

PortalColors enum
foreach (var col in enum){
var num = col.enum num
if (Portal_Crystal_Cost["Red"] > 0 || Portal_Key["Red"])
                {
                    if (CrystalCountRed == 0) // has none of required
                        flagCarry = 1;
                    else if (Portal_Crystal_Cost["Red"] > CrystalCountRed) // has less than required
                        flagCarry = 11;
                    else flagCarry = 21; // has more than required

                    if (Portal_Key["Red"])
                    {
                        if (Portal_Crystal_Cost["Red"] == 0)
                        {
                            crystalorkey = 1;
                            if (KeyCountRed > 0)
                                flagCarry = 111;
                            else
                                flagCarry = 1; // no crystal cost, but key cost with no key
                        }
                        else
                        {
                            if (KeyCountRed > 0 && flagCarry < 20)
                                flagCarry = 111;
                            else
                                crystalorkey = 2; // yes crystal cost, and key cost with no key, so let user know both is good
                        }
                    }
                }
                if (flagCarry > 20)
                    foundAccess = true;
                if (flagCarry < 20 && lowest == 0)
                    lowest = flagCarry;

}




*/

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;
using UnityEngine.UI;
using HarmonyLib;
using RareMagicPortal;
//using PieceManager;
using ServerSync;
using ItemManager;
using BepInEx.Logging;
using BepInEx.Bootstrap;
using YamlDotNet;
using YamlDotNet.Serialization;
using LocalizationManager;
using StatusEffectManager;



namespace RareMagicPortal
{
    //extra
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("org.bepinex.plugins.targetportal", BepInDependency.DependencyFlags.SoftDependency)]  // it loads before this mod// not really required, but whatever
    internal class MagicPortalFluid : BaseUnityPlugin
    {
        public const string PluginGUID = "WackyMole.RareMagicPortal";
        public const string PluginName = "RareMagicPortal";
        public const string PluginVersion = "2.5.3";

        internal const string ModName = PluginName;
        internal const string ModVersion = PluginVersion;
        internal const string Author = "WackyMole";
        internal const string ModGUID = Author + "." + ModName;
        internal static string ConfigFileName = PluginGUID + ".cfg";
        internal static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + "WackyMole.RareMagicPortal.cfg";
        internal static string YMLFULL = YMLFULLFOLDER + "World1.yml";
        //internal static string YMLFULLServer = Paths.ConfigPath + Path.DirectorySeparatorChar + "WackyMole" + ".PortalServerNames.yml";
        internal static string YMLFULLFOLDER = Path.Combine(Path.GetDirectoryName(Paths.ConfigPath + Path.DirectorySeparatorChar), "Portal_Names");

        internal static string ConnectionError = "";

        internal readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource RareMagicPortal =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        internal static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = "2.5.2" };

        internal static MagicPortalFluid? plugin;
        internal static MagicPortalFluid context;

        internal AssetBundle portalmagicfluid;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static bool firstTime = false;
        public static ConfigEntry<int> nexusID;
        internal static List<RecipeData> recipeDatas = new List<RecipeData>();
        internal static string assetPath;
        internal static string assetPathyml;
        public static string PiecetoLookFor = "portal_wood"; //name
        public static string PieceTokenLookFor = "$piece_portal"; //m_name
        public static Vector3 tempvalue;
        public static bool loadfilesonce = false;
        public static Dictionary<string, int> Ind_Portal_Consumption;
        public static int CurrentCrystalCount;
        public static bool isAdmin = true;
        public static bool isLocal = true;
        public static string Worldname = "demo_world";
        public static bool LoggingOntoServerFirst = true;
        internal static Dictionary<string, Material> originalMaterials;

        public static bool FluidwasTrue = false;
        public static bool piecehaslvl = false;
        public static string DefaultTable = "$piece_workbench";
        internal static string YMLCurrentFile = Path.Combine(YMLFULLFOLDER, Worldname + ".yml");
        internal static int JustWrote = 0;
        internal static bool JustWait = false;
        internal static int JustSent = 0;
        internal static bool JustRespawn = false;
        internal static bool NoMoreLoading = false;
        internal static bool Teleporting = false;
        internal static int TeleportingforWeight = 0;
        internal static string checkiftagisPortal = null;
        internal static bool JustStop = false;
        internal static bool JustWaitforInventory = true;

        internal static bool m_hadTarget = false;
        internal static List<Minimap.PinData> HoldPins;
        internal static bool Globaliscreator = false;


        internal static ConfigEntry<bool>? ConfigFluid;
        internal static ConfigEntry<int>? ConfigSpawn;
        internal static ConfigEntry<string>? ConfigTable;
        internal static ConfigEntry<int>? ConfigTableLvl;
        internal static ConfigEntry<bool>? ConfigCreator;
        internal static ConfigEntry<float>? ConfiglHealth;
        internal static ConfigEntry<bool>? ConfigCreatorLock;
        internal static ConfigEntry<int>? ConfigFluidValue;
        internal static ConfigEntry<bool>? ConfigEnableCrystalsNKeys;
       // internal static ConfigEntry<bool>? ConfigEnableKeys;
        internal static ConfigEntry<int>? ConfigCrystalsConsumable;
       // internal static ConfigEntry<bool>? ConfigAdminOnly;
        internal static ConfigEntry<string>? CrystalKeyDefaultColor;
        internal static ConfigEntry<int>? PortalDrinkTimer;
        internal static ConfigEntry<bool>? ConfigEnableYMLLogs;
        internal static ConfigEntry<string>? ConfigAddRestricted;
        internal static ConfigEntry<bool>? ConfigEnableGoldAsMaster;
        internal static ConfigEntry<string>? ConfigEnableColorEnable;
        internal static ConfigEntry<KeyboardShortcut>? portalRMPKEY = null!;
        internal static ConfigEntry<bool>? ConfigMessageLeft;
        internal static ConfigEntry<bool>? ConfigTargetPortalAnimation;
        internal static ConfigEntry<int>? ConfigMaxWeight;
        internal static ConfigEntry<bool>? ConfigUseBiomeColors;

        public static string crystalcolorre = ""; // need to reset everytime maybe?
        public string message_eng_NO_Portal = $"Portal Crystals/Key Required"; // Blue Portal Crystal
        public string message_eng_MasterCost = $", Rainbow Crystals Required"; // 3, Master Crystals Required
        public string message_eng_NotCreator = $"";
        public string message_eng_Grants_Acess = $"";
        public string message_eng_Crystal_Consumed = $"";
        public string message_eng_Odins_Kin = $"Only Odin's Kin are Allowed";
        public string message_only_Owner_Can_Change = $"Only the Owner Can change Name";

        public const string CrystalMaster = "$item_PortalCrystalMaster";  // RGB
        public const string CrystalRed = "$item_PortalCrystalRed";
        public const string CrystalGreen = "$item_PortalCrystalGreen";
        public const string CrystalBlue = "$item_PortalCrystalBlue";
        public const string CrystalPurple = "$item_PortalCrystalPurple";
        public const string CrystalTan = "$item_PortalCrystalTan";

        public const string PortalKeyGold = "$item_PortalKeyGold";
        public const string PortalKeyRed = "$item_PortalKeyRed";
        public const string PortalKeyGreen = "$item_PortalKeyGreen";
        public const string PortalKeyBlue = "$item_PortalKeyBlue";
        public const string PortalKeyPurple = "$item_PortalKeyPurple";
        public const string PortalKeyTan = "$item_PortalKeyTan";

        SpriteTools IconColor = new SpriteTools();

        public static Sprite IconBlack = null!;
        public static Sprite IconYellow = null!;
        public static Sprite IconRed = null!;
        public static Sprite IconGreen = null!;
        public static Sprite IconBlue = null!;
        public static Sprite IconGold = null!;
        public static Sprite IconWhite = null!;
        public static Sprite IconDefault = null!;
        public static Sprite IconPurple = null!;
        public static Sprite IconTan = null!;

        internal static Localization english = null!;
        internal static Localization spanish = null!;

        public static CustomSE AllowTeleEverything = new CustomSE("yippeTele");
        public static List<StatusEffect> statusEffectactive;

        internal static readonly List<string> portalPrefabs = new List<string>();


        public static string WelcomeString = "#Hello, this is the Portal yml file. It keeps track of all portals you enter";
        

        static Coroutine myCoroutineRMP;
        public static ItemDrop.ItemData Crystal { get; internal set; }

        internal static readonly int _teleportWorldColorHashCode = "TeleportWorldColorRMP".GetStableHashCode(); // I should probably change this
        internal static readonly int _teleportWorldColorAlphaHashCode = "TeleportWorldColorAlphaRMP".GetStableHashCode();
        internal static readonly int _portalLastColoredByHashCode = "PortalLastColoredByRMP".GetStableHashCode();
        internal static string PortalFluidname;
        internal static bool TargetPortalLoaded = false;

        internal static readonly Dictionary<TeleportWorld, TeleportWorldDataRMP> _teleportWorldDataCache = new();
        static readonly KeyboardShortcut _changePortalReq = new(KeyCode.E, KeyCode.LeftControl);



        static internal Dictionary<string, bool> enaColor = new Dictionary<string, bool>(){
                {"Red", false },
                {"Blue", false },
                {"Green", false },
                {"Gold", false },
                {"Purple",false },
                {"Tan",false }
            };


        static IEnumerator RemovedDestroyedTeleportWorldsCoroutine()
        {
            WaitForSeconds waitThirtySeconds = new(seconds: 30f);
            List<KeyValuePair<TeleportWorld, TeleportWorldDataRMP>> existingPortals = new();
            int portalCount = 0;

            while (true)
            {
                yield return waitThirtySeconds;
                portalCount = _teleportWorldDataCache.Count;

                existingPortals.AddRange(_teleportWorldDataCache.Where(entry => entry.Key));
                _teleportWorldDataCache.Clear();

                foreach (KeyValuePair<TeleportWorld, TeleportWorldDataRMP> entry in existingPortals)
                {
                    _teleportWorldDataCache[entry.Key] = entry.Value;
                }

                existingPortals.Clear();

                if ((portalCount - _teleportWorldDataCache.Count) > 0)
                {
                    RareMagicPortal.LogInfo($"Removed {portalCount - _teleportWorldDataCache.Count}/{portalCount} portal references.");
                }
            }
        }
   

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        [HarmonyPriority(Priority.Last)]
        static class ZNetScene_Awake_PatchWRare
        {
            static void Postfix()
            {
                {
                    Worldname = ZNet.instance.GetWorldName();// for singleplayer  // won't be ready for multiplayer
                    TargetPortalLoaded = Chainloader.PluginInfos.ContainsKey("org.bepinex.plugins.targetportal");

                    RareMagicPortal.LogInfo("Setting MagicPortal Fluid Afterdelay");
                    if (ZNet.instance.IsDedicated() && ZNet.instance.IsServer())
                    {
                        LoadIN();
                    }
                    else // everyone else
                    {

                    }

                }
            }
        }

        [HarmonyPatch(typeof(Game), "SpawnPlayer")]
        internal static class Game_SpawnPreRMP
        {
            [HarmonyPrefix]
            internal static void Prefix()
            {
                {
                    LoadAllRecipeData(reload: true);
                    LoadIN();

                }
            }
        }

        [HarmonyPatch(typeof(Game), "SpawnPlayer")]
        internal static class Game_OnNewCharacterDone_Patch
        {
            [HarmonyPostfix]
            internal static void Postfix()
            {
                {
                    JustWaitforInventory = false;
                    StartingitemPrefab();
                    
                    //((MonoBehaviour)(object)context).StartCoroutine(DelayedLoad()); // important
                }
            }
        }

        [HarmonyPatch(typeof(FejdStartup), "OnNewCharacterDone")]
        internal static class FejdStartup_OnNewCharacterDone_Patch
        {
            internal static void Postfix()
            {
                StartingFirsttime();

            }

        }

       
        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.TargetFound))]
        public static class DisabldHaveTarget
        {
            internal static bool Prefix(ref bool __result)
            {
                if (Player.m_localPlayer.m_seman.GetStatusEffect("yippeTele") != null)
                {
                    __result = true;
                    return false;
                }
                if (TargetPortalLoaded && !ConfigTargetPortalAnimation.Value) // I should make a config for this instead
                {
                    __result = false;
                    return false;
                }
                if (TargetPortalLoaded && ConfigTargetPortalAnimation.Value) // I should make a config for this instead
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }


        

        [HarmonyPatch(typeof(Minimap), "SetMapMode")] // doesn't matter if targetportal is loaded or not
        public class LeavePortalModeOnMapCloseMagicPortal
        {
            internal static void Postfix(Minimap.MapMode mode)
            {
                if (mode != Minimap.MapMode.Large)
                {
                    Teleporting = false;
                }
            }
        }


        [HarmonyPatch(typeof(Inventory), "IsTeleportable")]
        public static class InventoryIsTeleportablePatchRMP
        {
            [HarmonyPriority(Priority.LowerThanNormal)]
            internal static bool Prefix(ref bool __result, ref Inventory __instance)
            {
                /*
				if (Game.instance.m_firstSpawn)
				{
					return __result;
				}
				*/
                if (JustWaitforInventory)
                {
                    return true;
                }
               // __instance.GetTotalWeight()

                //Player.m_localPlayer
                bool bo2 = false;
                if (Player.m_localPlayer.m_seman.GetStatusEffect("yippeTele") != null)
                {
                    bo2 = true;
                }

                
                Piece portal = null;
                String name = null;
                Vector3 hi = Player.m_localPlayer.transform.position;
                List<Piece> piecesfound = new List<Piece>();
                Piece.GetAllPiecesInRadius(hi, 5f, piecesfound);
                /*
				foreach (Piece piece in piecesfound)
                {
					//RareMagicPortal.LogInfo($"Piece found {piece.name}");
					if (piece.name == "portal_wood(Clone)") // list of pieces that have teleport world
                    {
						portal = piece;
						//RareMagicPortal.LogInfo($"Inventory found Portal");
						break;
                    }
                }*/
                TeleportWorld portalW = null;
                foreach (Piece piece in piecesfound)
                {
                    if (piece.TryGetComponent<TeleportWorld>(out portalW))
                    {
                        break;
                    }
                }

                if (portalW != null)
                {
                    //portalW = portal.GetComponent<TeleportWorld>();
                    name = portalW.GetText(); // much better
                    if (name != null)
                    {
                        /*
						var found = name.IndexOf(":") + 2;
						var end = name.IndexOf("\" ");
						var le = end - found;
						name = name.Substring(found, le); // lol wish it was more efficent
						//RareMagicPortal.LogInfo($"Inventory Portal Check name is {name}");
						*/

                        var PortalName = name;
                        bool OdinsKin = false;
                        bool Free_Passage = false;
                        bool TeleportAny = false;
                        List<string> AdditionalProhibitItems;

                        if (!PortalColorLogic.PortalN.Portals.ContainsKey(PortalName)) // if doesn't contain use defaults
                        {
                            PortalColorLogic.WritetoYML(PortalName);
                        }
                        OdinsKin = PortalColorLogic.PortalN.Portals[PortalName].Admin_only_Access;
                        Free_Passage = PortalColorLogic.PortalN.Portals[PortalName].Free_Passage;
                        TeleportAny = PortalColorLogic.PortalN.Portals[PortalName].TeleportAnything;
                        AdditionalProhibitItems = PortalColorLogic.PortalN.Portals[PortalName].AdditionalProhibitItems;

                        if (TeleportAny && ConfigEnableCrystalsNKeys.Value) // allows for teleport anything portal if EnableCrystals otherwise just white
                            bo2 = true;

                        if (!bo2 && AdditionalProhibitItems.Count > 0)
                        {
                            var instan = ObjectDB.instance;
                            foreach (ItemDrop.ItemData allItem in __instance.GetAllItems())
                            {
                                foreach (var item in AdditionalProhibitItems)
                                {
                                    GameObject go = instan.GetItemPrefab(item);
                                    if (go != null)
                                    {
                                        ItemDrop.ItemData data = go.GetComponent<ItemDrop>().m_itemData;
                                        if (data != null)
                                        {
                                            if (data.m_shared.m_name == allItem.m_shared.m_name)
                                            {
                                                RareMagicPortal.LogInfo($"Found Prohibited item {data.m_shared.m_name} Teleport Not Allowed");
                                                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Prohibited item " + data.m_shared.m_name);
                                                __result = false;
                                                return false;
                                            }
                                        }
                                    }


                                }
                            }
                        }// end !bo2
                        if (!bo2 && ConfigMaxWeight.Value > 0 && (TeleportingforWeight > 0 || Teleporting))
                        {
                            var playerweight = __instance.GetTotalWeight();
                            
                            if (playerweight > ConfigMaxWeight.Value)
                            {
                                RareMagicPortal.LogInfo($"Player Weight is greater than Max Portal Weight");
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You are carrying too much, max is " + ConfigMaxWeight.Value);
                                TeleportingforWeight++;

                                if (TeleportingforWeight > 10)
                                    TeleportingforWeight = 0;

                                __result = false;
                                return false;
                            }
                            TeleportingforWeight = 0;
                        }


                    }
                }


                if (bo2) // if status effect is active 
                {
                    __result = true;
                    return false;
                }
                return true;

            }
        }


        [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
        internal class MapLeftClickForRareMagic // for magic portal
        {
            internal class SkipPortalException2 : Exception // skip all other mods if targetportal is installed and passes everything else
            {
            }

            [HarmonyPriority(Priority.HigherThanNormal)]
            internal static bool Prefix()
            {


                if (!Teleporting)
                {
                    return true;
                }
                if (!Chainloader.PluginInfos.ContainsKey("org.bepinex.plugins.targetportal"))
                { // check to see if targetportal is loaded
                    return true;
                }
                //RareMagicPortal.LogInfo($"Made it to Map during Telecheck");
                string PortalName;
                Minimap Instancpass = Minimap.instance;
                HoldPins = Instancpass.m_pins;
                //return true;

                try
                {
                    PortalName = functions.HandlePortalClick(); //my handleportal click
                }
                catch { PortalName = null; }
                if (PortalName == null)
                {
                    throw new SkipPortalException2();//return false; and stop TargetPortals from executing

                }
                if (!Player.m_localPlayer.IsTeleportable())
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                    return false;
                }
                if (PortalColorLogic.CrystalandKeyLogic(PortalName))
                {
                    //RareMagicPortal.LogInfo($"True, so TargetPortalShould Take over");

                    //HandleTeleport(Instancpass);
                    return true; // allow TargetPortal to do it's checks
                                 //throw new SkipPortalException2();//return false; and stop TargetPortals from executing
                }
                else
                {
                    //RareMagicPortal.LogInfo($"TargetPortal is forced to stop");
                    throw new SkipPortalException2();//return false; and stop TargetPortals from executing

                }



            }
            internal static Exception? Finalizer(Exception __exception) => __exception is SkipPortalException2 ? null : __exception;
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
        [HarmonyPriority(Priority.Low)]
        public class AddMinimapPortalIconRMP
        {

            internal static void Postfix(Minimap __instance)
            {
                HoldPins = Minimap.instance.m_pins;
                //RareMagicPortal.LogWarning("Here is MinimapStart");
            }
        }

        [HarmonyPatch(typeof(TeleportWorldTrigger), nameof(TeleportWorldTrigger.OnTriggerEnter))]  // for Crystals and Keys
        internal class TeleportWorld_Teleport_CheckforCrystal
        {
            internal class SkipPortalException : Exception
            {
            }
            //throw new SkipPortalException(); This is used for return false instead/ keeps other mods from loading patches.

            static string OutsideP = null;
            [HarmonyPriority(Priority.HigherThanNormal)]
            internal static bool Prefix(TeleportWorldTrigger __instance, Collider collider)
            {
                //finding portal name
                if (collider.GetComponent<Player>() != Player.m_localPlayer)
                {
                    throw new SkipPortalException();
                }
                TeleportingforWeight = 1;

                PortalColorLogic.player = collider.GetComponent<Player>();
                string PortalName = "";
                if (!Chainloader.PluginInfos.ContainsKey("com.sweetgiorni.anyportal"))
                { // check to see if AnyPortal is loaded // don't touch when anyportal is loaded

                    PortalName = __instance.m_tp.GetText();
                }
                else // for anyportal
                {
                    ZDOID zDOID = __instance.m_tp.m_nview.GetZDO().GetZDOID("target");
                    ZDO zDO = ZDOMan.instance.GetZDO(zDOID);
                    if (zDO == null || !zDO.IsValid())
                    {
                    }
                    else
                    {
                        PortalName = zDO.GetString("tag");
                    }
                }
                // end finding portal name

                m_hadTarget = __instance.m_tp.m_hadTarget;
                OutsideP = PortalName;
                // keep player and m_hadTarget for future patch for targetportal

                if (Chainloader.PluginInfos.ContainsKey("org.bepinex.plugins.targetportal"))
                {
                    Teleporting = true;
                    return true; // skip on checking because we don't know where this is going 
                                 // we will catch in map for tele check
                }
                if (!m_hadTarget) // if no target continuie on with logic
                    return false;

                if (PortalColorLogic.CrystalandKeyLogic(PortalName))
                {
                   // Teleporting = true;
                    return true;

                }
                else // false 
                {
                    Teleporting = false;
                    if (Chainloader.PluginInfos.ContainsKey("org.bepinex.plugins.targetportal")) // or any other mods that need to be skipped // this shoudn't be hit
                        throw new SkipPortalException();  // stops other mods from executing  // little worried about betterwards and loveisward
                    else return false;
                }

                //else return true;
            }
            internal static Exception? Finalizer(Exception __exception) => __exception is SkipPortalException ? null : __exception;

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Low)]
            internal static void Postfix(TeleportWorldTrigger __instance)
            {
                if (Teleporting && Chainloader.PluginInfos.ContainsKey("org.bepinex.plugins.targetportal"))
                {
                    //RareMagicPortal.LogInfo($"Made it to Portal Trigger");

                    int colorint;
                    String PName;
                    String PortalName;
                    Minimap instance = Minimap.instance;
                    List<Minimap.PinData> paul = instance.m_pins;

                    //instance.ShowPointOnMap(__instance.transform.position);
                    try
                    {
                        //PortalName = HandlePortalClick(); //This is making minimap instance correctly
                    }
                    catch { }
                    //List<Minimap.PinData> paul = HoldPins;
                    foreach (Minimap.PinData pin in paul)
                    {
                        PName = null;
                        try
                        {
                            if (pin.m_icon.name == "TargetPortalIcon") // only selects correct icon now
                            {

                                PName = pin.m_name; // icons name - Portalname

                                colorint = PortalColorLogic.CrystalandKeyLogicColor(PName); // kindof expensive task to do this cpu wize for all portals
                                switch (colorint)
                                {
                                    case 0:
                                        pin.m_icon = IconBlack;
                                        break;
                                    case 1:
                                        pin.m_icon = IconDefault;
                                        break;
                                    case 2:
                                        pin.m_icon = IconRed;
                                        break;
                                    case 3:
                                        pin.m_icon = IconGreen;
                                        break;
                                    case 4:
                                        pin.m_icon = IconBlue;
                                        break;
                                    case 5:
                                        pin.m_icon = IconPurple;
                                        break;
                                    case 6:
                                        pin.m_icon = IconTan;
                                        break;
                                    case 7:
                                        pin.m_icon = IconGold;
                                        break;
                                    case 8:
                                        pin.m_icon = IconWhite;
                                        break;
                                    default:
                                        pin.m_icon = IconDefault;
                                        break;
                                }
                                pin.m_icon.name = "TargetPortalIcon"; // test after 2.4
                            }
                        }
                        catch { }
                    }// foreach
                }

            }
        }


        [HarmonyPatch(typeof(Player), "CheckCanRemovePiece")]
        internal static class Player_CheckforOwnerP
        {
            [HarmonyPrefix]
            internal static bool Prefix(ref Player __instance, ref Piece piece)
            {
                if (piece == null)
                    return true;

                if (piece.name == PiecetoLookFor && !__instance.m_noPlacementCost) // portal
                {
                    bool bool2 = piece.IsCreator();// nice
                    if (bool2 || !ConfigCreator.Value)
                    { // can remove because is creator or creator only mode is foff
                        return true;

                    }
                    else
                    {
                        __instance.Message(MessageHud.MessageType.Center, "$rmp_youarenotcreator");
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Player), "PlacePiece")]
        internal static class Player_MessageforPortal_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(ref Player __instance, ref Piece piece)

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
                    if (ConfigTableLvl.Value > 10 || ConfigTableLvl.Value < 1)
                        ConfigTableLvl.Value = 1;

                    if (paullvl + 1 > ConfigTableLvl.Value) // just for testing
                    {
                        piecehaslvl = true;
                    }
                    else
                    {
                        string worktablename = piece.m_craftingStation.name;
                        GameObject temp = GetPieces().Find(g => Utils.GetPrefabName(g) == worktablename);
                        var name = temp.GetComponent<Piece>().m_name;
                        __instance.Message(MessageHud.MessageType.Center, "$rmp_needlvl " + ConfigTableLvl.Value + " " + name + " $rmp_forplacement");
                        piecehaslvl = false;
                        return false;
                    }
                }
                return true;
            }

        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        internal static class ZrouteMethodsClientRMP

        {
            internal static void Prefix()
            {
                ZRoutedRpc.instance.Register("RequestServerAnnouncementRMP", new Action<long, ZPackage>(functions.RPC_RequestServerAnnouncementRMP)); // Our Server Handler
                ((MonoBehaviour)(object)context).StartCoroutine(RemovedDestroyedTeleportWorldsCoroutine()); // moved to this incase the stop and start joining
                                                                                                            //ZRoutedRpc.instance.Register("EventServerAnnouncementRMP", new Action<long, ZPackage>(RPC_EventServerAnnouncementRMP)); // event handler
            }
        }

        [HarmonyPatch(typeof(ZNet), "Shutdown")]
        internal class PatchZNetDisconnect
        {

            internal static bool Prefix()
            {
                RareMagicPortal.LogInfo("Logoff? Save text file, don't delete");

                context.StopCoroutine(RemovedDestroyedTeleportWorldsCoroutine());
                //context.StopCoroutine(myCoroutineRMP);

                NoMoreLoading = true;
                JustWaitforInventory = true;
                return true;
            }
        }


        [HarmonyPatch(typeof(ZNet), "OnDestroy")]
        internal class PatchZNetDestory
        {
            internal static void Postfix()
            { // The Server send once last config sync before destory, but after Shutdown which messes stuff up. 
                NoMoreLoading = false;

            }
        }

        public void Awake()
        {
            CreateConfigValues();
            ReadAndWriteConfigValues();
            Localizer.Load();
            //english = new Localization();
            //english.SetupLanguage("English");
            //spanish = new Localization();
            //spanish.SetupLanguage("Spanish");
            LoadAssets();



            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(MagicPortalFluid).Namespace);
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), (string)null);

            SetupWatcher();
            setupYMLFolderWatcher();
            PortalDrink();

            YMLPortalData.ValueChanged += CustomSyncEventDetected;

            IconColors();


            RareMagicPortal.LogInfo($"MagicPortalFluid has loaded start assets");


        }
        internal void IconColors()
        {
            context = this;

            Texture2D tex = IconColor.loadTexture("portal.png");
            Texture2D temp = IconColor.loadTexture("portaliconTarget.png");
            IconDefault = IconColor.CreateSprite(temp, false);
            //Color Gold = new Color(1f, 215f / 255f, 0, 1f);
            IconColor.setTint(Color.black);
            IconBlack = IconColor.CreateSprite(tex, true);
            IconColor.setTint(Color.yellow);
            IconYellow = IconColor.CreateSprite(tex, true);
            IconColor.setTint(Color.red);
            IconRed = IconColor.CreateSprite(tex, true);
            IconColor.setTint(Color.green);
            IconGreen = IconColor.CreateSprite(tex, true);
            IconColor.setTint(Color.blue);
            IconBlue = IconColor.CreateSprite(tex, true);
            IconColor.setTint(PortalColorLogic.Gold);
            IconGold = IconColor.CreateSprite(tex, true);
            IconColor.setTint(Color.white);
            IconWhite = IconColor.CreateSprite(tex, true);
            IconColor.setTint(PortalColorLogic.Purple);
            IconPurple = IconColor.CreateSprite(tex, true);
            IconColor.setTint(PortalColorLogic.Brown);
            IconTan = IconColor.CreateSprite(tex, true);

        }

        internal static void LoadIN()
        {
            LoggingOntoServerFirst = true;
            setupYMLFile();
            ReadYMLValuesBoring();
        }


        // end startup

        internal void PortalDrink()
        {
            AllowTeleEverything.Name.English("Odin's Portal Blessing");
            AllowTeleEverything.Type = EffectType.Consume;
            AllowTeleEverything.Icon = "portaldrink.png";
            //AllowTeleEverything.IconSprite = portaldrind;
            AllowTeleEverything.Effect.m_startMessageType = MessageHud.MessageType.Center;
            AllowTeleEverything.Effect.m_startMessage = "$rmp_startmessage";
            AllowTeleEverything.Effect.m_stopMessageType = MessageHud.MessageType.Center; // Specify where the stop effect message shows
            AllowTeleEverything.Effect.m_stopMessage = "$rmp_stopmessage";
            AllowTeleEverything.Effect.m_tooltip = "$rmp_tooltip_drink";
            //AllowTeleEverything.Effect.m_stopEffects = 
            AllowTeleEverything.Effect.m_ttl = PortalDrinkTimer.Value; // 2min
            AllowTeleEverything.Effect.m_time = 0f;// starts at 0 
            AllowTeleEverything.Effect.m_flashIcon = true;
            //AllowTeleEverything.Effect.m_cooldown = DrinkDuration;
            AllowTeleEverything.Effect.IsDone();// well be true if done
            AllowTeleEverything.AddSEToPrefab(AllowTeleEverything, "PortalDrink");

        }

        internal void LoadAssets()
        {

            //ObjectDB Dat = ObjectDB.instance;

            Item portalmagicfluid = new("portalmagicfluid", "portalmagicfluid", "assetsEmbedded");
            portalmagicfluid.Name.English("Magical Portal Fluid");
            portalmagicfluid.Description.English("Once a mythical essence, now made real with Odin's blessing");
            portalmagicfluid.DropsFrom.Add("gd_king", 1f, 1, 2); // Elder drop 100% 1-2 portalFluids
            portalmagicfluid.ToggleConfigurationVisibility(Configurability.Drop);
            

            PortalFluidname = portalmagicfluid.Prefab.name;

            Item PortalDrink = new("portalmagicfluid", "PortalDrink", "assetsEmbedded");
            PortalDrink.Name.English("Magical Portal Drink");
            PortalDrink.Description.English("Odin's Blood of Teleportation");
            PortalDrink.ToggleConfigurationVisibility(Configurability.Drop);
  

            Item PortalCrystalMaster = new("portalcrystal", "PortalCrystalMaster", "assetsEmbedded");
            PortalCrystalMaster.Name.English("Gold Portal Crystal");
            PortalCrystalMaster.Description.English("Odin's Golden Crystal allows for Golden Portal Traveling and maybe more Portals");
            PortalCrystalMaster.ToggleConfigurationVisibility(Configurability.Drop);

            Item PortalCrystalRed = new("portalcrystal", "PortalCrystalRed", "assetsEmbedded");
            PortalCrystalRed.Name.English("Red Portal Crystal");
            PortalCrystalRed.Description.English("Odin's Traveling Crystals allow for Red Portal Traveling");
            PortalCrystalRed.ToggleConfigurationVisibility(Configurability.Drop);
            

            Item PortalCrystalGreen = new("portalcrystal", "PortalCrystalGreen", "assetsEmbedded");
            PortalCrystalGreen.Name.English("Green Portal Crystal");
            PortalCrystalGreen.Description.English("Odin's Traveling Crystals allow for Green Portal Traveling");
            PortalCrystalGreen.ToggleConfigurationVisibility(Configurability.Drop);

            Item PortalCrystalBlue = new("portalcrystal", "PortalCrystalBlue", "assetsEmbedded");
            PortalCrystalBlue.Name.English("Blue Portal Crystal");
            PortalCrystalBlue.Description.English("Odin's Traveling Crystals allow for Blue Portal Traveling");
            PortalCrystalBlue.ToggleConfigurationVisibility(Configurability.Drop);

            Item PortalCrystalPurple = new("portalcrystal", "PortalCrystalPurple", "assetsEmbedded");
            PortalCrystalPurple.Name.English("Purple Portal Crystal");
            PortalCrystalPurple.Description.English("Odin's Traveling Crystals allow for Purple Portal Traveling");
            PortalCrystalPurple.ToggleConfigurationVisibility(Configurability.Drop);

            Item PortalCrystalTan = new("portalcrystal", "PortalCrystalTan", "assetsEmbedded");
            PortalCrystalTan.Name.English("Tan Portal Crystal");
            PortalCrystalTan.Description.English("Odin's Traveling Crystals allow for Tan Portal Traveling");
            PortalCrystalTan.ToggleConfigurationVisibility(Configurability.Drop);

            Item PortalKeyRed = new("portalcrystal", "PortalKeyRed", "assetsEmbedded");
            PortalKeyRed.Name.English("Red Portal Key");
            PortalKeyRed.Description.English("Unlock Portals Requiring The Red Key");
            PortalKeyRed.ToggleConfigurationVisibility(Configurability.Disabled);

            Item PortalKeyGold = new("portalcrystal", "PortalKeyGold", "assetsEmbedded");
            PortalKeyGold.Name.English("Gold Portal Key");
            PortalKeyGold.Description.English("Unlock Gold Portals and perhaps more Portals");
            PortalKeyGold.ToggleConfigurationVisibility(Configurability.Disabled);

            Item PortalKeyBlue = new("portalcrystal", "PortalKeyBlue", "assetsEmbedded");
            PortalKeyBlue.Name.English("Blue Portal Key");
            PortalKeyBlue.Description.English("Unlock Portals Requiring The Blue Key");
            PortalKeyBlue.ToggleConfigurationVisibility(Configurability.Disabled);

            Item PortalKeyGreen = new("portalcrystal", "PortalKeyGreen", "assetsEmbedded");
            PortalKeyGreen.Name.English("Green Portal Key");
            PortalKeyGreen.Description.English("Unlock Portals Requiring The Green Key");
            PortalKeyGreen.ToggleConfigurationVisibility(Configurability.Disabled);

            Item PortalKeyPurple = new("portalcrystal", "PortalKeyPurple", "assetsEmbedded");
            PortalKeyPurple.Name.English("Purple Portal Key");
            PortalKeyPurple.Description.English("Unlock Portals Requiring The Purple Key");
            PortalKeyPurple.ToggleConfigurationVisibility(Configurability.Disabled);

            Item PortalKeyTan = new("portalcrystal", "PortalKeyTan", "assetsEmbedded");
            PortalKeyTan.Name.English("Tan Portal Key");
            PortalKeyTan.Description.English("Unlock Portals Requiring The Tan Key");
            PortalKeyTan.ToggleConfigurationVisibility(Configurability.Disabled);

            //Item WorldTreeSeed = new("worldtreeseed", "WorldTreeSeed", "assetsEmbedded");
            //PortalKeyTan.Name.English("WorldTree Seed");
            //PortalKeyTan.Description.English("A seed dropped from Yggdrasil, it is known to have magical properites");




        }

        internal void UnLoadAssets()
        {
            portalmagicfluid.Unload(false);
        }

        public void setupYMLFolderWatcher()
        {
            if (!Directory.Exists(YMLFULLFOLDER))
            {
                Directory.CreateDirectory(YMLFULLFOLDER);
                //File.WriteAllText(YMLFULL, WelcomeString + yaml); //overwrites
            }

            FileSystemWatcher watcherfolder = new(YMLFULLFOLDER);
            watcherfolder.Changed += ReadYMLValues;
            watcherfolder.Created += ReadYMLValues;
            watcherfolder.Renamed += ReadYMLValues;
            watcherfolder.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcherfolder.IncludeSubdirectories = true;
            watcherfolder.EnableRaisingEvents = true;

        }

        internal static void setupYMLFile()

        {
            Worldname = ZNet.instance.GetWorldName();
            RareMagicPortal.LogInfo("WorldName " + Worldname);
            YMLCurrentFile = Path.Combine(YMLFULLFOLDER, Worldname + ".yml");
            functions.GetAllMaterials();

            if (!File.Exists(YMLCurrentFile))
            {
                PortalColorLogic.PortalN = new PortalName()  // kind of iffy in inside this
                {
                    Portals = new Dictionary<string, PortalName.Portal>
                    {
                        {"Demo_Portal_Name", new PortalName.Portal() {
							//Crystal_Cost_Master = 3,
						}},

                    }
                };
                PortalColorLogic.PortalN.Portals["Demo_Portal_Name"].AdditionalProhibitItems.Add("Stone");
                PortalColorLogic.PortalN.Portals["Demo_Portal_Name"].AdditionalProhibitItems.Add("Wood");

                var serializer = new SerializerBuilder()
                    .Build();
                var yaml = serializer.Serialize(PortalColorLogic.PortalN);
                WelcomeString = WelcomeString + Environment.NewLine;

                File.WriteAllText(YMLCurrentFile, WelcomeString + yaml); //overwrites
                RareMagicPortal.LogInfo("Creating Portal_Name file " + Worldname);
                JustWrote = 2;
            }
        }
        internal void CustomSyncEventDetected()
        {
            //Worldname = ZNet.instance.GetWorldName();
            if (String.IsNullOrEmpty(ZNet.instance.GetWorldName()))
                JustWait = true;
            else JustWait = false;

            if (!JustWait && !NoMoreLoading)
            {
                YMLCurrentFile = Path.Combine(YMLFULLFOLDER, Worldname + ".yml");
                if (LoggingOntoServerFirst)
                {
                    RareMagicPortal.LogInfo("You are now connected to Server World" + Worldname);
                    LoggingOntoServerFirst = false;
                }

                string SyncedString = YMLPortalData.Value;

                if (ConfigEnableYMLLogs.Value)
                    RareMagicPortal.LogInfo(SyncedString);

                var deserializer = new DeserializerBuilder()
                    .Build();

                PortalColorLogic.PortalN.Portals.Clear();
                PortalColorLogic.PortalN = deserializer.Deserialize<PortalName>(SyncedString);
                if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated())
                {
                    //RareMagicPortal.LogInfo("Server Portal UPdates Are being Saved " + Worldname);
                    //File.WriteAllText(YMLCurrentFile, SyncedString);
                }
                JustWrote = 2;
                JustSent = 0; // ready for another send
                              //StartCoroutine(WaitforJustSent());

            }
            if (!ZNet.instance.IsServer())
            {
                isLocal = false;
                bool admin = ConfigSync.IsAdmin;
                isAdmin = admin;
                if (isAdmin)
                    RareMagicPortal.LogInfo("You are RMP admin");
                else
                    RareMagicPortal.LogInfo("You are NOT RMP admin");
            }

        }
        IEnumerator WaitforReadWrote()
        {
            yield return new WaitForSeconds(1);

            JustWrote = 0; // lets manual update happen no problem
                           //StopCoroutine(WaitforReadWrote()); not needed
        }

        IEnumerator WaitforJustSent()
        {
            yield return new WaitForSeconds(1);

            JustSent = 0;

        }

        internal void ReadYMLValues(object sender, FileSystemEventArgs e) // Thx Azumatt // This gets hit after writing
        {
            if (!File.Exists(YMLCurrentFile)) return;
            if (isAdmin && JustWrote == 0) // if local admin or ServerSync admin
            {
                var yml = File.ReadAllText(YMLCurrentFile);

                var deserializer = new DeserializerBuilder()
                    .Build();

                PortalColorLogic.PortalN.Portals.Clear();
                PortalColorLogic.PortalN = deserializer.Deserialize<PortalName>(yml);
                if (ZNet.instance.IsServer())//&& ZNet.instance.IsDedicated()) Any Server
                {
                    RareMagicPortal.LogInfo("Server Portal YML Manual UPdate " + Worldname);
                    YMLPortalData.Value = yml;
                }
                else
                {
                    RareMagicPortal.LogInfo("Client Admin Manual YML UPdate " + Worldname);
                    if (ConfigEnableYMLLogs.Value)
                        RareMagicPortal.LogInfo(yml);
                }

            }


            if (JustWrote == 2)
            {
                JustWrote = 3; // stops from doing again
                StartCoroutine(WaitforReadWrote());
            }

            if (JustWrote == 1)
                JustWrote = 2;

            if (!isAdmin)
            {
                //RareMagicPortal.LogInfo("Portal Values Didn't change because you are not an admin");

            }
        }

        internal void SetupWatcher() // Thx Azumatt
        {
            FileSystemWatcher watcher = new(BepInEx.Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        internal static void ReadYMLValuesBoring() // Startup File 
        {
            if (JustWait)
            {
                Worldname = ZNet.instance.GetWorldName();
                JustWait = false;
                YMLCurrentFile = Path.Combine(YMLFULLFOLDER, Worldname + ".yml");
                if (LoggingOntoServerFirst)
                {
                    RareMagicPortal.LogInfo("You are now connected to Server World" + Worldname);
                    LoggingOntoServerFirst = false;
                }

                isLocal = false;
                if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated())
                {
                    //isDedServer = true;
                }
                string SyncedString = YMLPortalData.Value;

                if (ConfigEnableYMLLogs.Value)
                    RareMagicPortal.LogInfo(SyncedString);

                var deserializer2 = new DeserializerBuilder()
                    .Build();

                //PortalN.Portals.Clear();
                PortalColorLogic.PortalN = deserializer2.Deserialize<PortalName>(SyncedString);
                JustWrote = 2;
                File.WriteAllText(YMLCurrentFile, WelcomeString + SyncedString); //overwrites

            }
            else
            {
                if (!File.Exists(YMLCurrentFile)) return;
                var yml = File.ReadAllText(YMLCurrentFile);

                var deserializer = new DeserializerBuilder()
                    .Build();
                //PortalN.Portals.Clear();
                PortalColorLogic.PortalN = new PortalName(); // init
                PortalColorLogic.PortalN = deserializer.Deserialize<PortalName>(yml);
                if (ConfigEnableYMLLogs.Value)
                    RareMagicPortal.LogInfo(yml);

                if (ZNet.instance.IsServer()) // not just dedicated should send this 
                {
                    YMLPortalData.Value = yml; // should only be one time and for server
                }
            }

        }

        internal void ReadConfigValues(object sender, FileSystemEventArgs e) // Thx Azumatt
        {
            if (!File.Exists(ConfigFileFullPath)) return;


            bool admin = ConfigSync.IsAdmin;
            bool locked = ConfigSync.IsLocked;
            bool islocaladmin = false;
            bool isdedServer = false;
            if (ZNet.instance.IsServer() && !ZNet.instance.IsDedicated())
                islocaladmin = true;
            if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated())
                isdedServer = true;


            if (admin || islocaladmin || isdedServer) // Server Sync Admin Only
            {
                isAdmin = admin; // need to check this
                try
                {
                    if (islocaladmin)
                    {
                        isAdmin = true;
                        RareMagicPortal.LogInfo("ReadConfigValues loaded- you are a local admin");
                        Config.Reload();
                        ReadAndWriteConfigValues();
                        PortalChanger();
                    }
                    else if (isdedServer)
                    {
                        RareMagicPortal.LogInfo("ReadConfigValues loaded- you the dedicated Server");
                        Config.Reload();
                        ReadAndWriteConfigValues();
                        PortalChanger();
                    }
                    else if (ConfigSync.IsSourceOfTruth)
                    {
                        RareMagicPortal.LogInfo("ReadConfigValues loaded- you are an admin - on a server");
                        Config.Reload();
                        ReadAndWriteConfigValues();
                        PortalChanger();
                    }
                    else
                    {
                        // false so remote config is being used
                        RareMagicPortal.LogInfo("Server values loaded");
                        ReadAndWriteConfigValues();
                        PortalChanger();
                    }


                }
                catch
                {
                    RareMagicPortal.LogInfo($"There was an issue loading your {ConfigFileName}");
                    RareMagicPortal.LogInfo("Please check your config entries for spelling and format!");
                }
            }
            else
            {
                isAdmin = false;
                RareMagicPortal.LogInfo("You are not ServerSync admin");
                try
                {


                    if (ConfigSync.IsSourceOfTruth && !locked)
                    {
                        RareMagicPortal.LogInfo("ReadConfigValues loaded- You are an local admin"); // Server hits this on dedicated
                        ReadAndWriteConfigValues();
                        PortalChanger();
                        isAdmin = true; // local admin as well
                    }
                    else
                    {

                        RareMagicPortal.LogInfo("You are not an Server admin - Server Sync Values loaded "); // regular non admin clients
                        ReadAndWriteConfigValues();
                        PortalChanger();

                    }


                }
                catch
                {
                    RareMagicPortal.LogInfo($"There was an issue loading your {ConfigFileName}");
                    RareMagicPortal.LogInfo("Please check your config entries for spelling and format!");
                }


            }
        }


        // changing portals section
        internal static void PortalChanger()
        {
            var peter = GetPieces().Find((GameObject g) => Utils.GetPrefabName(g) == "portal_wood"); //item prefab loaded from hammer
            if (peter != null)
            {
                WearNTear por = peter.GetComponent<WearNTear>();
                por.m_health = ConfiglHealth.Value; // set New Portal Health

                List<Piece.Requirement> requirements = new List<Piece.Requirement>();
                requirements.Add(new Piece.Requirement
                {
                    m_amount = 20,
                    m_resItem = ObjectDB.instance.GetItemPrefab("FineWood").GetComponent<ItemDrop>(),
                    m_recover = true
                });
                if (ConfigFluid.Value)
                { // make this more dynamic
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

                var CraftingStationforPaul = GetCraftingStation(ConfigTable.Value);
                if (CraftingStationforPaul == null)
                {
                    CraftingStationforPaul.m_name = DefaultTable;
                }

                Piece petercomponent = peter.GetComponent<Piece>();
                petercomponent.m_craftingStation = GetCraftingStation(CraftingStationforPaul.m_name); // sets crafting station workbench/forge /ect

                if (ConfigFluid.Value)
                    FluidwasTrue = true;

                if (ConfigFluid.Value || FluidwasTrue)
                petercomponent.m_resources = requirements.ToArray(); // always update?

                //RareMagicPortal.LogInfo($"There changing fluid value {PortalFluidname}");
                ObjectDB.instance.GetItemPrefab(PortalFluidname).GetComponent<ItemDrop>().m_itemData.m_shared.m_value = ConfigFluidValue.Value;

            }       // if loop															  			

        }

        internal static void StartingFirsttime()
        {
            firstTime = true;

        }

        internal static void StartingitemPrefab()
        {

            if (firstTime && ConfigSpawn.Value != 0 && ConfigFluid.Value)
            {
                RareMagicPortal.LogInfo("New Starting Item Set");
                Inventory inventory = ((Humanoid)Player.m_localPlayer).m_inventory;
                inventory.AddItem("PortalMagicFluid", ConfigSpawn.Value, 1, 0, 0L, "");
                firstTime = false;

            }
        }

        public static IEnumerator DelayedLoad()
        {
            yield return new WaitForSeconds(0.05f);
            LoadAllRecipeData(reload: true);
            //yield break;

            // I need to keep checking until the world name is populated- probably at respawn
            while (String.IsNullOrEmpty(ZNet.instance.GetWorldName()) && !NoMoreLoading)
            {
                yield return new WaitForSeconds(0.1f);
            }
            LoadIN();
            yield break;
        }

        internal static void LoadAllRecipeData(bool reload)
        {
            if (reload) // waits until the last seconds to reference and overwrite
            {

                PortalChanger();
                

            }
        }

        internal static CraftingStation GetCraftingStation(string name)
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

        internal static List<GameObject> GetPieces()
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

        #region ConfigOptions

        internal static ConfigEntry<bool>? _serverConfigLocked;


        internal static readonly CustomSyncedValue<string> YMLPortalData = new(ConfigSync, "PortalYmlData", ""); // doesn't show up in config
        internal ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        internal ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        internal class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        #endregion


        internal void CreateConfigValues()
        {
            _serverConfigLocked = config("1.General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            ConfigEnableYMLLogs = config("1.General", "YML Portal Logs", false, "Show YML Portal Logs after Every update", false);

            // Add server config which gets pushed to all clients connecting and can only be edited by admins
            // In local/single player games the player is always considered the admin

            ConfigFluid = config("2.PortalFluid", "Enable Portal Fluid", false,
                            "Enable PortalFluid requirement?");

            ConfigSpawn = config("2.PortalFluid", "Portal Magic Fluid Spawn", 3,
                "How much PortalMagicFluid to start with on a new character?");

            ConfigFluidValue = config("2.PortalFluid", "Portal Fluid Value", 0, "What is the value of MagicPortalFluid? " + System.Environment.NewLine + "A Value of 1 or more, makes the item saleable to trader");

            ConfigTable = config("3.Portal Config", "CraftingStation Requirement", DefaultTable,
                "Which CraftingStation is required nearby?" + System.Environment.NewLine + "Default is Workbench = $piece_workbench, forge = $piece_forge, Artisan station = $piece_artisanstation " + System.Environment.NewLine + "Pick a valid table otherwise default is workbench"); // $piece_workbench , $piece_forge , $piece_artisanstation

            ConfigTableLvl = config("3.Portal Config", "Level of CraftingStation Req", 1,
                "What level of CraftingStation is required for placing Portal?");

            ConfigCreator = config("3.Portal Config", "Only Creator Can Deconstruct", true, "Only the Creator of the Portal can deconstruct it. It can still be destoryed");

            ConfiglHealth = config("3.Portal Config", "Portal Health", 400f, "Health of Portal");

            ConfigAddRestricted = config("3.Portal Config", "Portal D Restrict", "", "Additional Items to Restrict by Default - 'Wood,Stone'");

            ConfigCreatorLock = config("3.Portal Config", "Only Creator Can Change Name", false, "Only Creator can change Portal name");

            ConfigTargetPortalAnimation = config("3.Portal Config", "Force Portal Animation", false, "Forces Portal Animation for Target Portal Mod, is not synced and only config only applies if mod is loaded", false);

            portalRMPKEY = config("3.Portal Config", "Modifier key for toggle", new KeyboardShortcut(KeyCode.LeftControl), "Modifier key that has to be pressed while hovering over Portal + E", false);

            ConfigMaxWeight = config("3.Portal Config", "Max Weight Allowed for Portals", 0, "This affects all portals - Enter the max weight that can transit through a portal at a time. Value of 0 disables the check");

           // ConfigUseBiomeColors = config("6.ForceBiomeColors", "Force Biome Colors for Default", false, "This will Override Default Colors and Use Specific Colors for Biomes");

           // BiomeRepColors = config("6.BiomeColors", "Biome Colors", "Meadows:Brown,BlackForest:Blue,Swamp:Green,Mountain:Black,Plains:Orange,Mistlands:Purple,DeepNorth:Cyan,AshLands:Red,Ocean:Blue", "Biome Colors Represented");

            ConfigEnableCrystalsNKeys = config("4.Portal Crystals", "Enable Portal Crystals and Keys", false, "Enable Portal Crystals and Keys");

            ConfigEnableGoldAsMaster = config("4.Portal Crystals", "Use Gold as Portal Master", true, "Enabled Gold Key and Crystal as Master Key to all (Red,Green,Blue,Purple,Tan,Gold)");

            //ConfigEnableColorEnable = config("Portal Crystals", "ColorsEnabled", "Red,Green,Blue,Purple,Gold", "Whether anyone can use these Colored Portals, even if they have the requirements-  Red,Green,Blue,Purple,Gold");

            //ConfigEnableKeys = config("Portal Keys", "Portal_Keys_Enable", false, "Enable Portal Crystals");

            ConfigCrystalsConsumable = config("4.Portal Crystals", "Crystal Consume Default", 1, "What is the Default number of crystals to consume for each New Portal? - Depending on the default Color, all other colors will be 0 (no access)" + System.Environment.NewLine + " Gold/Master gets set to this regardless of Default Color" + System.Environment.NewLine + " 0 - Means that passage is denied for this color");

            //ConfigAdminOnly = config("Portal Config", "Only_Admin_Can_Build", false, "Only The Admins Can Build Portals");

            CrystalKeyDefaultColor = config("4.Portal Crystals", "Portal Crystal Color Default", "Red", "Default Color for New Portals? " + System.Environment.NewLine + "Red,Green,Blue,Purple,Tan,None" + System.Environment.NewLine + " None - will set Portals to Free Passage by default");

            ConfigMessageLeft = config("4.Portal Crystals", "Use Top Left Message", false, "In case a mod is interfering with Center Messages for Portal tags, display on TopLeft instead.");

            PortalDrinkTimer = config("5.Portal Drink", "Portal Drink Timer", 120, "How Long Odin's Drink lasts");


        }

        internal void ReadAndWriteConfigValues()
        {
            /*
            if (CraftingStationlvl > 10 || CraftingStationlvl < 1)
                CraftingStationlvl = 1;

            */

            AllowTeleEverything.Effect.m_cooldown = PortalDrinkTimer.Value;
        }

/* maybe keep?
        internal static void HandleTeleport(Minimap Instancpass) // this is just for testing
        {

            Minimap instance = Instancpass;
            List<Minimap.PinData> paul = instance.m_pins;
            Vector3 pos = instance.ScreenToWorldPoint(Input.mousePosition);
            float radius = instance.m_removeRadius * (instance.m_largeZoom * 2f);

            Quaternion rotation = Quaternion.Euler(0f, 0f, 0f);

            instance.SetMapMode(Minimap.MapMode.Small);

            Player.m_localPlayer.TeleportTo(pos + rotation * Vector3.forward + Vector3.up, rotation, true);
        }

        
        static void SetParticleColors(
       IEnumerable<Light> lights,
       IEnumerable<ParticleSystem> systems,
       IEnumerable<ParticleSystemRenderer> renderers,
       Color targetColor)
        {
            var targetColorGradient = new ParticleSystem.MinMaxGradient(targetColor);

            foreach (ParticleSystem system in systems)
            {
                var colorOverLifetime = system.colorOverLifetime;

                if (colorOverLifetime.enabled)
                {
                    colorOverLifetime.color = targetColorGradient;
                }

                var sizeOverLifetime = system.sizeOverLifetime;

                if (sizeOverLifetime.enabled)
                {
                    var main = system.main;
                    main.startColor = targetColor;
                }
            }

            foreach (ParticleSystemRenderer renderer in renderers)
            {
                renderer.material.color = targetColor;
            }

            foreach (Light light in lights)
            {
                light.color = targetColor;
            }
        }

*/

        
      
    }// end of  class
    public static class ObjectExtensions
    {
        public static T Ref<T>(this T o) where T : UnityEngine.Object
        {
            return o ? o : null;
        }
    }



}// end of namespace
