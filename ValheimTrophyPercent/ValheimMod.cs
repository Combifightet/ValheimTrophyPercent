using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ValheimTrophyPercent
{
	[BepInPlugin("dickdangerjustice.ValheimMod", "Valheim Mod", "1.0.0")]
	[BepInProcess("valheim.exe")]
	public class ValheimMod : BaseUnityPlugin
	{
		private readonly Harmony harmony = new Harmony("dickdangerjustice.ValheimMod");

		void Awake()
		{
			harmony.PatchAll();
		}

	}
}

