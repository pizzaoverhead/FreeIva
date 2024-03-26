using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;
using System.Linq;

namespace FreeIva
{
	/// <summary>
	/// This class is currently empty, but in the future it may contain data to persist hatch state etc across saves.
	/// </summary>
	public class ModuleFreeIva : PartModule
	{
		private static string str_CanTraverse = Localizer.Format("#FreeIVA_PartInfo_CanTraverse");
		private static string str_NotBlockHatch = Localizer.Format("#FreeIVA_PartInfo_NotBlockHatch");
		[KSPField]
		public string passThroughNodeA = string.Empty;
		[KSPField]
		public string passThroughNodeB = string.Empty;

		[KSPField]
		public bool doesNotBlockEVA = false;

		[KSPField]
		public bool allowsUnbuckling = true;

		[KSPField]
		public string partInfo = string.Empty;

		[KSPField]
		public bool forceInternalCreation = false;
		[KSPField]
		public bool requireDeploy = false;

		[KSPField]
		public string deployAnimationName = string.Empty;

		[KSPField]
		public string centrifugeTransformName = string.Empty;
		[KSPField]
		public Vector3 centrifugeAlignmentRotation = new Vector3(180, 0, 180);

		[KSPEvent(guiActiveEditor = true)]
		public void ActivateInEditor()
		{
			FreeIva.EnableInternals();
		}

		public IDeployable Deployable
		{
			get; private set;
		}

		public ICentrifuge Centrifuge
		{
			get; private set;
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			foreach (var cfgValue in node.values.values)
			{
				switch (cfgValue.name)
				{
					case "deleteObject":
						var obj = TransformUtil.FindPartTransform(part, cfgValue.value);
						if (obj != null)
						{
							obj.SetParent(null, false);
							GameObject.Destroy(obj.gameObject);
						}
						break;
				}
			}
		}

		void OnLoadFinalize()
		{
			// first time?
			if (x_internalNameToPartPrefab == null)
			{
				x_internalNameToPartPrefab = new Dictionary<string, HashSet<Part>>();
				x_revivaIsInstalled = AssemblyLoader.loadedAssemblies.Contains("Reviva");
			}

			string internalName = part.partInfo?.internalConfig?.GetValue("name");

			if (internalName != null)
			{
				AddInternalForPart(internalName, part);
			}

			if (x_revivaIsInstalled)
			{
				foreach (var moduleNode in part.partInfo.partConfig.nodes.nodes)
				{
					if (moduleNode.name != "MODULE") continue;
					if (moduleNode.GetValue("name") != "ModuleB9PartSwitch") continue;

					foreach (var subtypeNode in moduleNode.nodes.nodes)
					{
						if (subtypeNode.name != "SUBTYPE") continue;
						var subtypeModuleNode = subtypeNode.GetNode("MODULE");
						if (subtypeModuleNode == null) continue;

						var identifierNode = subtypeModuleNode.GetNode("IDENTIFIER");
						var dataNode = subtypeModuleNode.GetNode("DATA");

						string moduleName = identifierNode?.GetValue("name");
						internalName = dataNode?.GetValue("internalName");

						if (moduleName == null || internalName == null || moduleName != "ModuleIVASwitch") continue;

						AddInternalForPart(internalName, part);
					}
				}
			}
		}

		static void AddInternalForPart(string internalName, Part partPrefab)
		{
			if (!x_internalNameToPartPrefab.TryGetValue(internalName, out var partList))
			{
				partList = new HashSet<Part>();
				x_internalNameToPartPrefab.Add(internalName, partList);
			}

			partList.Add(partPrefab);
		}

		internal static HashSet<Part> GetPartPrefabsForInternal(string internalName)
		{
			if (x_internalNameToPartPrefab.TryGetValue(internalName, out var partList))
			{
				return partList;
			}

			Debug.LogWarningFormat($"[FreeIva] No parts referencing internal '{internalName}' were found; autodetection will not work");
			return null;
		}

		internal static Part GetPartPrefabForInternal(string internalName)
		{
			var partPrefabs = ModuleFreeIva.GetPartPrefabsForInternal(internalName);

			if (partPrefabs == null) return null;

			var part = partPrefabs.First();

			if (partPrefabs.Count > 1)
			{
				Debug.LogWarning($"[FreeIva] multiple parts are referencing INTERNAL '{internalName}'; using the first one for autodetection: {part.partInfo.name}");
			}

			return part;
		}

		// for a given internal name, tracks which parts use it
		// this gets used for hatch and airlock auto-detection when compiling internal spaces
		static Dictionary<string, HashSet<Part>> x_internalNameToPartPrefab;
		static bool x_revivaIsInstalled;

		public override string GetModuleDisplayName()
		{
			return "FreeIVA";
		}

		public override string GetInfo()
		{
			// HACK: GetInfo gets called towards the end of part compilation, after everything has been added to the prefab
			// So we hijack this call as a spot to do loading work that needs everything set up
			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				OnLoadFinalize();
			}

			if (!allowsUnbuckling) return string.Empty;

			string result = partInfo == string.Empty ? str_CanTraverse : partInfo; 

			if (doesNotBlockEVA)
			{
				string hatchInfo = str_NotBlockHatch;

				if (passThroughNodeA == string.Empty)
				{
					result = hatchInfo;
				}
				else
				{
					result += "\n" + hatchInfo;
				}
			}

			return result;
		}

		// NOTE: beware of interacting with other PartModules in here, because they may not have been started yet
		// In particular the Kerbalism GravityRing is a little tricky
		public override void OnStart(StartState state)
		{
			if (!(HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)) return;

			Centrifuge = CentrifugeFactory.Create(part, centrifugeTransformName, centrifugeAlignmentRotation);
			Deployable = Centrifuge as IDeployable; // some centrifuges may also be deployables

			if (Deployable == null)
			{
				Deployable = DeployableFactory.Create(part, deployAnimationName);
			}

			if ((requireDeploy || deployAnimationName != string.Empty) && Deployable == null)
			{
				Debug.LogError($"[FreeIva] no deployable module found on part {part.partInfo.name}");
			}
		}

		public void OnInternalCreated(InternalModuleFreeIva internalModule)
		{
			// try to find a centrifuge and deployable modules for the *primary* iva only so this doesn't happen twice for centrifuges
			if (internalModule.internalModel == part.internalModel)
			{
				if (Centrifuge != null)
				{
					Centrifuge.OnInternalCreated();
				}
				if (Deployable != null && Deployable != Centrifuge)
				{
					Deployable.OnInternalCreated();
				}
			}
		}
	}
}
