﻿using RoR2;
using UnityEngine;
using R2API;
using HANDMod.Content.HANDSurvivor.Components.Body;
using UnityEngine.Networking;

namespace HANDMod.Content
{
    public static class DamageTypes
    {
        public static DamageAPI.ModdedDamageType ResetVictimForce;
        public static DamageAPI.ModdedDamageType HANDPrimaryPunch;
        public static DamageAPI.ModdedDamageType HANDSecondary;
        public static DamageAPI.ModdedDamageType HANDSecondaryScepter;
        public static DamageAPI.ModdedDamageType SquashOnKill;
        public static DamageAPI.ModdedDamageType ScaleForceToMass;

        private static bool initialized = false;

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            DamageTypes.ResetVictimForce = DamageAPI.ReserveDamageType();
            DamageTypes.HANDPrimaryPunch = DamageAPI.ReserveDamageType();
            DamageTypes.HANDSecondary = DamageAPI.ReserveDamageType();
            DamageTypes.HANDSecondaryScepter = DamageAPI.ReserveDamageType();
            DamageTypes.SquashOnKill = DamageAPI.ReserveDamageType();

            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;

        }

        private static void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (NetworkServer.active)
            {
                CharacterBody cb = self.body;

                if (damageInfo.attacker)
                {
                    if (damageInfo.HasModdedDamageType(DamageTypes.SquashOnKill))
                    {
                        HANDNetworkComponent hnc = damageInfo.attacker.GetComponent<HANDNetworkComponent>();
                        if (hnc)
                        {
                            if (cb.master)
                            {
                                NetworkIdentity ni = cb.master.GetComponent<NetworkIdentity>();
                                if (ni)
                                {
                                    hnc.SquashEnemy(ni.netId.Value);
                                }
                            }
                        }
                    }
                }

                //This will only work on things that are run on the server.
                if (damageInfo.HasModdedDamageType(DamageTypes.ResetVictimForce))
                {
                    if (cb.rigidbody)
                    {
                        cb.rigidbody.velocity = new Vector3(0f, cb.rigidbody.velocity.y, 0f);
                        cb.rigidbody.angularVelocity = new Vector3(0f, cb.rigidbody.angularVelocity.y, 0f);
                    }
                    if (cb.characterMotor != null)
                    {
                        cb.characterMotor.velocity.x = 0f;
                        cb.characterMotor.velocity.z = 0f;
                        cb.characterMotor.rootMotion.x = 0f;
                        cb.characterMotor.rootMotion.z = 0f;
                    }
                }

                if (damageInfo.HasModdedDamageType(DamageTypes.HANDPrimaryPunch))
                {
                    if (cb.isFlying)
                    {
                        damageInfo.force.x *= 0.5f;
                        damageInfo.force.z *= 0.5f;
                    }
                    else if (cb.characterMotor != null)
                    {
                        if (!cb.characterMotor.isGrounded)    //Multiply launched enemy force
                        {
                            damageInfo.force.x *= 1.8f;
                            damageInfo.force.z *= 1.8f;

                            if (cb.isChampion)
                            {
                                damageInfo.force.x *= 0.8f;
                                damageInfo.force.z *= 0.8f;
                            }
                        }
                        else
                        {
                            if (cb.isChampion) //deal less knockback against bosses if they're on the ground
                            {
                                damageInfo.force.x *= 0.5f;
                                damageInfo.force.z *= 0.5f;
                            }
                        }
                    }

                    if (cb.rigidbody)
                    {
                        damageInfo.force *= Mathf.Max(cb.rigidbody.mass / 100f, 1f);
                    }
                }

                //Jank.
                bool isSecondary = damageInfo.HasModdedDamageType(DamageTypes.HANDSecondary);
                bool isScepter = damageInfo.HasModdedDamageType(DamageTypes.HANDSecondaryScepter);
                if (isSecondary || isScepter)
                {
                    bool launchEnemy = false;
                    //Downwards force is determined when setting up the attack.
                    //Force gets overwritten into upwards force if target is grounded.
                    if (cb.characterMotor && cb.characterMotor.isGrounded)
                    {
                        launchEnemy = true;
                        damageInfo.force.y = 2000f;
                    }

                    if (cb.rigidbody)
                    {
                        float forceMult = Mathf.Max(cb.rigidbody.mass / 100f, 1f);
                        if (!launchEnemy && !isScepter)
                        {
                            forceMult = Mathf.Max(7.5f, forceMult);
                        }
                        damageInfo.force *= forceMult;
                    }
                }
            }
            orig(self, damageInfo);
        }
    }
}
