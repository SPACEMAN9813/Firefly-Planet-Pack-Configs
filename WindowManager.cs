﻿using KSP.UI.Screens;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Firefly
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	internal class WindowManager : MonoBehaviour
	{
		public static WindowManager Instance { get; private set; }

		ApplicationLauncherButton appButton;
		Rect windowPosition = new Rect(0, 100, 300, 100);
		Rect infoWindowPosition = new Rect(300, 100, 300, 100);

		bool uiHidden = false;
		bool appToggle = false;

		// override toggle values
		public bool tgl_EffectToggle = true;

		// timer
		float reloadBtnTime = 0f;
		
		public void Awake()
		{
			Instance = this;
		}

		public void Start()
		{
			appButton = ApplicationLauncher.Instance.AddModApplication(
				OnApplicationTrue, 
				OnApplicationFalse, 
				null, null, null, null, 
				ApplicationLauncher.AppScenes.FLIGHT, 
				AssetLoader.Instance.iconTexture
			);

			GameEvents.onHideUI.Add(OnHideUi);
			GameEvents.onShowUI.Add(OnShowUi);
		}

		public void OnDestroy()
		{
			// remove everything associated with the thing

			ApplicationLauncher.Instance.RemoveModApplication(appButton);

			GameEvents.onHideUI.Remove(OnHideUi);
			GameEvents.onShowUI.Remove(OnShowUi);
		}

		void OnApplicationTrue()
		{
			appToggle = true;
		}

		void OnApplicationFalse()
		{
			appToggle = false;
		}

		void OnHideUi()
		{
			uiHidden = true;
		}

		void OnShowUi()
		{
			uiHidden = false;
		}

		public void Update()
		{
			
		}

		public void OnGUI()
		{
			if (uiHidden || !appToggle || FlightGlobals.ActiveVessel == null) return;

			windowPosition = GUILayout.Window(416, windowPosition, OnWindow, "Atmospheric Effects Configuration");
			infoWindowPosition = GUILayout.Window(410, infoWindowPosition, OnInfoWindow, "Atmospheric Effects Info");
		}

		/// <summary>
		/// Config and override window
		/// </summary>
		void OnWindow(int id)
		{
			// init
			Vessel vessel = FlightGlobals.ActiveVessel;
			if (vessel == null) return;
			var fxModule = vessel.FindVesselModuleImplementing<AtmoFxModule>();
			if (fxModule == null) return;

			if (!fxModule.isLoaded)
			{
				GUILayout.BeginVertical();
				GUILayout.Label("FX are not loaded for the active vessel");
				GUILayout.EndVertical();
				GUI.DragWindow();
				return;
			}

			// drawing
			GUILayout.BeginVertical();

			bool canReload = (Time.realtimeSinceStartup - reloadBtnTime) > 1f;
			if (GUILayout.Button("Reload Vessel") && canReload)
			{
				fxModule.ReloadVessel();
				reloadBtnTime = Time.realtimeSinceStartup;
			}

			// draw config fields
			for (int i = 0; i < ModSettings.Instance.fields.Count; i++)
			{
				KeyValuePair<string, ModSettings.Field> field = ModSettings.Instance.fields.ElementAt(i);

				if (field.Value.valueType == ModSettings.ValueType.Boolean) DrawConfigFieldBool(field.Key, ModSettings.Instance.fields);
				else if (field.Value.valueType == ModSettings.ValueType.Float) DrawConfigFieldFloat(field.Key, ModSettings.Instance.fields);
			}
			
			// other configs
			GUILayout.Space(20);
			if (GUILayout.Button("Save overrides to file")) SettingsManager.Instance.SaveModSettings();
			if (GUILayout.Button($"Toggle effects {(tgl_EffectToggle ? "(TURN OFF)" : "(TURN ON)")}")) tgl_EffectToggle = !tgl_EffectToggle;
			if (GUILayout.Button($"Toggle debug vis {(fxModule.debugMode ? "(TURN OFF)" : "(TURN ON)")}")) fxModule.debugMode = !fxModule.debugMode;
			if (GUILayout.Button("Reload assetbundle")) AssetLoader.Instance.ReloadAssets();

			// end
			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		/// <summary>
		/// Info window
		/// </summary>
		void OnInfoWindow(int id)
		{
			// init
			Vessel vessel = FlightGlobals.ActiveVessel;
			if (vessel == null) return;
			var fxModule = vessel.FindVesselModuleImplementing<AtmoFxModule>();
			if (fxModule == null) return;

			if (!fxModule.isLoaded)
			{
				GUILayout.BeginVertical();
				GUILayout.Label("FX are not loaded for the active vessel");
				GUILayout.EndVertical();
				GUI.DragWindow();
				return;
			}

			// drawing
			GUILayout.BeginVertical();

			GUILayout.Label($"Current vessel loaded? {fxModule.isLoaded}");
			GUILayout.Label($"Mod version: beta-{Versioning.Version}. This is a testing-only build.");
			GUILayout.Label($"All assets loaded? {AssetLoader.Instance.allAssetsLoaded}");
			GUILayout.Space(20);

			GUILayout.Label($"Active vessel is {vessel.vesselName}");
			GUILayout.Label($"Vessel radius is {fxModule.fxVessel.vesselBoundRadius}");
			GUILayout.Label($"Effect length multiplier is {fxModule.fxVessel.lengthMultiplier}");
			GUILayout.Label($"Final entry speed is {fxModule.GetAdjustedEntrySpeed()}");
			GUILayout.Space(20);

			GUILayout.Label($"AeroFX scalar is {fxModule.AeroFX.FxScalar}");
			GUILayout.Label($"AeroFX state is {fxModule.AeroFX.state}");
			GUILayout.Label($"AeroFX airspeed is {fxModule.AeroFX.airSpeed}");
			GUILayout.Space(20);

			GUILayout.Label($"Current config is {fxModule.currentBody.bodyName}");
			GUILayout.Space(20);

			// end
			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		/// <summary>
		/// Draws a config field with a toggle switch
		/// This variant uses a dict instead of a toggle reference
		/// </summary>
		/// <param name="label">Label to show</param>
		/// <param name="tgl">The dict contatining the toggle values</param>
		/// <returns>The apply button state</returns>
		void DrawConfigFieldBool(string label, Dictionary<string, ModSettings.Field> tgl)
		{
			tgl[label].value = GUILayout.Toggle((bool)tgl[label].value, label);
		}

		void DrawConfigFieldFloat(string label, Dictionary<string, ModSettings.Field> tgl)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label);

			string text = GUILayout.TextField(((float)tgl[label].value).ToString());
			bool hasValue = float.TryParse(text, out float value);
			if (hasValue) tgl[label].value = value;

			GUILayout.EndHorizontal();
		}
	}
}
