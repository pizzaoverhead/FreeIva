using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace FreeIva
{
	/* Stock EVA setup:
        KerbalEVA is on layer 17 (EVA).
        capsuleCollider:
            height 0.47
            radius: 0.12
        helmetAndHeadCollider: Sphere
            radius: 0.27
    */

	/// <summary>
	/// Character controller for IVA movement.
	/// </summary>
	[KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
	public class KerbalIvaAddon : MonoBehaviour
	{
		// Localisation strings
		private static string str_GravityEnabled = Localizer.Format("#FreeIVA_Message_GravityEnabled");
		private static string str_GravityDisabled = Localizer.Format("#FreeIVA_Message_GravityDisabled");
		private static string str_Unbuckle = Localizer.Format("#FreeIVA_Message_Unbuckle");
		private static string str_Buckled = Localizer.Format("#FreeIVA_Message_Buckled");
		private static string str_PartCannotUnbuckle = Localizer.Format("#FreeIVA_Message_PartCannotUnbuckle");
		private static string str_Unbuckled = Localizer.Format("#FreeIVA_Message_Unbuckled");
		private static string str_EnterSeat = Localizer.Format("#FreeIVA_Message_EnterSeat");
		private static string str_HatchLocked = Localizer.Format("#FreeIVA_Message_HatchLocked");
		private static string str_GoEVA = Localizer.Format("#FreeIVA_Message_GoEVA");
		private static string str_HatchBlocked = Localizer.Format("#FreeIVA_Message_HatchBlocked");
		private static string str_CloseHatch = Localizer.Format("#FreeIVA_Message_CloseHatch");
		private static string str_OpenHatch = Localizer.Format("#FreeIVA_Message_OpenHatch");
        public KerbalIvaController KerbalIva;
#if Experimental
		public GameObject KerbalWorldSpace;
		public Collider KerbalWorldSpaceCollider;
		public PhysicMaterial KerbalWorldSpacePhysics;
		public Rigidbody KerbalWorldSpaceRigidbody;
		public Renderer KerbalWorldSpaceRenderer;
#endif

		public bool buckled = true;
		public ProtoCrewMember ActiveKerbal => CameraManager.Instance.ivaCameraActiveKerbal?.protoCrewMember;
		public InternalSeat OriginalSeat = null;
		public InternalSeat TargetedSeat = null;
		public bool Gravity = true;
		public static Vector3 flightForces;

		private CameraManager.CameraMode _lastCameraMode = CameraManager.CameraMode.Flight;
		private bool _changingCurrentIvaCrew = false;

		public static KerbalIvaAddon _instance;
		public static KerbalIvaAddon Instance { get { return _instance; } }

		public delegate void GetInputDelegate(ref IVAInput input);
		public static GetInputDelegate GetInput = GetKeyboardInput;

		ShadowCascadeTweak cascadeTweak;

		void Start()
		{
			CreateCameraCollider();

			// cascadeTweak = InternalCamera.Instance._camera.gameObject.GetOrAddComponent<ShadowCascadeTweak>();

			GameEvents.OnCameraChange.Add(OnCameraChange);
			GameEvents.OnIVACameraKerbalChange.Add(OnIVACameraKerbalChange);
			GameEvents.onVesselChange.Add(OnVesselChange);
			_instance = this;
		}

		void OnDestroy()
		{
			GameObject.Destroy(KerbalIva);
			GameEvents.OnCameraChange.Remove(OnCameraChange);
			GameEvents.OnIVACameraKerbalChange.Remove(OnIVACameraKerbalChange);
			GameEvents.onVesselChange.Remove(OnVesselChange);

			_instance = null;
			KerbalIva = null;
		}

		void OnGUI()
		{
			GuiTutorial.Gui(GetInstanceID());
		}

		public void Update()
		{
			// if we just respawned the crew because we exited ProbeControlRoom, mark them all as "alive" so you don't have to wait 3 seconds to hit C to re-enter IVA
			if (_markCrewAlive)
			{
				if (FlightGlobals.ActiveVessel != null)
				{
					foreach (var pcm in FlightGlobals.ActiveVessel.crew)
					{
						if (pcm.KerbalRef != null && pcm.KerbalRef.state == Kerbal.States.NO_SIGNAL)
						{
							pcm.KerbalRef.state = Kerbal.States.ALIVE;
						}
					}
				}

				_markCrewAlive = false;
			}

			if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(Settings.ToggleGravityKey))
			{
				Gravity = !Gravity && Settings.EnableCollisions;
				ScreenMessages.PostScreenMessage(Gravity ? str_GravityEnabled : str_GravityDisabled, 1f, ScreenMessageStyle.LOWER_CENTER);
			}

			if (!buckled && GameSettings.CAMERA_MODE.GetKeyDown(true))
			{
				ReturnToSeat();
			}

			if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA)
			{
				GuiTutorial.Active = false;
			}
			else
			{
				// Switching to IVA.
				if (_lastCameraMode != CameraManager.CameraMode.IVA)
				{
					GuiTutorial.Active = true;

					FreeIva.EnableInternals();
					UpdateActiveKerbal();

					var freeIvaModule = FreeIva.CurrentPart.GetModule<ModuleFreeIva>();
					if (freeIvaModule != null && freeIvaModule.allowsUnbuckling)
					{
						if (buckled)
						{
							ScreenMessages.PostScreenMessage($"{str_Unbuckle} [{Settings.UnbuckleKey}]", 2f, ScreenMessageStyle.LOWER_CENTER);
						}
						else
						{
							ScreenMessages.PostScreenMessage($"{str_EnterSeat} [{GameSettings.CAMERA_MODE.primary}]", 2f, ScreenMessageStyle.LOWER_CENTER);
						}
					}
				}

				// Check if we're changing crew member using the 'V' key.
				if (GameSettings.CAMERA_NEXT.GetKeyDown())
					_changingCurrentIvaCrew = true;
				else
				{
					if (_changingCurrentIvaCrew)
					{
						UpdateActiveKerbal();
						_changingCurrentIvaCrew = false;
					}
				}

				input = new IVAInput();
				GetInput(ref input);

				JumpLatched = JumpLatched && input.Jump;
				input.Jump = input.Jump && !JumpLatched;
			}
			_lastCameraMode = CameraManager.Instance.currentCameraMode;
		}

		// This must be after the internal camera updates
		private void LateUpdate()
		{
			if (FreeIva.Paused) return;

			ApplyInput(input);

			if (!buckled)
			{
				// Normally the InternalCamera's transform is copied to the FlightCamera at the end of InternalCamera.Update, which will have happened right before this component updates.
				// So we need to make sure the latest internal camera rotation gets copied to the flight camera.
				FlightCamera.fetch.transform.position = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
				FlightCamera.fetch.transform.rotation = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.rotation);
			}
		}

		bool _markCrewAlive = false;
		CameraManager.CameraMode _previousCameraMode = CameraManager.CameraMode.Flight;
		public void OnCameraChange(CameraManager.CameraMode cameraMode)
		{
			if (cameraMode == CameraManager.CameraMode.IVA)
			{
				// this can sometimes get destroyed if it was attached to the internal (like in a centrifuge).  just handle recreating it here.
				if (KerbalIva == null)
				{
					CreateCameraCollider();
				}

				// STOP HIDING INTERNALS DAMMIT
				var ivaOverlay = KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance?.ivaOverlay;
				if (ivaOverlay != null)
				{
					ivaOverlay.onDismiss = null;
				}
				FreeIva.EnableInternals();
			}
			else if (cameraMode != CameraManager.CameraMode.IVA && _previousCameraMode == CameraManager.CameraMode.IVA)
			{
				InternalModuleFreeIva.RefreshDepthMasks();

				InputLockManager.RemoveControlLock("FreeIVA");

				// if ProbeControlRoom was enabled, we may have deactivated all the internal models.  The stock game doesn't expect this, so restore them
				if (cameraMode == CameraManager.CameraMode.Flight && FlightGlobals.ActiveVessel != null)
				{
					foreach (var p in FlightGlobals.ActiveVessel.parts)
					{
						if (!FreeIva.PartIsProbeCore(p) && p.internalModel != null && !p.internalModel.gameObject.activeSelf)
						{
							p.internalModel.gameObject.SetActive(true);
							p.internalModel.SpawnCrew();
							_markCrewAlive = true;
						}
					}
				}

#if Experimental
				KerbalWorldSpaceCollider.enabled = false;
#endif
				if (!buckled && ActiveKerbal != null)
				{
					ReturnToSeatInternal(false);
				}
			}
#if Experimental
			if (cameraMode == CameraManager.CameraMode.IVA)
			{
				//KerbalWorldSpaceCollider.enabled = true;
			}
#endif
			_previousCameraMode = cameraMode;
		}

		private void OnIVACameraKerbalChange(Kerbal data)
		{
			UpdateActiveKerbal();
		}

		private void OnVesselChange(Vessel data)
		{
			if (!buckled)
			{
				ReturnToSeatInternal(false);
			}
		}

		static Vector3 WorldDirectionToInternal(Vector3 worldDirection)
		{
			Quaternion direction = Quaternion.LookRotation(worldDirection);
			Quaternion internalDirection = InternalSpace.WorldToInternal(direction);
			return internalDirection * Vector3.forward;
		}

		static void GetAngularVelocity(Part part, out Vector3 centerOfRotationWorld, out Vector3 angularVelocityWorld)
		{
			if (part)
			{
				Rigidbody rb = part.Rigidbody;
				Vector3 localVelocity = rb.velocity - part.vessel.rb_velocity;
				float omegaSquared = rb.angularVelocity.sqrMagnitude;

				centerOfRotationWorld = rb.position;

				if (omegaSquared > 0.0001f)
				{
					centerOfRotationWorld += Vector3.Cross(rb.angularVelocity, localVelocity) / omegaSquared;
				}

				angularVelocityWorld = rb.angularVelocity;
			}
			else
			{
				centerOfRotationWorld = FlightGlobals.ActiveVessel.CoM;
				angularVelocityWorld = FlightGlobals.ActiveVessel.angularVelocity;
			}
		}

		internal static Vector3 GetFlightAccelerationInternalSpace(Part part, Vector3 internalSpacePosition)
		{
			Vector3 accelWorldSpace;

			if (FlightGlobals.ActiveVessel.LandedOrSplashed)
			{
				accelWorldSpace = FlightGlobals.ActiveVessel.graviticAcceleration;
			}
			else
			{
				// linear acceleration
				accelWorldSpace = -FlightGlobals.ActiveVessel.perturbation;

				// angular (centrifugal) acceleration => omega^2 * r
				// this is super fake, it will apply to things that aren't in contact with the rotating part and doesn't account for coriolis, etc.
				GetAngularVelocity(part, out Vector3 centerOfRotationWorld, out Vector3 angularVelocityWorld);

				Vector3 rotationAxis = angularVelocityWorld.normalized;
				Vector3 worldSpacePosition = InternalSpace.InternalToWorld(internalSpacePosition);
				Vector3 fromAxis = Vector3.ProjectOnPlane(worldSpacePosition - centerOfRotationWorld, rotationAxis);
				

				float r = fromAxis.magnitude;
				float omegaSquared = angularVelocityWorld.sqrMagnitude;

				float centrifugalAccelMag = omegaSquared * r;

				if (centrifugalAccelMag > 0.01f)
				{
					accelWorldSpace += fromAxis / r * centrifugalAccelMag;
				}
			}

			float magnitude = accelWorldSpace.magnitude;

			if (magnitude <= 0.01f)
			{
				return Vector3.zero;
			}

			return WorldDirectionToInternal(accelWorldSpace) * magnitude;
		}

		public static Vector3 GetCentrifugeAccel(ICentrifuge centrifuge, Vector3 internalSpacePosition)
		{
			float omega = Mathf.Deg2Rad * Mathf.Abs(centrifuge.CurrentSpinRate);
			if (omega == 0) return Vector3.zero;

			// for now, we'll assume that the centrifuge is spinning around the "top/bottom" axis - Z in local IVA space
			Transform rotationRoot = centrifuge.IVARotationRoot;
			Vector3 localKerbalPosition = rotationRoot.InverseTransformPoint(internalSpacePosition);
			localKerbalPosition.z = 0;

			// centripetal acceleration = omega^2 * r
			Vector3 localAcceleration = omega * omega * localKerbalPosition;

			return rotationRoot.TransformVector(localAcceleration);
		}

		public static Vector3 GetInternalSubjectiveAcceleration(InternalModuleFreeIva ivaModule, Vector3 internalSpacePosition)
		{
			if (!Instance.Gravity)
			{
				return Vector3.zero;
			}
			else if (ivaModule?.Centrifuge != null && ivaModule.Centrifuge.CurrentSpinRate != 0)
			{
				return GetCentrifugeAccel(ivaModule.Centrifuge, internalSpacePosition);
			}
			else if (ivaModule != null && ivaModule.customGravity != Vector3.zero)
			{
				return ivaModule.transform.TransformDirection(ivaModule.customGravity);
			}
			else
			{
				return GetFlightAccelerationInternalSpace(ivaModule?.part, internalSpacePosition);
			}
		}

		public void FixedUpdate()
		{
			if (!buckled)
			{
				KerbalIva.DoFixedUpdate(input);
			}

			input.Jump = false;
		}

		private void CreateCameraCollider()
		{
			KerbalIva = new GameObject("KerbalIvaController").AddComponent<KerbalIvaController>();
			KerbalIva.gameObject.SetActive(false);

#if Experimental
			KerbalWorldSpace = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			KerbalWorldSpace.name = "Kerbal World Space collider";
			KerbalWorldSpace.GetComponentCached<Collider>(ref KerbalWorldSpaceCollider);
			KerbalWorldSpaceCollider.enabled = false; // TODO: Makes vessel explode.
			KerbalWorldSpacePhysics = KerbalWorldSpaceCollider.material;
			KerbalWorldSpaceRigidbody = KerbalWorldSpace.AddComponent<Rigidbody>();
			KerbalWorldSpaceRigidbody.useGravity = false;
			KerbalWorldSpaceRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

			WorldCollisionTracker KerbalWorldCollisionTracker = KerbalWorldSpace.AddComponent<WorldCollisionTracker>();
#if DEBUG
			KerbalWorldSpace.AddComponent<IvaCollisionPrinter>();
#endif
			KerbalWorldCollisionTracker.Initialise(KerbalRigidbody);

			KerbalWorldSpaceCollider.isTrigger = false;
			KerbalWorldSpace.layer = (int)Layers.LocalScenery;
			KerbalWorldSpaceCollider.transform.parent = KerbalCollider.transform;
			KerbalWorldSpace.transform.localScale = new Vector3(Settings.HelmetSize, Settings.HelmetSize, Settings.HelmetSize);
			KerbalWorldSpace.transform.localPosition = new Vector3(0, 0, 0.2f);
			KerbalWorldSpace.GetComponentCached<Renderer>(ref KerbalWorldSpaceRenderer);
			KerbalWorldSpaceRenderer.enabled = true;
#endif
		}

		public struct IVAInput
		{
			public Vector3 MovementThrottle;
			public Vector3 RotationInputEuler;
			public bool Unbuckle;
			public bool Buckle;
			public bool SwitchTo;
			public bool ToggleHatch;
			public bool ToggleFarHatch;
			public bool Jump;
			public bool ToggleCrouch;
		}

		IVAInput input;
		public bool JumpLatched;
		public bool ToggleCrouchLatched;

		private static float GetKeyInputAxis(KeyCode positive, KeyCode negative)
		{
			return (Input.GetKey(positive) ? 1.0f : 0.0f) + (Input.GetKey(negative) ? -1.0f : 0.0f);
		}

		private static void GetKeyboardInput(ref IVAInput input)
		{
			if (KOSPropMonitor.IsLocked())
			{
				return;
			}

			if (Input.GetKeyDown(Settings.UnbuckleKey))
			{
				if (Instance.buckled)
				{
					input.Unbuckle = true;
				}
				else
				{
					input.Buckle = true;
				}
			}

			input.SwitchTo = GameSettings.CAMERA_NEXT.GetKeyDown(true);

			if (!Instance.buckled && !FreeIva.Paused)
			{
				// TODO: add key controls for turning
				input.RotationInputEuler.z = GetKeyInputAxis(Settings.RollCCWKey, Settings.RollCWKey);

				// movement
				{
					input.MovementThrottle.z = GetKeyInputAxis(Settings.ForwardKey, Settings.BackwardKey);
					input.MovementThrottle.x = GetKeyInputAxis(Settings.RightKey, Settings.LeftKey);
					input.MovementThrottle.y = GetKeyInputAxis(Settings.UpKey, Settings.DownKey);

					input.MovementThrottle.Normalize();
				}

				input.Jump = Input.GetKey(Settings.JumpKey);

				if (Input.GetKeyDown(Settings.OpenHatchKey))
				{
					input.ToggleFarHatch = Input.GetKey(Settings.ModifierKey);
					input.ToggleHatch = !input.ToggleFarHatch;
				}

				input.ToggleCrouch = Input.GetKeyDown(Settings.CrouchKey);
			}
		}

		private void ApplyInput(IVAInput input)
		{
			if (input.ToggleCrouch && !ToggleCrouchLatched)
			{
				KerbalIva.targetCrouchFraction = 1.0f - KerbalIva.targetCrouchFraction;
			}

			ToggleCrouchLatched = input.ToggleCrouch;

			if (input.Unbuckle)
			{
				Unbuckle();
			}
			else if (input.Buckle)
			{
				Buckle();
			}
			else if (input.SwitchTo && !buckled)
			{
				SwitchToKerbal();
			}

			if (!buckled && !FreeIva.Paused)
			{
				KerbalIva.UpdateOrientation(input.RotationInputEuler);
				TargetSeats();
				TargetHatches(input.ToggleHatch, input.ToggleFarHatch);
			}
		}

		void PlaySeatBuckleAudio(InternalSeat seat)
		{
			// this sucks: the buckle props aren't actually directly connected to the seat object
			// because in most IVAs, the seat is a standalone prop and the InternalSeat module is just placed directly in the internal
			// maybe we should fix that on loading or something?
			foreach (var buckleButton in seat.internalModel.GetComponentsInChildren<PropBuckleButton>())
			{
				if (Vector3.Distance(buckleButton.transform.position, seat.seatTransform.position) < 2f)
				{
					buckleButton.PlayBuckleSound();
					break;
				}
			}
		}


		//private bool _reseatingCrew = false;
		private void BuckleInternal(bool resetCamera)
		{
			if (TargetedSeat == null || (TargetedSeat.taken && TargetedSeat.crew != ActiveKerbal))
				return;

			Debug.Log(ActiveKerbal.name + " is entering seat " + TargetedSeat.transform.name + " in part " + FreeIva.CurrentPart);

			buckled = true;
			MoveKerbalToSeat(ActiveKerbal, TargetedSeat);

			if (resetCamera)
			{
				InternalCamera.Instance.transform.parent = ActiveKerbal.KerbalRef.eyeTransform;
				SetCameraIVA(ActiveKerbal.KerbalRef);
			}

			KerbalIva.gameObject.SetActive(false);
			HideCurrentKerbal(false);
			DisablePartHighlighting(false);
			FreeIvaInternalCameraSwitch.SetCameraSwitchesEnabled(true);
			InputLockManager.RemoveControlLock("FreeIVA");
			//ActiveKerbal.flightLog.AddEntry("Buckled");
			ScreenMessages.PostScreenMessage(str_Buckled, 1f, ScreenMessageStyle.LOWER_CENTER);

			PlaySeatBuckleAudio(TargetedSeat);
		}

		public void ReturnToSeat()
		{
			ReturnToSeatInternal(true);
		}

		public void Buckle()
		{
			BuckleInternal(true);
		}

		private void ReturnToSeatInternal(bool resetCamera)
		{
			if (buckled) return;

			// some of this stuff should probably get moved to a common function
			KerbalIva.gameObject.SetActive(false);
			buckled = true;
			DisablePartHighlighting(false);
			InputLockManager.RemoveControlLock("FreeIVA");

			// the original seat should still be pointing at this kerbal.  If not, they probably went EVA or something
			if (OriginalSeat.crew == null)
			{
				return;
			}

			TargetedSeat = OriginalSeat;
			BuckleInternal(resetCamera);
			ScreenMessages.PostScreenMessage(Localizer.Format("#FreeIVA_Message_KermanReturnedSeat", ActiveKerbal.name), 1f, ScreenMessageStyle.LOWER_CENTER);
		}

		public void SwitchToKerbal()
		{
			if (TargetedSeat == null || TargetedSeat.crew == ActiveKerbal)
			{
				return;
			}

			var targetKerbal = TargetedSeat.kerbalRef;
			ReturnToSeatInternal(false);

			SetCameraIVA(targetKerbal);
		}

		internal static void SetCameraIVA(Kerbal kerbal)
		{
			// SetCameraIVA will actually change to flight mode if called with the current kerbal and already in iva mode, so make sure that doesn't happen
			if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA ||
				CameraManager.Instance.ivaCameraActiveKerbal != kerbal)
			{
				CameraManager.Instance.SetCameraIVA(kerbal, true);
			}

			GameEvents.OnIVACameraKerbalChange.Fire(kerbal);

			kerbal.IVAEnable(true);

			FreeIva.EnableInternals(); // SetCameraIVA also calls FlightGlobals.ActiveVessel.SetActiveInternalSpace(activeInternalPart); which will hide all other IVAs
		}

		public static Part GetPartContainingCrew(ProtoCrewMember crewMember)
		{
			// Kerbalref.InPart is always the part where the kerbal was last *seated*
			// most of the time, this is also the part that contains their protocrewmember.
			// but sometimes when entering a seat we don't actually transfer the kerbal between parts - specifically when the seat is in a part that doesn't have enough crew capacity e.g. science lab or inline docking port
			Part containingPart = crewMember.KerbalRef.InPart;

			if (containingPart.protoModuleCrew.Contains(crewMember))
			{
				return containingPart;
			}
			else
			{
				foreach (var part in containingPart.vessel.parts)
				{
					if (part.protoModuleCrew.Contains(crewMember))
					{
						return part;
					}
				}
			}

			return null;
		}

		public void MoveKerbalToSeat(ProtoCrewMember crewMember, InternalSeat newSeat)
		{
			var oldSeat = crewMember.seat;
			var sourceModel = oldSeat.internalModel; // note: this might not be the part where the protocrewmember actually exists.
			var destModel = newSeat.internalModel;

			// remove the kerbal from their old seat in a non-destructive way
			oldSeat.kerbalRef = null;
			sourceModel.UnseatKerbalAt(oldSeat); // does not mess with protocrewmember assignments

			// transferring seats in the same part
			if (sourceModel.part == destModel.part)
			{
				destModel.SitKerbalAt(crewMember, newSeat);
			}
			else if (destModel.part.protoModuleCrew.Count < destModel.part.CrewCapacity)
			{
				Part oldPart = GetPartContainingCrew(crewMember);
				if (oldPart != null)
				{
					// fully move the kerbal
					oldPart.RemoveCrewmember(crewMember);
					
					destModel.part.AddCrewmemberAt(crewMember, destModel.seats.IndexOf(newSeat));

					// suppress the portrait system's response to this message because it messes with internal model visibility
					bool removePortraitEventHandler = !KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.portraitContainer.isActiveAndEnabled;
					if (removePortraitEventHandler)
					{
						GameEvents.onCrewTransferred.Remove(KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.onCrewTransferred);
					}
					GameEvents.onCrewTransferred.Fire(new GameEvents.HostedFromToAction<ProtoCrewMember, Part>(crewMember, sourceModel.part, destModel.part));
					if (removePortraitEventHandler)
					{
						GameEvents.onCrewTransferred.Add(KSP.UI.Screens.Flight.KerbalPortraitGallery.Instance.onCrewTransferred);
					}
					Vessel.CrewWasModified(sourceModel.part.vessel, destModel.part.vessel);
				}
				else
				{
					Debug.LogWarning($"[FreeIva] Kerbal {crewMember.name} did not exist in crew of source part {crewMember.KerbalRef.InPart.partInfo.name} nor anywhere on the vessel");
					destModel.SitKerbalAt(crewMember, newSeat);
				}
			}
			else
			{
				// put the kerbal in the seat without adjusting crew assignments
				// this would normally be done inside AddCrewMemberAt
				// NOTE: SitKerbalAt alters the ProtoCrewMember's seatIdx member, which is stored in the save file and used when spawning kerbals for the IVA.
				// https://github.com/pizzaoverhead/FreeIva/issues/380#issuecomment-1952986010
				int oldSeatIndex = crewMember.seatIdx;
				destModel.SitKerbalAt(crewMember, newSeat);
				crewMember.seatIdx = oldSeatIndex;
			}

			// this is basically InternalSeat.SpawnCrew but without creating the new kerbal (because we already have one)
			{
				var kerbal = crewMember.KerbalRef;
				// Kerbal kerbal = ProtoCrewMember.Spawn(crew);
				kerbal.transform.parent = newSeat.seatTransform;
				kerbal.transform.localPosition = newSeat.kerbalOffset;
				kerbal.transform.localScale = Vector3.Scale(kerbal.transform.localScale, newSeat.kerbalScale);
				kerbal.transform.localRotation = Quaternion.identity;
				kerbal.InPart = destModel.part;
				kerbal.ShowHelmet(newSeat.allowCrewHelmet);
				newSeat.kerbalRef = kerbal;
			}
		}

		public void Unbuckle(bool feedbackEnabled = true)
		{
			if (!buckled) return;

			UpdateActiveKerbal();

			var freeIvaModule = FreeIva.CurrentPart.GetModule<ModuleFreeIva>();
			if ((freeIvaModule && !freeIvaModule.allowsUnbuckling) || FreeIva.CurrentInternalModuleFreeIva == null)
			{
				if (feedbackEnabled)
				{
					ScreenMessages.PostScreenMessage(str_PartCannotUnbuckle, 1f, ScreenMessageStyle.LOWER_CENTER);
				}
				return;
			}

			FreeIva.EnableInternals();
			OriginalSeat = ActiveKerbal.seat;

			Gravity = Gravity && Settings.EnableCollisions;

			HideCurrentKerbal(true);

			InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.CAMERAMODES, "FreeIVA");
			//ActiveKerbal.flightLog.AddEntry("Unbuckled");
			KerbalIva.Activate(ActiveKerbal);
			buckled = false;

			// eventually might want to attach this kerbal to the rigid body (or combine their gameobjects etc) but for now this is important to signal to ProbeControlRoom that the kerbal is unbuckled
			ActiveKerbal.KerbalRef.transform.SetParent(null, true);

			if (feedbackEnabled)
			{
				PlaySeatBuckleAudio(OriginalSeat);
				ScreenMessages.PostScreenMessage(str_Unbuckled, 1f, ScreenMessageStyle.LOWER_CENTER);
			}

			DisablePartHighlighting(true);
			FreeIvaInternalCameraSwitch.SetCameraSwitchesEnabled(false);
		}

		private void UpdateActiveKerbal()
		{
			if (ActiveKerbal.KerbalRef != null && ActiveKerbal.KerbalRef.InPart != null)
			{
				FreeIva.SetCurrentPart(ActiveKerbal.seat.internalModel);
			}
		}

		/// <summary>
		/// Hides or unhides the kerbal IVA model of the currently controlled crew member.
		/// </summary>
		/// <param name="hidden"></param>
		public void HideCurrentKerbal(bool hidden)
		{
			try
			{
				// Set the visible/invisible state for each component of the kerbal model.
				// This won't unhide a helmet hidden elsewhere.
				if (ActiveKerbal != null)
				{
					Renderer[] renderers = ActiveKerbal.KerbalRef.GetComponentsInChildren<Renderer>();
					foreach (var r in renderers)
					{
						r.enabled = !hidden;
					}
				}

				if (!hidden)
				{
					// hides the head renderers
					ActiveKerbal.KerbalRef.IVAEnable();
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("[FreeIVA] Error hiding current kerbal: " + ex.Message + ", " + ex.StackTrace);
			}
		}

		public void DisablePartHighlighting(bool isDisabled)
		{
			int partCount = FlightGlobals.ActiveVessel.parts.Count;
			for (int i = 0; i < partCount; i++)
			{
				if (isDisabled)
					FlightGlobals.ActiveVessel.parts[i].highlightType = Part.HighlightType.Disabled;
				else
				{
					FlightGlobals.ActiveVessel.parts[i].highlightType = Part.HighlightType.OnMouseOver;
				}
			}
		}

		bool ClearLineOfSight(Vector3 from, Transform target, Vector3 targetPosition)
		{
			// first test a direct ray
			if (Physics.Linecast(from, targetPosition, out RaycastHit raycastHit, 1 << (int)Layers.Kerbals, QueryTriggerInteraction.Ignore))
			{
				// if we hit the thing we're trying to reach, success
				if (raycastHit.transform.IsChildOf(target))
				{
					return true;
				}
				// if we hit something other than the source object, it's obstructed
				else if (!raycastHit.transform.IsChildOf(KerbalIva.transform))
				{
					return false;
				}

				// the ray hit ourselves - test a new ray from the hit point
				Vector3 toHit = Vector3.Normalize(raycastHit.point - from) * 0.01f;
				if (Physics.Linecast(raycastHit.point + toHit, targetPosition, out raycastHit, 1 << (int)Layers.Kerbals, QueryTriggerInteraction.Ignore))
				{
					if (raycastHit.transform.IsChildOf(target))
					{
						return true;
					}
					else
					{
						return false;
					}
				}
			}

			return true;
		}

		public bool IsTargeted(Transform targetTransform, Vector3 targetPosition, ref float closestDistance)
		{
			Vector3 toTarget = targetPosition - InternalCamera.Instance.transform.position;
			float distance = toTarget.magnitude;
			float angle = Vector3.Angle(toTarget, InternalCamera.Instance.transform.forward);

			if (angle > 90 || distance > closestDistance)
				return false;

			float radius = (distance * Mathf.Sin(angle * Mathf.Deg2Rad)) / Mathf.Sin((90 - angle) * Mathf.Deg2Rad);
			if (radius <= Settings.ObjectInteractionRadius)
			{
				if (ClearLineOfSight(InternalCamera.Instance.transform.position, targetTransform, targetPosition))
				{
					closestDistance = distance;
					return true;
				}
				else
				{
					return false;
				}
			}

			return false;
		}

		public void TargetSeats()
		{
			float closestDistance = Settings.MaxInteractDistance;
			TargetedSeat = null;
			if (FreeIva.CurrentPart?.internalModel == null)
				return;

			foreach (var seat in FreeIva.CurrentPart.internalModel.seats)
			{
				if (seat.seatTransform == null)
					continue; // Some parts were originally designed to have more seats, but later had their transforms removed without changing the seat count.

				// target a position halfway between the origin of the seat and the kerbal eye position
				// the seat origin is often close to or underneath the shell collider, and the eye position is well above the "center" of the seat
				Vector3 localTargetPosition = KerbalIva.ActiveKerbal.KerbalRef.eyeInitialPos * 0.5f;
				Vector3 targetPosition = seat.seatTransform.position + seat.seatTransform.TransformDirection(localTargetPosition); // can't use TransformPoint because the seat might have scale on it

				if (IsTargeted(seat.seatTransform, targetPosition, ref closestDistance))
				{
					TargetedSeat = seat;
				}
			}

			if (TargetedSeat != null)
			{
				// is someone else here?
				if (TargetedSeat.taken && TargetedSeat.crew != ActiveKerbal)
				{
					ScreenMessages.PostScreenMessage(Localizer.Format("#FreeIVA_Message_CamerSwitchtoKerbal", GameSettings.CAMERA_NEXT.primary, TargetedSeat.crew.name), 0.1f, ScreenMessageStyle.LOWER_CENTER);
				}
				else
				{
					ScreenMessages.PostScreenMessage(str_EnterSeat + " [" + Settings.UnbuckleKey + "]", 0.1f, ScreenMessageStyle.LOWER_CENTER);
				}
			}
		}

		void ConsiderHatch(ref FreeIvaHatch targetedHatch, ref float closestDistance, FreeIvaHatch newHatch)
		{
			if (!newHatch.enabled) return;

			if (newHatch.HandleTransforms == null || newHatch.HandleTransforms.Length == 0)
			{
				if (IsTargeted(newHatch.transform, newHatch.transform.position, ref closestDistance))
				{
					targetedHatch = newHatch;
				}
			}
			else
			{
				// should each hatch specify its own offset point? This code is mostly for the benefit of the BDB LM, where the hatch origins are at the center of the IVA
				foreach (var handleTransform in newHatch.HandleTransforms)
				{
					if (IsTargeted(newHatch.transform, handleTransform.position, ref closestDistance))
					{
						targetedHatch = newHatch;
					}
				}
			}
		}

		void TargetHatchesInPart(Part part, ref FreeIvaHatch targetedHatch, ref float closestDistance)
		{
			InternalModuleFreeIva ivaModule = InternalModuleFreeIva.GetForModel(part.internalModel);
			while (ivaModule != null && ivaModule.isActiveAndEnabled)
			{
				foreach (FreeIvaHatch h in ivaModule.Hatches)
				{
					ConsiderHatch(ref targetedHatch, ref closestDistance, h);
				}

				ivaModule = InternalModuleFreeIva.GetForModel(ivaModule.SecondaryInternalModel);
			}
		}

		public void TargetHatches(bool openHatch, bool openFarHatch)
		{
			if (FreeIva.CurrentPart == null) return;

			FreeIvaHatch targetedHatch = null;
			float closestDistance = Settings.MaxInteractDistance;

			for (var internalModule = InternalModuleFreeIva.GetForModel(FreeIva.CurrentPart.internalModel); internalModule != null; internalModule = InternalModuleFreeIva.GetForModel(internalModule.SecondaryInternalModel))
			{
				if (internalModule.isActiveAndEnabled)
				{
					foreach (FreeIvaHatch h in internalModule.Hatches)
					{
						ConsiderHatch(ref targetedHatch, ref closestDistance, h);

						// if this hatch is open, consider other hatches in the adjacent part
						if (h.IsOpen && h.ConnectedHatch != null)
						{
							TargetHatchesInPart(h.ConnectedHatch.part, ref targetedHatch, ref closestDistance);
						}
					}
				}
			}

			// try neighboring parts
			if (targetedHatch == null)
			{
				if (FreeIva.CurrentPart.parent != null)
				{
					TargetHatchesInPart(FreeIva.CurrentPart.parent, ref targetedHatch, ref closestDistance);
				}

				foreach (Part child in FreeIva.CurrentPart.children)
				{
					TargetHatchesInPart(child, ref targetedHatch, ref closestDistance);
				}
			}

			if (targetedHatch != null)
			{
				var interaction = targetedHatch.GetInteraction();

				PostHatchInteractionMessage(interaction);

				if (FreeIvaHatch.InteractionAllowed(interaction) && openHatch)
				{
					targetedHatch.SetDesiredOpen(!targetedHatch.DesiredOpen);
				}
			}
		}

		public static void PostHatchInteractionMessage(FreeIvaHatch.Interaction interaction)
		{
			ScreenMessages.PostScreenMessage(GetInteractionString(interaction), 0.1f, ScreenMessageStyle.LOWER_CENTER);
		}

		private static string GetInteractionString(FreeIvaHatch.Interaction interaction)
		{
			switch (interaction)
			{
			case FreeIvaHatch.Interaction.Locked: return str_HatchLocked;
			case FreeIvaHatch.Interaction.EVA: return str_GoEVA + " [" + Settings.OpenHatchKey + "]";
			case FreeIvaHatch.Interaction.Blocked: return str_HatchBlocked;
			case FreeIvaHatch.Interaction.Open: return str_OpenHatch + " [" + Settings.OpenHatchKey + "]";
			case FreeIvaHatch.Interaction.Close: return str_CloseHatch + " [" + Settings.OpenHatchKey + "]";
			default: return string.Empty;
			}
		}

		public void OnCollisionEnter(Collision collision)
		{
			//Debug.Log("# OnCollisionEnter " + name + " with " + collision.gameObject + " layer " + collision.gameObject.layer);
			ScreenMessages.PostScreenMessage("OnCollisionEnter " + name + " with " + collision.gameObject + " layer " + collision.gameObject.layer,
				1f, ScreenMessageStyle.LOWER_CENTER);
		}

		public void OnCollisionStay(Collision collision)
		{
			//Debug.Log("# OnCollisionStay " + collision.gameObject + " with " + collision.transform);
			ScreenMessages.PostScreenMessage("OnCollisionStay " + collision.gameObject + " with " + collision.transform + " layer " + collision.gameObject.layer,
				1f, ScreenMessageStyle.LOWER_CENTER);
		}

		public void OnCollisionExit(Collision collision)
		{
			//Debug.Log("# OnCollisionExit " + collision.gameObject + " with " + collision.transform);
			ScreenMessages.PostScreenMessage("OnCollisionExit " + collision.gameObject + " with " + collision.transform + " layer " + collision.gameObject.layer,
				1f, ScreenMessageStyle.LOWER_CENTER);
		}
	}
}
