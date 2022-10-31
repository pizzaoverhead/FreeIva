﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// Base class for hatches (points where kerbals can exit or enter a part during IVA).
    /// Manages attachment node, audio, and connections to other hatches
    /// </summary>
    public abstract class Hatch : InternalModule
    {
        // ----- fields set in prop config

        [KSPField]
        public string hatchOpenSoundFile = "FreeIva/Sounds/HatchOpen";

        [KSPField]
        public string hatchCloseSoundFile = "FreeIva/Sounds/HatchClose";

        [KSPField]
        public string handleTransformName = string.Empty;

        // ----- the following fields are set via PropHatchConfig, so that they can be different per placement of the prop

        // The name of the part attach node this hatch is positioned on, as defined in the part.cfg's "node definitions".
        // e.g. node_stack_top
        public string attachNodeId;
        
        [SerializeReference]
        public ObjectsToHide HideWhenOpen;

        public string airlockName = string.Empty;

        // -----

        [Serializable]
        public struct ObjectToHide
        {
            public string name;
            public Vector3 position;
        }

        // this bullshit brought to you by the Unity serialization system
        public class ObjectsToHide : ScriptableObject
        {
            public List<ObjectToHide> objects = new List<ObjectToHide>();
        }

        // Where the GameObject is located. Used for basic interaction targeting (i.e. when to show the "Open hatch?" prompt).
        public virtual Vector3 WorldPosition => transform.position;

        private Hatch _connectedHatch = null;
        // The other hatch that this one is connected or docked to, if present.
        public Hatch ConnectedHatch
        {
            get
            {
                if (_connectedHatch == null)
                    GetConnectedHatch();
                return _connectedHatch;
            }
        }

        private AttachNode _hatchNode;
        // The part attach node this hatch is positioned on.
        public AttachNode HatchNode
        {
            get
            {
                if (_hatchNode == null)
                    _hatchNode = GetHatchNode(attachNodeId);
                return _hatchNode;
            }
        }

        public FXGroup HatchOpenSound = null;
        public FXGroup HatchCloseSound = null;

        public bool IsOpen { get; private set; }

        public void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            Debug.Log($"# Creating hatch {internalProp.propName} for part {part.partName}");
            
            HatchOpenSound = SetupAudio(hatchOpenSoundFile);
            HatchCloseSound = SetupAudio(hatchCloseSoundFile);

            if (handleTransformName != string.Empty)
            {
                var handleTransform = internalProp.FindModelTransform(handleTransformName);
                if (handleTransform != null)
                {
                    var clickWatcher = handleTransform.gameObject.AddComponent<ClickWatcher>();
                    clickWatcher.AddMouseDownAction(OnHandleClick);
                }
            }

            InternalModuleFreeIva.GetForModel(internalModel).Hatches.Add(this);
        }

        void OnDestroy()
        {
            InternalModuleFreeIva.GetForModel(internalModel).Hatches.Remove(this);
        }

        private void OnHandleClick()
        {
            ToggleHatch();
        }

        private void GetConnectedHatch()
        {
            AttachNode hatchNode = GetHatchNode(attachNodeId);
            if (hatchNode == null) return;

            InternalModuleFreeIva iva = InternalModuleFreeIva.GetForModel(hatchNode.attachedPart?.internalModel);
            if (iva == null) return;
            for (int i = 0; i < iva.Hatches.Count; i++)
            {
                AttachNode otherHatchNode = iva.Hatches[i].HatchNode;
                if (otherHatchNode != null && otherHatchNode.attachedPart != null && otherHatchNode.attachedPart.Equals(part))
                {
                    _connectedHatch = iva.Hatches[i];
                    break;
                }
            }
        }

        /// <summary>
        /// Find the part attach node this hatch is associated with.
        /// </summary>
        /// <param name="attachNodeId"></param>
        /// <returns></returns>
        private AttachNode GetHatchNode(string attachNodeId)
        {
            if (string.IsNullOrEmpty(attachNodeId)) return null;
            string nodeName = RemoveNodePrefix(attachNodeId);
            foreach (AttachNode n in part.attachNodes)
            {
                if (n.id == nodeName)
                    return n;
            }
            return null;
        }

        private static string RemoveNodePrefix(string attachNodeId)
        {
            string nodeName;
            string prefix = @"node_stack_";
            if (attachNodeId.StartsWith(prefix))
            {
                nodeName = attachNodeId.Substring(prefix.Length, attachNodeId.Length - prefix.Length);
            }
            else
                nodeName = attachNodeId;
            return nodeName;
        }

        public FXGroup SetupAudio(string soundFile)
        {
            FXGroup result = null;
            if (!string.IsNullOrEmpty(soundFile))
            {
                result = new FXGroup("HatchOpen");
                result.audio = gameObject.AddComponent<AudioSource>(); // TODO: if we deactivate this object when the hatch opens, do we need to put the sound on a different object?
                result.audio.dopplerLevel = 0f;
                result.audio.Stop();
                result.audio.clip = GameDatabase.Instance.GetAudioClip(hatchOpenSoundFile);
                result.audio.loop = false;
            }

            return result;
        }

        public void ToggleHatch()
        {
            Open(!IsOpen);
        }

        public virtual void Open(bool open)
        {
            var connectedHatch = ConnectedHatch;

            // if we're trying to open a door to space, just go EVA
            if (connectedHatch == null && open)
            {
                GoEVA();
            }
            else
            {
                HideOnOpen(open);
                FreeIva.SetRenderQueues(FreeIva.CurrentPart);

                if (open != IsOpen)
                {
                    var sound = open ? HatchOpenSound : HatchCloseSound;
                    if (sound != null && sound.audio != null)
                        sound.audio.Play();
                }

                IsOpen = open;

                // automatically toggle the far hatch too
                if (connectedHatch != null && connectedHatch.IsOpen != open)
                {
                    connectedHatch.Open(open);
                }
            }
        }

        public virtual void HideOnOpen(bool open)
        {
            if (HideWhenOpen == null) return;

            MeshRenderer[] meshRenderers = internalModel.GetComponentsInChildren<MeshRenderer>();
            foreach (var hideProp in HideWhenOpen.objects)
            {
                bool found = false;

                foreach (MeshRenderer mr in meshRenderers)
                {
                    if (mr.name.Equals(hideProp.name) && mr.transform != null)
                    {
                        float error = Vector3.Distance(mr.transform.localPosition, hideProp.position);
                        if (error < 0.15)
                        {
                            Debug.Log("# Toggling " + mr.name);
                            mr.enabled = !open;
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    Debug.LogError($"[FreeIva] HideWhenOpen - could not find meshrenderer named {hideProp.name} in model {internalModel.internalName}");
                }
            }
        }

        static Transform FindAirlock(Part part, string airlockName)
        {
            if (!string.IsNullOrEmpty(airlockName))
            {
                var childTransform = part.FindModelTransform(airlockName);

                if (childTransform.CompareTag("Airlock"))
                {
                    return childTransform;
                }
            }

            return part.airlock;
        }

        bool GoEVA()
        {
            float acLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex);
            bool evaUnlocked = GameVariables.Instance.UnlockedEVA(acLevel);
            bool evaPossible = GameVariables.Instance.EVAIsPossible(evaUnlocked, vessel);

            Kerbal kerbal = CameraManager.Instance.IVACameraActiveKerbal;

            if (kerbal != null && evaPossible && HighLogic.CurrentGame.Parameters.Flight.CanEVA)
            {
                var kerbalEVA = FlightEVA.fetch.spawnEVA(kerbal.protoCrewMember, kerbal.InPart, FindAirlock(kerbal.InPart, airlockName), true);

                if (kerbalEVA != null)
                {
                    CameraManager.Instance.SetCameraFlight();
                    return true;
                }
            }

            return false;
        }

        public static void InitialiseAllHatchesClosed()
        {
            foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            {
                InternalModuleFreeIva mfi = InternalModuleFreeIva.GetForModel(p.internalModel);
                if (mfi != null)
                {
                    foreach (var hatch in mfi.Hatches)
                    {
                        hatch.Open(false);
                    }
                }
            }
        }

        /*public void Destroy()
        {
            cylinder.DestroyGameObject();
        }*/

        /*public static void PositionHole()
        {
            cylinder.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            cylinder.transform.localScale = Scale;
            return;
            /*Debug.Log("Positioning cylinder from " + cylinder.transform.localPosition);
            cylinder.transform.localPosition = new Vector3(hatchX, hatchY, hatchZ);
            Debug.Log("                       to " + cylinder.transform.localPosition);
            cylinder.transform.localScale = new Vector3(hatchScaleX, hatchScaleY, hatchScaleZ);* /
        }*/
    }
}
