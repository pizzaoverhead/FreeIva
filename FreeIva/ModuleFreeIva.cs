﻿using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

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

		public override string GetModuleDisplayName()
		{
			return "FreeIVA";
		}

		public override string GetInfo()
		{
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

		public override void OnStart(StartState state)
		{
			if (!HighLogic.LoadedSceneIsFlight) return;

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
	}
}
