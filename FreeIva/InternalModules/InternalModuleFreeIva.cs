using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace FreeIva
{
	public class InternalModuleFreeIva : InternalModule
	{
		#region Cache
		private static Dictionary<InternalModel, InternalModuleFreeIva> perModelCache = new Dictionary<InternalModel, InternalModuleFreeIva>();
		public static InternalModuleFreeIva GetForModel(InternalModel model)
		{
			if (model == null) return null;
			perModelCache.TryGetValue(model, out InternalModuleFreeIva module);
			return module;
		}

		public static void RefreshDepthMasks()
		{
			bool internalModeActive = CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.Flight;

			foreach (var ivaModule in perModelCache.Values)
			{
				if (ivaModule.externalDepthMask == ivaModule.internalDepthMask && ivaModule.externalDepthMask != null)
				{
					ivaModule.externalDepthMask.gameObject.SetActive(true);
					continue;
				}

				if (ivaModule.externalDepthMask != null)
				{
					ivaModule.externalDepthMask.gameObject.SetActive(!internalModeActive);
				}

				if (ivaModule.internalDepthMask != null)
				{
					ivaModule.internalDepthMask.gameObject.SetActive(internalModeActive);
				}
			}
		}

		public static void RefreshInternals()
		{
			foreach (var ivaModule in perModelCache.Values)
			{
				if (ivaModule.vessel != FlightGlobals.ActiveVessel)
				{
					ivaModule.part.DespawnIVA();
				}
				else if (ivaModule.isActiveAndEnabled)
				{
					foreach (var hatch in ivaModule.Hatches)
					{
						var otherHatch = hatch.ConnectedHatch;
						if (otherHatch == null || otherHatch.vessel != hatch.vessel)
						{
							hatch.SetOpened(false, false);
						}
						hatch.RefreshConnection();
					}
				}
			}
		}

		#endregion

		[KSPField]
		public bool CopyPartCollidersToInternalColliders = false;

		public List<FreeIvaHatch> Hatches = new List<FreeIvaHatch>(); // hatches will register themselves with us

		List<CutParameter> cutParameters = new List<CutParameter>();

		[KSPField]
		public string secondaryInternalName = string.Empty;
		public InternalModel SecondaryInternalModel { get; private set; }

		public ICentrifuge Centrifuge { get; private set; }
		public IDeployable Deployable { get; private set; }

		public bool NeedsDeployable;

		[KSPField]
		public string externalDepthMaskFile = string.Empty;
		[KSPField]
		public string externalDepthMaskName = string.Empty;
		public Transform externalDepthMask;

		[KSPField]
		public string internalDepthMaskName = string.Empty;
		public Transform internalDepthMask;

		[KSPField]
		public Vector3 customGravity = Vector3.zero;

		[KSPField]
		public string autoCutoutTargetName = string.Empty; // for auto-hatch configuration, target this gameobject for cutouts

		[SerializeField]
		public Bounds ShellColliderBounds;

		#region OnLoad code

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			OnLoad_ShellCollider(node);
			OnLoad_CopyColliders();

			foreach (var disableColliderNode in node.GetNodes("DisableCollider"))
			{
				DisableCollider.DisableColliders(internalProp, disableColliderNode);
			}

			foreach (var reparentNode in node.GetNodes("Reparent"))
			{
				ReparentUtil.Reparent(internalProp, reparentNode);
			}

			foreach (var deleteObjectsNode in node.GetNodes("DeleteInternalObject"))
			{
				DeleteInternalObject.DeleteObjects(internalProp, deleteObjectsNode);
			}

			OnLoad_Shadows(node);

			OnLoad_DepthMasks();
			OnLoad_Windows(node);
			OnLoad_MeshCuts(node);
		}

		private void OnLoad_CopyColliders()
		{
			if (CopyPartCollidersToInternalColliders)
			{
				var partBoxColliders = GetComponentsInChildren<BoxCollider>();

				if (partBoxColliders.Length > 0)
				{
					foreach (var c in partBoxColliders)
					{
						if (c.isTrigger || c.tag != "Untagged")
						{
							continue;
						}

						var go = Instantiate(c.gameObject);
						go.transform.parent = internalModel.transform;
						go.layer = (int)Layers.Kerbals;
						go.transform.position = InternalSpace.WorldToInternal(c.transform.position);
						go.transform.rotation = InternalSpace.WorldToInternal(c.transform.rotation);
					}
				}
			}
		}

		private void OnLoad_Shadows(ConfigNode node)
		{
			foreach (var shadowsNode in node.GetNodes("Shadows"))
			{
				foreach (var value in shadowsNode.values.values)
				{
					var transform = TransformUtil.FindInternalModelTransform(internalModel, value.name);
					if (transform != null)
					{
						var meshRenderer = transform.GetComponent<MeshRenderer>();
						if (meshRenderer != null)
						{
							if (Enum.TryParse(value.value, out ShadowCastingMode shadowCastingMode))
							{
								meshRenderer.shadowCastingMode = shadowCastingMode;
							}
							else
							{
								string validNames = string.Join(", ", Enum.GetNames(typeof(ShadowCastingMode)));
								Debug.LogError($"[FreeIva] INTERNAL '{internalModel.internalName}' invalid shadow casting mode for transform '{value.name}': '{value.value}'. Valid names: {validNames}");
							}
						}
					}
				}
			}
		}

		private void OnLoad_ShellCollider(ConfigNode node)
		{
			// Find the bounds of the shell colliders, so that we can tell when the player exits the bounds of a centrifuge
			ShellColliderBounds.center = Vector3.zero;
			ShellColliderBounds.size = -Vector3.one;
			var shellColliderNodes = node.GetValues("shellColliderName");
			foreach (var shellColliderName in shellColliderNodes)
			{
				var transform = TransformUtil.FindInternalModelTransform(internalModel, shellColliderName);
				if (transform != null)
				{
					var colliders = transform.GetComponentsInChildren<MeshCollider>();
					foreach (var meshCollider in colliders)
					{
						ShellColliderBounds.Encapsulate(meshCollider.bounds);
						meshCollider.convex = false;
					}

					if (colliders.Length == 0)
					{
						Debug.LogError($"[FreeIva] shellCollider {shellColliderName} in internal {internalModel.internalName} exists but does not have a MeshCollider");
					}
				}
				else
				{
					Debug.LogError($"[FreeIva] shellCollider {shellColliderName} not found in internal {internalModel.internalName}");
				}
			}

			// if there's no shell collider, try just finding any colliders on layer 16
			if (shellColliderNodes.Length == 0)
			{
				foreach (var collider in internalModel.transform.GetComponentsInChildren<Collider>())
				{
					if (collider.gameObject.layer == (int)Layers.Kerbals)
					{
						ShellColliderBounds.Encapsulate(collider.bounds);
					}
				}
			}
		}

		private void OnLoad_DepthMasks()
		{
			if (internalDepthMaskName != string.Empty)
			{
				internalDepthMask = TransformUtil.FindInternalModelTransform(internalModel, internalDepthMaskName);
			}

			if (externalDepthMaskName != string.Empty)
			{
				externalDepthMask = TransformUtil.FindInternalModelTransform(internalModel, externalDepthMaskName);
			}
			else if (externalDepthMaskFile != string.Empty)
			{
				externalDepthMask = TransformUtil.FindModelFile(internalModel.gameObject.transform, externalDepthMaskFile);
			}

			// try to find the external depth mask from existing meshes, based on the shader
			if (externalDepthMask == null)
			{
				var modelTransform = internalModel.gameObject.transform.Find("model");
				if (modelTransform != null)
				{
					// find all the depth mask renderers that aren't a child of the internal depth mask (if there is one)
					var allRenderers = modelTransform.GetComponentsInChildren<MeshRenderer>();

					var rendererGroups = allRenderers.GroupBy(meshRenderer =>
						meshRenderer.material.shader == Utils.GetDepthMask() &&
						(internalDepthMask == null || !meshRenderer.transform.IsChildOf(internalDepthMask)));

					var depthMaskRenderers = rendererGroups.Where(group => group.Key).FirstOrDefault();

					if (depthMaskRenderers != null && depthMaskRenderers.Any())
					{
						externalDepthMask = depthMaskRenderers.First().transform;
						// need to find the common ancestor of all the depth mask renderers
						foreach (var renderer in depthMaskRenderers)
						{
							renderer.gameObject.layer = (int)Layers.InternalSpace;

							while (!renderer.transform.IsChildOf(externalDepthMask))
							{
								externalDepthMask = externalDepthMask.parent;
							}
						}

						// if any of the OTHER renderers are also a child of this transform, we can't use it
						var otherRenderers = rendererGroups.Where(group => !group.Key).FirstOrDefault();
						if (otherRenderers != null)
						{
							if (otherRenderers.Any(otherRenderer => otherRenderer.transform.IsChildOf(externalDepthMask)))
							{
								Debug.LogWarning($"[FreeIva] INTERNAL '{internalModel.internalName}' auto-detected depth mask common ancestor '{externalDepthMask.name}' also has non-depth-mask renderers; cannot be used");
								externalDepthMask = null;
							}
						}
						else
						{
							Debug.Log($"[FreeIva] INTERNAL '{internalModel.internalName}' auto-detected external depth mask transform '{externalDepthMask.name}'");
						}
					}
					else
					{
						Debug.LogWarning($"[FreeIva] could not auto-detect depth mask for INTERNAL '{internalModel.internalName}' - no matching MeshRenderers found");
					}
				}
			}

			// try to generate an internal depth mask from the internal geometry
#if false
			if (internalDepthMask == null)
			{
				var stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start();
				Profiler.BeginSample("DepthMaskHull");

				var convexHullCalculator = new GK.ConvexHullCalculator();

				var currentVertices = new List<Vector3>();
				var modelTransform = internalModel.transform.Find("model");

				foreach (var meshRenderer in modelTransform.GetComponentsInChildren<MeshRenderer>())
				{
					var meshFilter = meshRenderer.GetComponent<MeshFilter>();

					Vector3 meshPosition = modelTransform.InverseTransformPoint(meshRenderer.transform.position);
					Quaternion meshRotation = Quaternion.Inverse(modelTransform.rotation) * meshRenderer.transform.rotation;

					if (meshPosition == Vector3.zero && Quaternion.Dot(meshRotation, Quaternion.identity) > 0.999f)
					{
						currentVertices.AddRange(meshFilter.mesh.vertices);
					}
					else
					{
						currentVertices.Capacity += meshFilter.mesh.vertices.Length;
						foreach (var vertex in meshFilter.mesh.vertices)
						{
							currentVertices.Add(meshRotation * vertex + meshPosition);
						}
					}
				}

				List<Vector3> newVerts = null;
				List<int> newIndices = null;
				List<Vector3> newNormals = null;

				if (currentVertices.Count > 0)
				{
					try
					{
						convexHullCalculator.GenerateHull(currentVertices, false, ref newVerts, ref newIndices, ref newNormals);

						var newMesh = new Mesh();
						newMesh.vertices = newVerts.ToArray();
						newMesh.triangles = newIndices.ToArray();

						internalDepthMask = new GameObject("InternalDepthMask").transform;
						internalDepthMask.SetParent(internalModel.transform, false);
						// internalDepthMask.localScale = Vector3.one * 1.001f;
						internalDepthMask.gameObject.AddComponent<MeshFilter>().mesh = newMesh;
						internalDepthMask.gameObject.AddComponent<MeshRenderer>();
						internalDepthMask.gameObject.layer = (int)Layers.InternalSpace;
					}
					catch (Exception ex)
					{
						Debug.LogException(ex);
					}
				}

				Profiler.EndSample();
				stopwatch.Stop();

				if (internalDepthMask != null)
				{
					Debug.Log($"[FreeIVA] depth mask convex hull for {internalModel.internalName}; {newVerts.Count} verts; {newIndices.Count / 3} triangles; {stopwatch.Elapsed.TotalMilliseconds}ms");
				}
			}
#endif

			if (internalDepthMask != null)
			{
				foreach (var meshRenderer in internalDepthMask.GetComponentsInChildren<MeshRenderer>())
				{
					meshRenderer.sharedMaterial = Utils.GetDepthMaskCullingMaterial();
					meshRenderer.gameObject.layer = (int)Layers.InternalSpace;
					meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
					meshRenderer.receiveShadows = false;
				}
			}
		}

		static Dictionary<Shader, Shader> x_windowShaderTranslations = null;
		static Shader[] x_windowShaders = null;

		// transparents are normally 3000, opaque geometry is 2000
		// we need something that will render before opaque geometry so that it writes to the z-buffer early and prevents other internals from drawing behind it
		public static readonly int WINDOW_RENDER_QUEUE = 1999;

		private void OnLoad_Windows(ConfigNode node)
		{
			bool hasWindows = false;

			if (x_windowShaderTranslations == null)
			{
				// for transforms specified as windows, we might need to change their shader to one that does z-write
				x_windowShaderTranslations = new Dictionary<Shader, Shader>()
				{
					{Shader.Find("Unlit/Transparent"), Shader.Find("KSP/Alpha/Unlit Transparent")},
				};
			}

			if (x_windowShaders == null)
			{
				x_windowShaders = new Shader[]
				{
					Shader.Find("KSP/Alpha/Translucent Specular"),
					Shader.Find("KSP/Alpha/Translucent"),
					Shader.Find("KSP/Alpha/Unlit Transparent"),
				};
			}

			var windowNames = node.GetValues("windowName");
			foreach (var windowName in windowNames)
			{
				var windowTransform = TransformUtil.FindInternalModelTransform(internalModel, windowName);
				if (windowTransform != null)
				{
					foreach (var meshRenderer in windowTransform.GetComponentsInChildren<MeshRenderer>())
					{
						if (x_windowShaderTranslations.TryGetValue(meshRenderer.material.shader, out Shader newShader))
						{
							meshRenderer.material.shader = newShader;
						}

						meshRenderer.material.renderQueue = WINDOW_RENDER_QUEUE;
					}

					hasWindows = true;
				}
			}

			// if there aren't any window names specified, try to find them by shader (unless we have an internal depth mask)
			bool intentionallyLeftBlank = internalDepthMaskName == string.Empty && node.HasValue(nameof(internalDepthMaskName));
			if (!windowNames.Any() && internalDepthMask == null && !intentionallyLeftBlank)
			{
				var modelTransform = internalModel.transform.Find("model");
				foreach (var meshRenderer in modelTransform.GetComponentsInChildren<MeshRenderer>())
				{
					if (x_windowShaders.Contains(meshRenderer.material.shader))
					{
						hasWindows = true;
						meshRenderer.material.renderQueue = WINDOW_RENDER_QUEUE;
						meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						Debug.Log($"[FreeIva] INTERNAL '{internalModel.internalName}' auto-detected window transform '{meshRenderer.transform.name}'");
					}
				}
			}

			if (internalDepthMask == null && !hasWindows && !intentionallyLeftBlank)
			{
				Debug.LogWarning($"[FreeIva] INTERNAL '{internalModel.internalName}' has neither an internal depth mask nor detectable windows.  It may be possible to see the internals of other parts from here.");
			}
		}

		static Quaternion x_partToInternalSpace = Quaternion.Euler(90, 0, 180);
		static Quaternion x_internalToPartSpace = Quaternion.Inverse(x_partToInternalSpace);

		void OnLoad_Finalize()
		{
			// try to find the part associated with this model (note there might be more than one...)
			// should we only do this if there are no HatchConfigs in any of the props?  I'm concerned about loading times, but probably should measure first
			Part part = ModuleFreeIva.GetPartPrefabForInternal(internalModel.internalName);

			List<FreeIvaHatch> hatches = new List<FreeIvaHatch>();

			foreach (var prop in internalModel.props)
			{
				var hatch = prop.GetComponent<FreeIvaHatch>();
				if (hatch != null)
				{
					var hatchConfig = prop.GetComponent<HatchConfig>();

					if (hatchConfig != null || hatch.cutoutTargetTransformName != string.Empty)
					{
						AddPropCut(hatch);
					}
					else
					{
						// try to auto-configure this prop
						hatches.Add(hatch);

						// for cutouts....
						if (autoCutoutTargetName != string.Empty && hatch.cutoutTransformName != string.Empty)
						{
							hatch.cutoutTargetTransformName = autoCutoutTargetName;
							AddPropCut(hatch);
						}

						// for attachnodes...
						if (part != null && hatch.tubeTransform != null && hatch.attachNodeId == string.Empty)
						{
							foreach (var attachNode in part.attachNodes)
							{
								Vector3 attachNodeInternalSpace = x_partToInternalSpace * attachNode.position;
								Vector3 localPosition = hatch.tubeTransform.InverseTransformPoint(attachNodeInternalSpace);

								if (localPosition.y >= -1 && localPosition.y <= 0)
								{
									localPosition.y = 0;
									if (localPosition.sqrMagnitude < 0.1f)
									{
										Debug.Log($"[FreeIva] INTERNAL '{internalModel.internalName}' hatch PROP '{prop.propName}' at {prop.transform.position} auto-detected attachnode '{attachNode.id}'");
										hatch.attachNodeId = attachNode.id;
										break;
									}
								}
							}
						}
					}
				}
			}

			// auto-configure airlocks
			// since hatches do not have a defined "out" direction (different hatches are authored in different orientations), we do this in part space (because the airlocks DO have a defined "in" direction)
			Transform[] airlocks = part?.airlock == null ? null : part.FindModelTransformsWithTag(FreeIvaHatch.AIRLOCK_TAG);
			string[] airlockNames = airlocks == null ? null : new string[airlocks.Length];

			if (airlocks != null)
			{
				// get a unique name for each airlock
				var countByName = new Dictionary<string, int>(airlocks.Length);
				for (int i = 0; i < airlocks.Length; i++)
				{
					string name = airlocks[i].name;
					if (countByName.TryGetValue(name, out int count))
					{
						airlockNames[i] = name + "," + count;
						++count;
						countByName[name] = count;
					}
					else
					{
						airlockNames[i] = name;
						countByName[name] = 1;
					}
				}

				for (int airlockIndex = 0; airlockIndex < airlocks.Length; airlockIndex++)
				{
					var airlock = airlocks[airlockIndex];
					float bestDistanceSquared = 1; // we're pretty generous with positioning (compared to attachnodes) because these don't need to line up perfectly
					FreeIvaHatch bestHatch = null;

					foreach (var hatch in hatches)
					{
						if (hatch.airlockName != string.Empty) continue;

						Vector3 hatchInPartSpace = x_internalToPartSpace * hatch.transform.position;
						
						// NOTE: some airlocks have non-identity scale (e.g. mk2LanderCan from restock) so InverseTransformPoint doesn't work
						Vector3 airlockToHatch = hatchInPartSpace - airlock.transform.position;
						Vector3 hatchInAirlockSpace = airlock.transform.InverseTransformDirection(airlockToHatch);

						// airlock's +Z is toward the part (the way the kerbal faces)
						if (hatchInAirlockSpace.z >= 0 && hatchInAirlockSpace.z <= 1)
						{
							hatchInAirlockSpace.z = 0;
							if (hatchInAirlockSpace.sqrMagnitude <= bestDistanceSquared)
							{
								bestDistanceSquared = hatchInAirlockSpace.sqrMagnitude;
								bestHatch = hatch;
							}
						}
					}

					if (bestHatch != null)
					{
						bestHatch.airlockName = airlockNames[airlockIndex];
						Debug.Log($"[FreeIva] INTERNAL '{internalModel.internalName}' hatch PROP '{bestHatch.internalProp.propName}' at {bestHatch.internalProp.transform.position} auto-detected airlock '{bestHatch.airlockName}'");
					}
				}
			}

			// TODO: auto-configure for docking ports...

			ExecuteMeshCuts();
		}

		private void OnLoad_MeshCuts(ConfigNode node)
		{
			var cutNodes = node.GetNodes("Cut");
			foreach (var cutNode in cutNodes)
			{
				CutParameter cp = CutParameter.LoadFromCfg(cutNode);

				if (cp != null)
				{
					cutParameters.Add(cp);
				}
			}
		}

		private void AddPropCut(FreeIvaHatch hatch)
		{
			if (hatch.cutoutTransformName != string.Empty)
			{
				var tool = TransformUtil.FindPropTransform(hatch.internalProp, hatch.cutoutTransformName);
				if (tool != null)
				{
					CutParameter cp = new CutParameter();
					cp.target = hatch.cutoutTargetTransformName;
					cp.tool = tool.gameObject;
					cp.type = CutParameter.Type.Mesh;
					cutParameters.Add(cp);
				}
				else
				{
					Debug.LogError($"[FreeIva] could not find cutout transform {hatch.cutoutTransformName} on prop {hatch.internalProp.propName}");
				}
			}
		}

		void ExecuteMeshCuts()
		{
			if (HighLogic.LoadedScene != GameScenes.LOADING) return;

			if (cutParameters.Any())
			{
				MeshCutter.CreateToolsForCutParameters(internalModel, cutParameters);
				MeshCutter.CutInternalModel(internalModel, cutParameters);


				// this code is only necessary for the internal depth masks that were generated via convex hull.  We're not doing that anymore
#if false
				if (internalDepthMask != null)
				{
					MeshCutter.ApplyCut(internalDepthMask, cutParameters);

					var mesh = internalDepthMask.GetComponent<MeshFilter>().mesh;

					Debug.Log($"[FreeIva] after cutting internal depth mask: {mesh.vertices.Length} verts; {mesh.triangles.Length / 3} tris");
				}
#endif
			}

			MeshCutter.DestroyTools(ref cutParameters);
		}

		#endregion

		InternalModel CreateInternalModel(string internalName)
		{
			InternalModel internalPart = PartLoader.GetInternalPart(internalName);
			if (internalPart == null)
			{
				Debug.LogError($"[FreeIva] Could not find INTERNAL named '{internalName}' referenced from INTERNAL '{internalModel.name}'");
				return null;
			}
			var result = UnityEngine.Object.Instantiate(internalPart);
			result.gameObject.name = internalPart.internalName + " interior";
			result.gameObject.SetActive(value: true);
			if (result == null)
			{
				Debug.LogError($"[FreeIva] Failed to instantiate INTERNAL named '{internalName}' referenced from INTERNAL '{internalModel.name}'");
				return null;
			}
			result.part = part;
			result.Load(new ConfigNode());
			
			// InternalModule.Initialize will try to seat the crew, but we don't want to do that for secondary internal models
			var partCrew = part.protoModuleCrew;
			part.protoModuleCrew = new List<ProtoCrewMember>();
			result.Initialize(part);
			part.protoModuleCrew = partCrew;
			return result;
		}

		void Start()
		{
			if (!HighLogic.LoadedSceneIsFlight) return;

			if (secondaryInternalName != string.Empty)
			{
				SecondaryInternalModel = CreateInternalModel(secondaryInternalName);
			}

			var partModule = part.FindModuleImplementing<ModuleFreeIva>();

			if (partModule == null)
			{
				Debug.LogError($"[FreeIva] INTERNAL '{internalModel.internalName}' used in PART '{part.partInfo.name}' but it does not have a ModuleFreeIva");
			}
			else
			{
				partModule.OnInternalCreated(this);
				
				// the rotating part of the centrifuge has a secondary internal (which is the stationary part)
				// for now we'll only set up the centrifuge module on the rotating part
				if (SecondaryInternalModel != null || partModule.Centrifuge != null)
				{
					Centrifuge = partModule.Centrifuge;
				}

				if (SecondaryInternalModel != null && Centrifuge == null)
				{
					Debug.LogError($"[FreeIva] Could not find a centrifuge module in INTERNAL '{internalModel.internalName}' for PART '{part.partInfo.name}'");
				}

				Deployable = partModule.Deployable;

				if (NeedsDeployable && Deployable == null)
				{
					Debug.LogError($"[FreeIva] Could not find a module to handle deployment in INTERNAL '{internalModel.internalName}' for PART '{part.partInfo.name}'");
				}
			}
		}

		static Vector3 InternalToWorld(Vector3 internalSpacePosition)
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				return InternalSpace.InternalToWorld(internalSpacePosition);
			}
			else
			{
				return x_internalToPartSpace * internalSpacePosition + EditorLogic.RootPart.transform.position;
			}
		}

		static Quaternion InternalToWorld(Quaternion internalSpaceRotation)
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				return InternalSpace.InternalToWorld(internalSpaceRotation);
			}
			else
			{
				return x_internalToPartSpace * EditorLogic.RootPart.transform.rotation;
			}
		}

		static Vector3 WorldToInternal(Vector3 worldSpacePosition)
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				return InternalSpace.WorldToInternal(worldSpacePosition);
			}
			else
			{
				return x_partToInternalSpace * (worldSpacePosition - EditorLogic.RootPart.transform.position);
			}
		}

		static Quaternion WorldToInternal(Quaternion worldSpaceRotation)
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				return InternalSpace.WorldToInternal(worldSpaceRotation);
			}
			else
			{
				return x_partToInternalSpace * worldSpaceRotation;
			}
		}

		void Update()
		{
			this.internalModel.transform.position = WorldToInternal(part.transform.position);
			this.internalModel.transform.rotation = WorldToInternal(part.transform.rotation) * Quaternion.Euler(90, 0, 180);

			if (Centrifuge != null)
			{
				Centrifuge.Update();
			}
		}

		void OnEnable()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				if (SecondaryInternalModel != null)
				{
					SecondaryInternalModel.gameObject.SetActive(true);
				}
			}
		}

		void OnDisable()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				if (SecondaryInternalModel != null)
				{
					SecondaryInternalModel.gameObject.SetActive(false);
				}
			}
			// dirty hack: the stock internal model loader will deactivate the internal space when it's done loading, so this gives us a place to do "final processing" after all props have been added
			else if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				OnLoad_Finalize();
			}
		}

		new void Awake()
		{
			if (HighLogic.LoadedScene == GameScenes.FLIGHT)
			{
				perModelCache[internalModel] = this;
			}
			base.Awake();
		}

		void OnDestroy()
		{
			perModelCache.Remove(internalModel);
			if (SecondaryInternalModel != null)
			{
				GameObject.Destroy(SecondaryInternalModel.gameObject);
			}
		}
	}
}
