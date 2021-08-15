using BepInEx;
using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using GorillaLocomotion;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.Audio;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using System.Collections;
using Photon.Pun;
using System.IO;

namespace Rewind
{

    [BepInPlugin("org.BananaInc.gorilla.BananaCar", "Banana Car", "1.0.0.0")]
    public class HarmonyStuff : BaseUnityPlugin
    {
        public void Awake()
        {
            var harmony = new Harmony("com.BananaInc.gorilla.BananaCar");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(GorillaLocomotion.Player))]
    [HarmonyPatch("Update")]
    public class Cars : BaseUnityPlugin
    {
        private static int layers = (1 << 9);
        private static Vector3 head_direction;
        private static Vector3 roll_direction;
        private static Vector2 left_joystick;

        private static ConfigEntry<float> Acceleration_con;
        private static ConfigEntry<float> Max_con;
        private static ConfigEntry<float> multi;

        private static float acceleration = 5f;
        private static float maxs = 10f;
        private static float distance = 0.35f;
        private static float multiplier = 1f;

        private static float speed = 0f;

        private static bool Start = false;
        private static void Postfix(GorillaLocomotion.Player __instance)
        {
            if (!Start)
            {
                var file = new ConfigFile(Path.Combine(Paths.ConfigPath, "BananaCars.cfg"), true);
                Acceleration_con = file.Bind("Banana-Car Settings", "Acceleration (m/s/s)", 5f, "The speed added per second while driving");
                acceleration = Acceleration_con.Value;
                Max_con = file.Bind("Banana-Car Settings", "Max-Speed", 7.5f, "The max amount of speed you can get while driving");
                maxs = Max_con.Value;
                multi = file.Bind("Banana-Car Settings", "Multiplier", 1f, "The speed multiplier (not necessary to have here but might as well add it)");
                multiplier = multi.Value;

                Start = true;
            }

            bool Enabled = PhotonNetwork.CurrentRoom == null ? false : !PhotonNetwork.CurrentRoom.IsVisible;

            if (Enabled)
            {
                List<InputDevice> list = new List<InputDevice>();
                InputDevices.GetDevices(list);

                for (int i = 0; i < list.Count; i++) //Get input
                {
                    if (list[i].characteristics.HasFlag(InputDeviceCharacteristics.Left))
                    {
                        list[i].TryGetFeatureValue(CommonUsages.grip, out left_joystick.y);
                    }
                    if (list[i].characteristics.HasFlag(InputDeviceCharacteristics.Right))
                    {
                    }
                }

                RaycastHit ray;

                var down = Physics.Raycast(__instance.bodyCollider.transform.position, Vector3.down, out ray, 100f, layers);

                head_direction = __instance.headCollider.transform.forward;
                roll_direction = Vector3.ProjectOnPlane(head_direction, ray.normal);

                if (left_joystick.y != 0)
                {
                    if (left_joystick.y < 0)
                    {
                        if (speed > -maxs)
                        {
                            speed -= acceleration * Math.Abs(left_joystick.y) * Time.deltaTime;
                        }
                    }
                    else
                    {
                        if (speed < maxs)
                        {
                            speed += acceleration * Math.Abs(left_joystick.y) * Time.deltaTime;
                        }
                    }
                }
                else
                {
                    if (speed < 0)
                    {
                        speed += acceleration * Time.deltaTime * 0.5f;
                    }
                    else if (speed > 0)
                    {
                        speed -= acceleration * Time.deltaTime * 0.5f;
                    }
                }

                if (speed > maxs)
                {
                    speed = maxs;
                }
                if (speed < -maxs)
                {
                    speed = -maxs;
                }

                if (speed != 0 && ray.distance < distance)
                {
                    __instance.bodyCollider.attachedRigidbody.velocity = roll_direction.normalized * speed * multiplier;
                }

                if (__instance.IsHandTouching(true) || __instance.IsHandTouching(false))
                {
                    speed *= 0.75f;
                }
            }
            else
            {
            }
        }
    }
}
