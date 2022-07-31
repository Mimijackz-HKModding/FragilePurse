using GlobalEnums;
using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace FragilePurse
{
    public class FragilePurse : Mod, IMenuMod, ITogglableMod
    {
        internal static FragilePurse Instance;

        GameObject smallGeo, mediumGeo, largeGeo;
        bool immune = false;
        GeoControl[] geos;
        public int damageType = 0;
        public bool geoImmunity = true;
        public int dropPercentage = 100;

        public bool ToggleButtonInsideMenu => true;


        /*public override List<ValueTuple<string, string>> GetPreloadNames()
        {
            return new List<ValueTuple<string, string>>
            {
               new ValueTuple<string, string>("Waterways_07", "Inflater")
            };
        }*/

        //public FragilePurse() : base("FragilePurse")
        //{
        //    Instance = this;
        //}
        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            List<IMenuMod.MenuEntry> menu = new List<IMenuMod.MenuEntry>();
            menu.Add(toggleButtonEntry.GetValueOrDefault());
            menu.Add(new IMenuMod.MenuEntry
            {
                Name = "Damage Type",
                Description = "Geo Damage is death upon hit w/out geo, Normal Damage is the mask system",
                Values = new string[] {
                    "Geo Damage Only",
                    "Geo + Normal Damage",
                    "Normal Damage Only"
                },
                Saver = opt => damageType = opt,
                Loader = () => damageType
            });
            menu.Add(new IMenuMod.MenuEntry
            {
                Name = "Geo Immunity",
                Description = "Determines wether or not you should be able to collect geo during Invincibility Frames",
                Values = new string[] {
                        "Off",
                       "On"
                    },
                Saver = opt => geoImmunity = opt == 1
                    ,
                Loader = () => this.geoImmunity switch
                {
                    false => 0,
                    true => 1
                }
            });
            menu.Add(new IMenuMod.MenuEntry
            {
                Name = "Dropped Percentage",
                Description = "how much of the geo is dropped on hit",
                Values = new string[] {
                    "0%",
                    "20%",
                    "25%",
                    "33%",
                    "50%",
                    "75%",
                    "100%"
                },
                Saver = opt => this.dropPercentage = indexToPercentage(opt),
                Loader = () => this.dropPercentage switch {
                    0 => 0,
                    20 => 1,
                    25 => 2,
                    33 => 3,
                    50 => 4,
                    75 => 5,
                    100 => 6,
                    _ => throw new InvalidOperationException()
                }
            });


            return menu;
        }


        public override string GetVersion() => "0.1";

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {

            Log("Initializing");

            Instance = this;
            /*smallGeo = Resources.Load<GameObject>("Geo Small");
            mediumGeo = Resources.Load<GameObject>("Geo Med");
            largeGeo = Resources.Load<GameObject>("Geo Large");*/
            smallGeo = GameObject.Find("GlobalPool").GetComponent<ObjectPool>().startupPools[5].prefab;
            mediumGeo = GameObject.Find("GlobalPool").GetComponent<ObjectPool>().startupPools[6].prefab;
            largeGeo = GameObject.Find("GlobalPool").GetComponent<ObjectPool>().startupPools[7].prefab;

            ModHooks.AfterTakeDamageHook += spawnGeo;
            ModHooks.HeroUpdateHook += HeroUpdate;
            Log("Initialized");
        }

        private void HeroUpdate()
        {
            if (immune && !HeroController.instance.cState.invulnerable && !HeroController.instance.cState.hazardDeath)
            {
                foreach (GeoControl gc in geos)
                {
                    gc.enabled = true;
                }
                immune = false;
            }
        }
        private int spawnGeo(int hazardType, int damage)
        {
            if (PlayerData.instance.geo <= 0)
            {
                return damageType == 2 ? damage : PlayerData.instance.health;
            }
            int geo = PlayerData.instance.geo * dropPercentage / 100;
            int remainingGeo = PlayerData.instance.geo - geo;
            for (int i = 0; i < Mathf.FloorToInt(geo / 25); i++)
            {
                spawnGeo(largeGeo);
            }
            geo -= Mathf.FloorToInt(geo / 25) * 25;
            for (int i = 0; i < Mathf.FloorToInt(geo / 5); i++)
            {
                spawnGeo(mediumGeo);
            }
            geo -= Mathf.FloorToInt(geo / 5) * 5;
            for (int i = 0; i < geo; i++)
            {
                spawnGeo(smallGeo);
            }
            PlayerData.instance.geo = remainingGeo;
            GameObject.Find("Geo Text").GetComponent<TextMesh>().text = PlayerData.instance.geo.ToString();
            if (geoImmunity)
            {
                geos = GameObject.FindObjectsOfType<GeoControl>();
                foreach (GeoControl gc in geos)
                {
                    gc.enabled = false;
                }
                immune = true;
            }
            return damageType == 0 ? 0 : damage;
        }
        void spawnGeo(GameObject type)
        {
            GameObject geoClone = UObject.Instantiate(type, HeroController.instance.transform.position, Quaternion.identity);
            Vector2 direction = (Vector2)UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(30f, 40f);
            geoClone.GetComponent<Rigidbody2D>().velocity = new Vector2(direction.x, Mathf.Abs(direction.y));
        }

        public void Unload()
        {
            ModHooks.AfterTakeDamageHook -= spawnGeo;
            ModHooks.HeroUpdateHook -= HeroUpdate;
        }
        private int indexToPercentage(int index)
        {
            switch (index)
            {
                case 0:
                    return 0;
                case 1:
                    return 20;
                case 2:
                    return 25;
                case 3:
                    return 33;
                case 4:
                    return 50;
                case 5:
                    return 75;
                case 6:
                    return 100;
                default:
                    return 0;
            }

        }
    }
}