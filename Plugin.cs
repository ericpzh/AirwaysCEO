﻿using BepInEx;
using BepInEx.Logging;
using DG.Tweening;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
                if (MapManager.gameMode == GameMode.SandBox)
                {
                    return;
                }

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
        public void SetupButtons(ref Button ___EntBtn, ref Button ___HdgBtn, ref Button ___HldBtn, ref Button ___LDBtn, ref Button ___TOBtn, ref TMP_Text ___ELVtext)
        {
            SetupButton(ref ___EntBtn, UpgradeOpt.LONGER_TAXIWAY, ref ___ELVtext);
            SetupButton(ref ___EntBtn, UpgradeOpt.MORE_TAXIWAY_EXIT, ref ___ELVtext);
            SetupButton(ref ___EntBtn, UpgradeOpt.TURN_FASTER, ref ___ELVtext);
            SetupButton(ref ___EntBtn, UpgradeOpt.AIRSPACE, ref ___ELVtext);
            SetupButton(ref ___EntBtn, UpgradeOpt.COMPENSATION, ref ___ELVtext);
            SetupButton(ref ___HdgBtn, UpgradeOpt.NAVIGATION_WAYPOINT, ref ___ELVtext);
            SetupButton(ref ___HldBtn, UpgradeOpt.HOLD_WAYPOINT, ref ___ELVtext);
            SetupButton(ref ___LDBtn, UpgradeOpt.LAND_WAYPOINT, ref ___ELVtext);
            SetupButton(ref ___TOBtn, UpgradeOpt.TAKEOFF_WAYPOINT, ref ___ELVtext);

            SetupCost(ref ___ELVtext);
        }

        private void Update()
        {
            if (cashDisplay == null)
            {
                return;
            }
            cashDisplay.text = "$ " + cash;

            if (Input.GetKeyDown(KeyCode.R))
            {
                RefreshUpgrade();
            }
            if (Input.GetKey(KeyCode.LeftShift))
            {
                BuyUpgrade();
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                BuyUpgrades();
            }
            UpdateButtons();
        }

        private void SetupButton(ref Button parent, UpgradeOpt upgradeOpt, ref TMP_Text parentText)
        {
            GameObject esc_button = GameObject.Find("ESC_Button");
            buttons[(int)upgradeOpt] = Instantiate(parent.gameObject, esc_button.transform.parent).GetComponent<Button>();
            buttons[(int)upgradeOpt].transform.localPosition = new Vector3(770 + ((int)upgradeOpt - 1) * 60, -680, -9f);
            buttons[(int)upgradeOpt].onClick.RemoveAllListeners();
            buttons[(int)upgradeOpt].onClick = new Button.ButtonClickedEvent();
            buttons[(int)upgradeOpt].onClick.AddListener(()=>ApplyUpgrade(upgradeOpt));

            TMP_Text cost = Instantiate(parentText.gameObject, esc_button.transform.parent).GetComponent<TMP_Text>();
            cost.transform.localPosition = new Vector3(780 + ((int)upgradeOpt - 1) * 60, -630, -9f);
            cost.text = "$" + buyCosts[(int)upgradeOpt];

            TMP_Text name = Instantiate(parentText.gameObject, esc_button.transform.parent).GetComponent<TMP_Text>();
            name.transform.localPosition = new Vector3(790 + ((int)upgradeOpt - 1) * 60, -600, -9f);
            name.text = GetName(upgradeOpt);
        }

        private string GetName(UpgradeOpt upgradeOpt)
        {
            switch (upgradeOpt)
            {
                case UpgradeOpt.LONGER_TAXIWAY:
                    return "Apron";
                case UpgradeOpt.MORE_TAXIWAY_EXIT:
                    return "Exit";
                case UpgradeOpt.TURN_FASTER:
                    return "Turn";
                case UpgradeOpt.AIRSPACE:
                    return "Space";
                case UpgradeOpt.COMPENSATION:
                    return "Comp";
            }
            return "";
        }

        private void UpdateButtons()
        {
            UpdateButton(UpgradeOpt.LONGER_TAXIWAY);
            UpdateButton(UpgradeOpt.MORE_TAXIWAY_EXIT);
            UpdateButton(UpgradeOpt.TURN_FASTER);
            UpdateButton(UpgradeOpt.AIRSPACE);
            UpdateButton(UpgradeOpt.COMPENSATION);
            UpdateButton(UpgradeOpt.NAVIGATION_WAYPOINT);
            UpdateButton(UpgradeOpt.HOLD_WAYPOINT);
            UpdateButton(UpgradeOpt.LAND_WAYPOINT);
            UpdateButton(UpgradeOpt.TAKEOFF_WAYPOINT);
        }

        private bool UpgradeAvailable(UpgradeOpt upgradeOpt)
        {
            bool available = cash >= buyCosts[(int)upgradeOpt];
            switch (upgradeOpt)
            {
                case UpgradeOpt.AIRSPACE:
                    available &= Camera.main.orthographicSize <= LevelManager.Instance.maximumCameraOrthographicSize - 0.5f;
                    break;
                case UpgradeOpt.COMPENSATION:
                    available &= RestrictedAreaManager.Instance != null && RestrictedAreaManager.Instance.counter < 3;
                    break;
            }
            return available;
        }

        private void UpdateButton(UpgradeOpt upgradeOpt)
        {
            buttons[(int)upgradeOpt].GetComponent<Image>().color = UpgradeAvailable(upgradeOpt) ? new Color(255, 255, 255, 0.1f) : new Color(255, 255, 255, 0.75f);
        }

        private void SetupCost(ref TMP_Text parent)
        {
            GameObject esc_button = GameObject.Find("ESC_Button");
            cashDisplay = Instantiate(parent.gameObject, esc_button.transform.parent).GetComponent<TMP_Text>();
            cashDisplay.fontSize = 40f;
            cashDisplay.transform.localPosition = new Vector3(720f, -670f, -9f);
            cashDisplay.rectTransform.sizeDelta = new Vector2(200f, 100f);
            cashDisplay.text = "$ " + cash;
        }

        private IEnumerator TurnFasterCoroutine()
        {
            float num = 0.1f;
            Aircraft.TurnSpeed += num * Aircraft.TurnSpeed;
            yield break;
        }

        private IEnumerator IncreaseAirspaceCoroutine()
        {
            float num = 0.5f;
            float orthographicSize = Camera.main.orthographicSize;
            Camera.main.DOOrthoSize(orthographicSize + num, 0.5f).SetUpdate(isIndependentUpdate: true);
            LevelManager.Instance.MaximumCameraSizeByInbound += num;
            yield break;
        }

        private void BuyUpgrade()
        {
            UpgradeOpt upgradeOpt = UpgradeOpt.NONE;
            if (Input.GetKey(KeyCode.Alpha1))
            {
                upgradeOpt = UpgradeOpt.LONGER_TAXIWAY;
            }
            else if (Input.GetKey(KeyCode.Alpha2))
            {
                upgradeOpt = UpgradeOpt.MORE_TAXIWAY_EXIT;
            }
            else if (Input.GetKey(KeyCode.Alpha3))
            {
                upgradeOpt = UpgradeOpt.TURN_FASTER;
            }
            else if (Input.GetKey(KeyCode.Alpha4))
            {
                upgradeOpt = UpgradeOpt.AIRSPACE;
            }
            else if (Input.GetKey(KeyCode.Alpha5))
            {
                upgradeOpt = UpgradeOpt.COMPENSATION;
            }
            else if (Input.GetKey(KeyCode.Alpha6))
            {
                upgradeOpt = UpgradeOpt.NAVIGATION_WAYPOINT;
            }
            else if (Input.GetKey(KeyCode.Alpha7))
            {
                upgradeOpt = UpgradeOpt.HOLD_WAYPOINT;
            }
            else if (Input.GetKey(KeyCode.Alpha8))
            {
                upgradeOpt = UpgradeOpt.LAND_WAYPOINT;
            }
            else if (Input.GetKey(KeyCode.Alpha9))
            {
                upgradeOpt = UpgradeOpt.TAKEOFF_WAYPOINT;
            }

            ApplyUpgrade(upgradeOpt);
        }

        private void ApplyUpgrade(UpgradeOpt upgradeOpt)
        {
            if (upgrading || upgradeOpt == UpgradeOpt.NONE || !UpgradeAvailable(upgradeOpt))
            {
                return;
            }

            switch (upgradeOpt)
            {
                case UpgradeOpt.LONGER_TAXIWAY:
                    TakeoffTaskManager.Instance.AddApron();
                    break;
                case UpgradeOpt.MORE_TAXIWAY_EXIT:
                    TakeoffTaskManager.Instance.AddEntrance();
                    break;
                case UpgradeOpt.TURN_FASTER:
                    StartCoroutine(TurnFasterCoroutine());
                    break;
                case UpgradeOpt.AIRSPACE:
                    StartCoroutine(IncreaseAirspaceCoroutine());
                    break;
                case UpgradeOpt.COMPENSATION:
                    RestrictedAreaManager.Instance.ResetCounter();
                    break;
                case UpgradeOpt.NAVIGATION_WAYPOINT:
                    WaypointPropsManager.Instance.SpawnWaypointAutoHeading();
                    break;
                case UpgradeOpt.HOLD_WAYPOINT:
                    WaypointPropsManager.Instance.SpawnWaypointAutoHover();
                    break;
                case UpgradeOpt.LAND_WAYPOINT:
                    WaypointPropsManager.Instance.SpawnWaypointAutoLanding();
                    break;
                case UpgradeOpt.TAKEOFF_WAYPOINT:
                    WaypointPropsManager.Instance.SpawnWaypointTakingOff();
                    break;
            }

            cash -= buyCosts[(int)upgradeOpt];
            upgrading = true;
            StartCoroutine(ResetUpgrading());
            // UpgradeManager.Instance.counter[(int)upgradeOpt]++;
        }

        private IEnumerator ResetUpgrading()
        {
            yield return new WaitForSeconds(0.5f);
            upgrading = false;
        }

        private void BuyUpgrades()
        {
            if (cash < buyCost || !UpgradeManager.Instance.UpgradeComplete)
            {
                return;
            }

            cash -= buyCost;
            UpgradeManager.Instance.EnableUpgrade();
        }

        public void RefreshUpgrade()
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
        public const int buyCost = 20;
        public List<int> buyCosts = new List<int> { 0, 30, 40, 30, 30, 40, 30, 40, 50, 50 };
        public int cash = 0;
        private TMP_Text cashDisplay;
        private GameObject cashDisplayObj;
        private List<Button> buttons = new List<Button> { null, null, null, null, null, null, null, null, null, null };
        private bool upgrading = false;
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

    [HarmonyPatch(typeof(GUISandBoxPanel), "Start", new Type[] { })]
    class PatchGUISandBoxPanelStart
    {
        static bool Prefix(ref Button ___TOBtn, ref Button ___LDBtn, ref Button ___HdgBtn, ref Button ___HldBtn, ref Button ___EntBtn, ref TMP_Text ___ELVtext)
        {
            if (MapManager.gameMode == GameMode.SandBox)
            {
                return true;
            }
            Plugin.manager.SetupButtons(ref ___EntBtn, ref ___HdgBtn, ref ___HldBtn, ref ___LDBtn, ref ___TOBtn, ref ___ELVtext);
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
