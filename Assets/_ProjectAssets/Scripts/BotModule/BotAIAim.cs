﻿using Anura.ConfigurationModule.Managers;
using Anura.ConfigurationModule.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using UnityEditor;
using UnityEngine;
using static BotAI;

public class BotAIAim
{
    public enum Weapon
    {
        Basic,
        Grenade,
        Split,
        Flare
    }

    [Serializable]
    public struct CustomWeaponData
    {
        public string key;
        public string value;
    }

    [Serializable]
    public struct WeaponData
    {
        public int internalIndex;
        public Weapon weapon;
        public Rigidbody2D rigidbody;
        public CapsuleCollider2D capsule;
        public int weight;
        public List<CustomWeaponData> customData;
    }

    public class CollisionInfo
    {
        public bool hit;
        public Vector3 position;
        public bool directHit;
        public float distanceFromEnemy;
    }

    private BotAI botAI;

    private Config Config => ConfigurationManager.Instance.Config;
    private BotConfiguration BotConfig => BotManager.Instance.configuration;

    public Dictionary<Weapon, WeaponData> WeaponsData { get; private set; } = new Dictionary<Weapon, WeaponData>();

    private const float SIM_DURATION = 5f;
    private const float SIM_INTERVAL = 0.05f;

    private const float SIM_ANGLE_MIN = 0;
    private const float SIM_ANGLE_MAX = 180;
    private const int SIM_ANGLE_ORDER_POW = 3;  // increment angles by 2^this

    private const float SIM_POWER_MIN = 0.1f;
    private const float SIM_POWER_MAX = 1.0f;
    private const float SIM_POWER_INC = 0.05f;

    private const string WEAPONDATA_EXTRA_FLARE_SECONDARY_RADIUS = "secondary_radius";

    private Collider2D[] enemy;

    private readonly List<float> angleOrderCache = new List<float>();
    private readonly Dictionary<float, Vector2> angleDirectionsCache = new Dictionary<float, Vector2>();
    private float flareSecondaryRadius = 0;

    public BotAIAim(BotAI botAI, Collider2D[] enemy)
    {
        this.botAI = botAI;
        this.enemy = enemy;

        WeaponsDataPrecalculations();
        AnglePrecalculations();
    }

    private void WeaponsDataPrecalculations()
    {
        foreach (WeaponData wd in BotManager.Instance.weaponsData)
        {
            if (wd.weight > 0)
                WeaponsData.Add(wd.weapon, wd);
        }

        if (WeaponsData.ContainsKey(Weapon.Flare))
        {
            if (WeaponsData[Weapon.Flare].customData.Select(x => x.key).Contains(WEAPONDATA_EXTRA_FLARE_SECONDARY_RADIUS))
            {
                flareSecondaryRadius = float.Parse(WeaponsData[Weapon.Flare].customData.First(x => x.key == WEAPONDATA_EXTRA_FLARE_SECONDARY_RADIUS).value);
            }
            else
            {
                Debug.Log("BotAIAim: Configuration error in BotManager.Instance.weaponsData, the weapon " + Weapon.Flare +
                    " is missing a custom field named " + WEAPONDATA_EXTRA_FLARE_SECONDARY_RADIUS +
                    ". This field should be filled in with the radius float value of the collider of the airplane projectiles.");
            }
        }        
    }

    private void AnglePrecalculations()
    {
        for (float i = SIM_ANGLE_MIN; i <= SIM_ANGLE_MAX; i++)
        {
            angleDirectionsCache.Add(i, new Vector2(Mathf.Cos(Mathf.Deg2Rad * i), Mathf.Sin(Mathf.Deg2Rad * i)));
        }

        for (int i = SIM_ANGLE_ORDER_POW; i >= 0; i--)
        {
            int inc = (int)Mathf.Pow(2, i);
            for (float j = SIM_ANGLE_MIN; j <= SIM_ANGLE_MAX; j += inc)
            {
                if (!angleOrderCache.Contains(j))
                    angleOrderCache.Add(j);
            }
        }
    }

