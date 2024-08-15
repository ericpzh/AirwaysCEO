using BepInEx;
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

namespace AirwaysCEO
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
            SetupPurchaseButton(ref ___EntBtn, ref ___ELVtext);

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

        public void UpdateButtonsSprite(ref List<Sprite> sprites_)
        {
            if (buttons[1] == null)
            {
                sprites = sprites_;
            }
            else
            {
                UpdateButtonSprite(UpgradeOpt.LONGER_TAXIWAY, ref sprites_);
                UpdateButtonSprite(UpgradeOpt.MORE_TAXIWAY_EXIT, ref sprites_);
                UpdateButtonSprite(UpgradeOpt.TURN_FASTER, ref sprites_);
                UpdateButtonSprite(UpgradeOpt.AIRSPACE, ref sprites_);
                UpdateButtonSprite(UpgradeOpt.COMPENSATION, ref sprites_);
            }
        }

        public void SetupRefreshButton(ref Button parent, ref TMP_Text textParent)
        {
            refreshButton = Instantiate(parent.gameObject).GetComponent<Button>();
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick = new Button.ButtonClickedEvent();
            refreshButton.onClick.AddListener(RefreshUpgrade);
            refreshButton.gameObject.SetActive(value: false);

            SetupRefreshCost(ref textParent);
        }

        public void ShowRefreshButton(Transform parent)
        {
            refreshButton.gameObject.SetActive(value: true);
            refreshButton.gameObject.transform.SetParent(parent);
            refreshButton.transform.localPosition = new Vector3(400, 50, -9f);

            refreshCostText.gameObject.SetActive(value: true);
            refreshCostText.gameObject.transform.SetParent(parent);
            refreshCostText.transform.localPosition = new Vector3(425, 150, -9f);
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

        private void UpdateButtonSprite(UpgradeOpt upgradeOpt, ref List<Sprite> sprites)
        {
            switch (upgradeOpt)
            {
                case UpgradeOpt.LONGER_TAXIWAY:
                case UpgradeOpt.MORE_TAXIWAY_EXIT:
                case UpgradeOpt.TURN_FASTER:
                case UpgradeOpt.AIRSPACE:
                case UpgradeOpt.COMPENSATION:
                    buttons[(int)upgradeOpt].transform.Find("Image").GetComponent<Image>().sprite = sprites[(int)upgradeOpt];
                    buttons[(int)upgradeOpt].transform.Find("Image").GetComponent<RectTransform>().sizeDelta = new Vector2(30, 45);
                    break;
            }
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
            cost.transform.localPosition = new Vector3(785 + ((int)upgradeOpt - 1) * 60, -630, -9f);
            cost.text = "-$" + buyCosts[(int)upgradeOpt];

            if (sprites != null)
            {
                UpdateButtonSprite(upgradeOpt, ref sprites);
            }
        }

        private void SetupPurchaseButton(ref Button parent, ref TMP_Text parentText)
        {
            GameObject esc_button = GameObject.Find("ESC_Button");
            purchaseButton = Instantiate(parent.gameObject, esc_button.transform.parent).GetComponent<Button>();
            purchaseButton.transform.localPosition = new Vector3(-1100f, 620f, -9f);
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick = new Button.ButtonClickedEvent();
            purchaseButton.onClick.AddListener(BuyUpgrades);

            TMP_Text costText = Instantiate(parentText.gameObject, esc_button.transform.parent).GetComponent<TMP_Text>();
            costText.fontSize = 30f;
            costText.rectTransform.sizeDelta = new Vector2(190f, 70f);
            costText.transform.localPosition = new Vector3(-1070f, 670f, -9f);
            costText.text = "-$" + buyCost;
        }

        private void UpdateButtons()
        {
            if (purchaseButton != null)
            {
                purchaseButton.GetComponent<Image>().color = cash >= buyCost ? new Color(255, 255, 255, 0.1f) : new Color(0, 0, 0, 0.5f);
                purchaseButton.transform.Find("Image").GetComponent<Image>().color = cash >= buyCost ? new Color(255, 255, 255, 1f) : new Color(0, 0, 0, 0.5f);
            }

            if (refreshButton != null)
            {
                refreshButton.GetComponent<Image>().color = cash >= refreshCost ? new Color(255, 255, 255, 0.1f) : new Color(0, 0, 0, 0.5f);
                refreshButton.transform.Find("Image").GetComponent<Image>().color = cash >= refreshCost ? new Color(255, 255, 255, 1f) : new Color(0, 0, 0, 0.5f);
            }

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
                case UpgradeOpt.MORE_TAXIWAY_EXIT:
                    available &= !TakeoffTaskManager.Instance.Aprons[TakeoffTaskManager.Instance.Aprons.Count - 1].Interactable;
                    break;
                case UpgradeOpt.AIRSPACE:
                    available &= Camera.main.orthographicSize <= LevelManager.Instance.maximumCameraOrthographicSize - 0.5f;
                    break;
                case UpgradeOpt.COMPENSATION:
                    available &= RestrictedAreaManager.Instance != null && RestrictedAreaManager.Instance.counter < 3;
                    break;
                case UpgradeOpt.LAND_WAYPOINT:
                    available &= WaypointPropsManager.Instance.WaypointAutoLandingCount < Runway.GetAvailableLandPointCount();
                    break;
                case UpgradeOpt.TAKEOFF_WAYPOINT:
                    available &= WaypointPropsManager.Instance.WaypointTakingOffCount < Runway.GetAvailableStartPointCount();
                    break;
            }
            return available;
        }

        private void UpdateButton(UpgradeOpt upgradeOpt)
        {
            buttons[(int)upgradeOpt].GetComponent<Image>().color = UpgradeAvailable(upgradeOpt) ? new Color(255, 255, 255, 0.1f) : new Color(0, 0, 0, 0.5f);
            buttons[(int)upgradeOpt].transform.Find("Image").GetComponent<Image>().color = UpgradeAvailable(upgradeOpt) ? new Color(255, 255, 255, 1f) : new Color(0, 0, 0, 0.5f);
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

        private void SetupRefreshCost(ref TMP_Text parent)
        {
            refreshCostText = Instantiate(parent.gameObject).GetComponent<TMP_Text>();
            refreshCostText.fontSize = 30f;
            refreshCostText.rectTransform.sizeDelta = new Vector2(195f, 50f);
            refreshCostText.text = "-$" + refreshCost;
            refreshCostText.gameObject.SetActive(value: false);
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

            // Reflex for UpgradeManager.Instance.counter[(int)upgradeOpt]++;
            int[] counter = UpgradeManager.Instance.GetFieldValue<int[]>("counter");
            counter[(int)upgradeOpt]++;
            UpgradeManager.Instance.SetFieldValue<int[]>("counter", counter);
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
        public const int buyCost = 20;
        public List<int> buyCosts = new List<int> { 0, 30, 40, 30, 30, 40, 30, 40, 50, 50 };
        public int cash = 0;
        private List<Sprite> sprites;
        private TMP_Text cashDisplay;
        private Button purchaseButton;
        private TMP_Text refreshCostText;
        private Button refreshButton;
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
        static bool Prefix(ref float ___upgradeInterval, ref List<Sprite> ___sprites)
        {
            // Double the speed for upgrade.
            ___upgradeInterval = float.MaxValue;
            Plugin.manager.UpdateButtonsSprite(ref ___sprites);
            return true;
        }
    }

    [HarmonyPatch(typeof(UpgradeManager), "ShowUpgradePanel", new Type[] { })]
    class PatchShowUpgradePanel
    {
        static bool Prefix(ref List<Transform> ___upgradeButtonList)
        {
            if (MapManager.gameMode == GameMode.SandBox)
            {
                return true;
            }
            Plugin.manager.ShowRefreshButton(___upgradeButtonList[2]);
            return true;
        }
    }

    [HarmonyPatch(typeof(GUISandBoxPanel), "Start", new Type[] { })]
    class PatchGUISandBoxPanelStart
    {
        static bool Prefix(ref Button ___TOBtn, ref Button ___LDBtn, ref Button ___HdgBtn, ref Button ___HldBtn, ref Button ___EntBtn, ref Button ___DelBtn, ref TMP_Text ___ELVtext)
        {
            if (MapManager.gameMode == GameMode.SandBox)
            {
                return true;
            }
            Plugin.manager.SetupButtons(ref ___EntBtn, ref ___HdgBtn, ref ___HldBtn, ref ___LDBtn, ref ___TOBtn, ref ___ELVtext);
            Plugin.manager.SetupRefreshButton(ref ___DelBtn, ref ___ELVtext);

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
