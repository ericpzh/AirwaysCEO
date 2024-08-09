using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoneyAirways
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {   
        private void Awake()
        {
            Log = Logger;

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo($"Scene loaded: {scene.name}");
            if ((scene.name == "MapPlayer" || scene.name == "London" || scene.name == "CreatorPlayer"))
            {
                GameObject esc_button = GameObject.Find("ESC_Button");
                if (esc_button != null)
                {
                    manager = esc_button.gameObject.AddComponent<Manager>();
                    manager.cash = 0;
                }
            }
        }

        internal static ManualLogSource Log;
        internal static Manager manager;
    }

    public class Manager : MonoBehaviour {
        private void Start()
        {
            InitTMP();
        }

        private void Update()
        {
            if (cashDisplay == null)
            {
                return;
            }

            Vector3 escButtonBottomRight = Camera.main.ViewportToWorldPoint(new Vector3(0.07f, 0.88f, 0f));
            cashDisplayObj.transform.position = new Vector3(escButtonBottomRight.x + 0.75f, escButtonBottomRight.y + 0.3f, 0f);
            cashDisplay.text = "$ " + cash;

            if (!UpgradeManager.Instance.UpgradeComplete && Input.GetKeyDown(KeyCode.R))
            {
                RefreshUpgrade();
            }

            if (UpgradeManager.Instance.UpgradeComplete && Input.GetKeyDown(KeyCode.B))
            {
                BuyUpgrade();
            }
        }

        private void InitTMP()
        {
            // Init wind text.
            cashDisplayObj = Instantiate(new GameObject("Text"));
            cashDisplay = cashDisplayObj.AddComponent<TextMeshPro>();

            cashDisplay.fontSize = 4f;
            cashDisplay.horizontalAlignment = HorizontalAlignmentOptions.Left;
            cashDisplay.verticalAlignment = VerticalAlignmentOptions.Top;
            cashDisplay.rectTransform.sizeDelta = new Vector2(2, 1);
        }

        private void BuyUpgrade()
        {
            if (cash < buyCost || !UpgradeManager.Instance.UpgradeComplete)
            {
                return;
            }

            cash -= buyCost;
            UpgradeManager.Instance.EnableUpgrade();
        }

        private void RefreshUpgrade()
        {
            if (cash < refreshCost || UpgradeManager.Instance.UpgradeComplete)
            {
                return;
            }

            cash -= refreshCost;
            UpgradeManager.Instance.AssignUpgrades();
        }

        public const int prize = 2;
        public const int refreshCost = 5;
        public const int buyCost = 10;
        public int cash = 0;
        private TMP_Text cashDisplay;
        private GameObject cashDisplayObj;
    }

    [HarmonyPatch(typeof(GameDataWhiteBoard), "OnAircraftHandOff", new Type[] {})]
    class PatchOnAircraftHandOff
    {
        static void Postfix()
        {
            Plugin.manager.cash += Manager.prize;
        }
    }

    [HarmonyPatch(typeof(GameDataWhiteBoard), "OnAircraftLanded", new Type[] {})]
    class PatchOnAircraftLanded
    {
        static void Postfix()
        {
            Plugin.manager.cash += Manager.prize;
        }
    }

    [HarmonyPatch(typeof(GameDataWhiteBoard), "On_VIP_EM_AircraftLanded", new Type[] { typeof(Aircraft) })]
    class PatchOn_VIP_EM_AircraftLanded
    {
        static void Postfix(Aircraft aircraft)
        {
            Plugin.manager.cash += Manager.prize * aircraft.regularEventRewardRemaining;
        }
    }

    [HarmonyPatch(typeof(GameDataWhiteBoard), "On_VIP_EM_AircraftHandOff", new Type[] { typeof(Aircraft) })]
    class PatchOn_On_VIP_EM_AircraftHandOff
    {
        static void Postfix(Aircraft aircraft)
        {
            Plugin.manager.cash += Manager.prize * aircraft.regularEventRewardRemaining;
        }
    }

    [HarmonyPatch(typeof(UpgradeManager), "Start", new Type[] { })]
    class PatchUpgradeManagerStart
    {
        static bool Prefix(ref float ___upgradeInterval)
        {
            // Double the speed for upgrade.
            ___upgradeInterval = float.MaxValue;
            return true;
        }
    }

    public static class ReflectionExtensions
    {
        public static T GetFieldValue<T>(this object obj, string name)
        {
            // Set the flags so that private and public fields from instances will be found
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var field = obj.GetType().GetField(name, bindingFlags);
            return (T)field?.GetValue(obj);
        }

        public static void SetFieldValue<T>(this object obj, string name, T value)
        {
            // Set the flags so that private and public fields from instances will be found
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var field = obj.GetType().GetField(name, bindingFlags);
            field.SetValue(obj, value);
        }
    }
}