    public bool Simulating => simulating;
    private bool simulating = false;
    private int simulationsInFrame;
    private float simulationStartTime;
    private List<Location> simulatedLocations;

    private int debugFrameCount;

    public void StartSimulation(List<Location> locations)
    {
        debugFrameCount = 1;
        simulatedLocations = locations;
        simulating = true;
        BotManager.Instance.StartCoroutine(SimulateLocations());
    }

    private IEnumerator SimulateLocations()
    {
        simulationsInFrame = 0;
        simulationStartTime = Time.time;

        for (int i = 0; i < simulatedLocations.Count; i++)
        {
            if (!simulating) break;
            yield return BotManager.Instance.StartCoroutine(SimulateLocation(i));
        }

        simulating = false;
        if (BotConfig.debugBotAI)
        {
            Debug.Log("BotAIAim: Aiming calculations lasted " + (Time.time - simulationStartTime) + " seconds and " + 
                debugFrameCount + " frames.");
        }
    }

    private bool StopLocationSimulation(Dictionary<Weapon, bool> stop)
    {
        if (BotConfig.locationWeaponEvaluationMode == BotConfiguration.LocationWeaponEvaluationMode.Complete)
        {
            if (!stop.Values.Contains(false)) return true;
        }
        else if (BotConfig.locationWeaponEvaluationMode == BotConfiguration.LocationWeaponEvaluationMode.Fast)
        {
            if (stop.Values.Contains(true)) return true;
        }
        return false;
    }

    private Dictionary<Weapon, CollisionInfo> collisions = new Dictionary<Weapon, CollisionInfo>();
    private IEnumerator SimulateLocation(int locationIndex)
    {
        Location location = simulatedLocations[locationIndex];

        Dictionary<Weapon, bool> stop = new Dictionary<Weapon, bool>();
        foreach (var wd in WeaponsData) stop.Add(wd.Key, false);

        for (int i = 0; i < angleOrderCache.Count; i++)
        {
            if (StopLocationSimulation(stop)) break;            

            float angle = angleOrderCache[i];
            Vector2 angleDir = angleDirectionsCache[angle];

            // Don't even calculate if the enemy is in a different direction than the angle
            if (Mathf.Sign(angleDir.x) != Mathf.Sign(location.enemyDirection)) continue;

            for (float power = SIM_POWER_MIN; power <= SIM_POWER_MAX; power += SIM_POWER_INC)
            {
                if (StopLocationSimulation(stop)) break;

                // Check frame limit
                if (simulationsInFrame >= BotConfig.maxSimulationsPerFrame)
                {
                    simulationsInFrame = 0;
                    debugFrameCount++;
                    yield return null;
                }

                // Check time limit
                if (Time.time - simulationStartTime >= BotConfig.maxThinkingTime)
                {
                    simulating = false;
                    if (BotConfig.debugBotAI)
                    {
                        int progress =(int)((float)locationIndex / simulatedLocations.Count * 100);
                        Debug.Log("BotAIAim: Thinking time exceeded. Progress was at " + progress + "%.");
                    }
                    yield break;
                }

                // Simulate
                collisions.Clear();
                foreach (var wd in WeaponsData)
                {
                    if (stop[wd.Key]) continue;

                    CollisionInfo collision = null;
                    if (!location.locationSims.ContainsKey(wd.Key)) 
                        location.locationSims.Add(wd.Key, new LocationSim(location));

                    // Check cache first
                    foreach (var c in collisions)
                    {
                        if (IsWeaponDataEqual(c.Key, wd.Key))
                        {
                            collision = c.Value;
                            break;
                        }
                    }
                    // Not cached
                    if (collision == null)
                    {
                        collision = SimulateArc(angleDir, location.launchPosition, GetBulletForce(power), wd.Value);
                        simulationsInFrame++;                                              
                    }

                    // Weapon-specific calculations

                    if (collision.hit)
                    {
                        float distanceFromEnemy = collision.distanceFromEnemy;

                        if (wd.Key == Weapon.Flare)
                        {
                            if (flareSecondaryRadius > 0)
                            {
                                Vector3 castOrigin = collision.position;
                                castOrigin.y = botAI.MapMaxY;
                                RaycastHit2D hit = Physics2D.CircleCast(
                                    castOrigin, flareSecondaryRadius, Vector3.down, botAI.MapMaxY, TerrainNavigationLibrary.LAYERMASK_HITTABLES);
                                if (hit)
                                {
                                    location.locationSims[wd.Key].directHit = enemy.Contains(hit.collider);
                                    distanceFromEnemy = GetDistanceFromEnemy(hit.point);
                                }
                            }
                        }
                        else
                        {
                            location.locationSims[wd.Key].directHit = collision.directHit;
                        }

                        if (distanceFromEnemy < location.locationSims[wd.Key].distanceToEnemy)
                        {
                            location.locationSims[wd.Key].angle = angle;
                            location.locationSims[wd.Key].power = power;
                            location.locationSims[wd.Key].distanceToEnemy = distanceFromEnemy;
                        }
                    }

                    if (location.locationSims[wd.Key].directHit) stop[wd.Key] = true;
                    location.locationSims[wd.Key].simulated = true;
                    collisions.Add(wd.Key, collision);
                }
            }
        }
    }

    // For all purposes of simulating an arc
    private bool IsWeaponDataEqual(Weapon w1, Weapon w2)
    {
        if (!WeaponsData.ContainsKey(w1)) return false;
        if (!WeaponsData.ContainsKey(w2)) return false;

        return
            WeaponsData[w1].capsule.size == WeaponsData[w2].capsule.size &&
            WeaponsData[w1].capsule.direction == WeaponsData[w2].capsule.direction &&
            WeaponsData[w1].rigidbody.mass == WeaponsData[w2].rigidbody.mass;
    }

    public List<Location> GetSimulationResults()
    {
        return simulatedLocations;
    }

    private float GetBulletForce(float power)
    {
        return Config.GetBulletSpeed(power);
    }

    public CollisionInfo SimulateArc(Vector2 directionVector, Vector2 launchPosition, float force, WeaponData weaponData)
    {
        Vector3 previousPosition = launchPosition;
        float velocity = force / weaponData.rigidbody.mass;

        int maxSteps = (int)(SIM_DURATION / SIM_INTERVAL);
        for (int i = 0; i < maxSteps; i++)
        {
            Vector3 calculatedPosition = launchPosition + directionVector * velocity * i * SIM_INTERVAL;
            calculatedPosition.y += Physics2D.gravity.y / 2 * Mathf.Pow(i * SIM_INTERVAL, 2);

            Vector3 calculatedDirection = i == 0 ? directionVector : calculatedPosition - previousPosition;
            previousPosition = calculatedPosition;

            float angle = Vector3.Angle(Vector3.right, calculatedDirection);

            CollisionInfo collision = CheckCollision(calculatedPosition, angle, weaponData.capsule.size, weaponData.capsule.direction);
            if (collision.hit) return collision;
        }

        return new CollisionInfo() { hit = false };
    }

    private CollisionInfo CheckCollision(Vector3 position, float angle, Vector2 capsuleSize, CapsuleDirection2D capsuleDirection)
    {
        CollisionInfo result = new CollisionInfo
        {
            position = position,
            hit = false,
            directHit = false,
            distanceFromEnemy = GetDistanceFromEnemy(position)
        };

        Collider2D[] hits = Physics2D.OverlapCapsuleAll(
            position,
            capsuleSize,
            capsuleDirection, 
            angle, 
            TerrainNavigationLibrary.LAYERMASK_HITTABLES);
        if (hits == null || hits.Length == 0) return result;

        result.hit = true;

        foreach (Collider2D e in enemy)
        {
            if (hits.Contains(e))
            {
                result.directHit = true;
                return result;
            }
        }

        return result;
    }

    private float GetDistanceFromEnemy(Vector3 position)
    {
        return enemy.Select(e => Vector3.Distance(position, e.transform.position)).OrderBy(x => x).First();
    }
}
