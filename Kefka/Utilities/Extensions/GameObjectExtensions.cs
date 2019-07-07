﻿using System;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using static Kefka.Utilities.Constants;
using Kefka.Models;
using System.Collections.Generic;
using System.Linq;
using Clio.Common;
using Clio.Utilities;
using ff14bot.Helpers;

namespace Kefka.Utilities.Extensions
{
    internal static class GameObjectExtensions
    {
        #region SafeNames

        public static string SafeName(this GameObject obj)
        {
            var useSafeNames = MainSettingsModel.Instance.UseSafeNames;

            if (obj.IsMe)
            {
                return "Me";
            }

            string name;
            var character = obj as BattleCharacter;
            if (character != null)
            {
                name = !PartyMembers.Contains(character) ? "Enemy -> " : "Ally -> ";

                if (name.Contains("Ally"))
                {
                    if (!useSafeNames) name += character.Name;
                    else name += character.CurrentJob.ToString();
                    return name;
                }

                if (obj.IsBoss())
                    name += "Boss: ";

                return name += character.Name;
            }
            name = obj.Name;

            return name;
        }

        #endregion SafeNames

        internal static bool InView(Vector3 playerLocation, float playerHeading, Vector3 targetLocation)
        {
            var d = Math.Abs(MathEx.NormalizeRadian(playerHeading - MathEx.NormalizeRadian(MathHelper.CalculateHeading(playerLocation, targetLocation) + (float)Math.PI)));

            if (d > Math.PI)
            {
                d = Math.Abs(d - 2 * (float)Math.PI);
            }
            return d < 0.78539f;
        }

        internal static int AuraCount(this GameObject unit)
        {
            var aura = (unit as BattleCharacter)?.CharacterAuras?.ToArray();
            var value = aura?.Length ?? 0;
            return value;
        }

        public static bool CanSilence(this GameObject unit)
        {
            var unitAsCharacter = unit as Character;

            if (unitAsCharacter == null)
                return false;

            return unitAsCharacter.IsCasting;
        }

        public static bool CanStun(this GameObject unit)
        {
            var unitAsCharacter = unit as Character;

            if (unitAsCharacter == null)
                return false;

            return unitAsCharacter.IsCasting;
        }

        internal static bool HasAura(this GameObject unit, uint spell, bool isMyAura = true, double msLeft = 0, bool belowZeroValid = true)
        {
            var unitasc = unit as Character;
            if (unit == null || unitasc == null || !unitasc.IsValid)
            {
                return false;
            }

            var auras = isMyAura
                ? unitasc.CharacterAuras.Where(r => r.CasterId == Me.ObjectId && r.Id == spell)
                : unitasc.CharacterAuras.Where(r => r.Id == spell);

            if (!belowZeroValid && auras.Any(aura => aura.TimespanLeft.TotalMilliseconds < 0)) return false;

            return auras.Any(aura => aura.TimespanLeft.TotalMilliseconds >= msLeft);
        }

        internal static bool HealthCheck(this GameObject unit, bool isdot)
        {
            var unitasc = unit as Character;
            if (unit == null || unitasc == null || !unitasc.IsValid)
                return false;

            if (!isdot)
            {
                if (MainSettingsModel.Instance.DestroyTarget || (MainSettingsModel.Instance.TarHpInt == 0 && MainSettingsModel.Instance.TarHpPct == 0)) return true;

                if (!MainSettingsModel.Instance.DynamicTargetHp)
                    return unitasc.IsBoss() || unitasc.CurrentHealth >= MainSettingsModel.Instance.TarHpInt && unitasc.CurrentHealthPercent >= MainSettingsModel.Instance.TarHpPct;

                if (!DutyManager.InInstance)
                    return unitasc.IsBoss() || unitasc.CurrentHealthPercent >= MainSettingsModel.Instance.CdOverworldTier1HpPct && (unitasc.MaxHealth / Me.MaxHealth >= MainSettingsModel.Instance.CdOverworldTier1HpAdvantage);

                if (unitasc.IsBoss() || unitasc.CurrentHealthPercent >= MainSettingsModel.Instance.CdInstanceTier1HpPct && (unitasc.MaxHealth / Me.MaxHealth >= MainSettingsModel.Instance.CdInstanceTier1HpAdvantage))
                    return true;

                return unitasc.IsBoss() || unitasc.CurrentHealthPercent >= MainSettingsModel.Instance.CdInstanceTier2HpPct && (unitasc.MaxHealth / Me.MaxHealth >= MainSettingsModel.Instance.CdInstanceTier2HpAdvantage);
            }

            if (MainSettingsModel.Instance.DestroyTarget || (MainSettingsModel.Instance.TarHpInt == 0 && MainSettingsModel.Instance.TarHpPct == 0)) return true;

            if (!MainSettingsModel.Instance.DynamicTargetHp)
                return unitasc.IsBoss() || unitasc.CurrentHealth >= MainSettingsModel.Instance.TarHpInt && unitasc.CurrentHealthPercent >= MainSettingsModel.Instance.TarHpPct;

            if (!DutyManager.InInstance)
                return unitasc.IsBoss() || unitasc.CurrentHealthPercent >= MainSettingsModel.Instance.DotOverworldTier1HpPct && (unitasc.MaxHealth / Me.MaxHealth >= MainSettingsModel.Instance.DotOverworldTier1HpAdvantage);

            if (unitasc.IsBoss() || unitasc.CurrentHealthPercent >= MainSettingsModel.Instance.DotInstanceTier1HpPct && (unitasc.MaxHealth / Me.MaxHealth >= MainSettingsModel.Instance.DotInstanceTier1HpAdvantage))
                return true;

            return unitasc.IsBoss() || unitasc.CurrentHealthPercent >= MainSettingsModel.Instance.DotInstanceTier2HpPct && (unitasc.MaxHealth / Me.MaxHealth >= MainSettingsModel.Instance.DotInstanceTier2HpAdvantage);
        }

        public static bool TimeToDeathCheck(this GameObject unit)
        {
            if (MainSettingsModel.Instance.DestroyTarget || MainSettingsModel.Instance.TimeToDeathLimit == 0) return true;

            int val;
            if (unit != null)
            {
                val = EnemyOverlayLogic.EnemyOverlayInfos.Any(r => r.Unit == unit) ? EnemyOverlayLogic.EnemyOverlayInfos.First(r => r.Unit == unit).TimeToDeath : 30;
            }
            else
            {
                val = 0;
            }

            return val > MainSettingsModel.Instance.TimeToDeathLimit || unit.IsBoss();
        }

        internal static bool HasAnyAura(this GameObject unit, uint[] auras)
        {
            var unitasc = unit as Character;

            if (unit == null || unitasc == null || !unitasc.IsValid)
            {
                return false;
            }

            return unitasc.CharacterAuras.Any(r => auras.Contains(r.Id));
        }

        public static bool IsTank(this GameObject tar)
        {
            var gameObject = tar as Character;
            return gameObject != null && Tanks.Contains(gameObject.CurrentJob);
        }

        public static bool IsHealer(this GameObject tar)
        {
            var gameObject = tar as Character;
            return gameObject != null && Healers.Contains(gameObject.CurrentJob);
        }

        public static bool IsDps(this GameObject tar)
        {
            var gameObject = tar as Character;
            return gameObject != null && (RangedDps.Contains(gameObject.CurrentJob) || MeleeDps.Contains(gameObject.CurrentJob));
        }

        public static bool IsMeleeDps(this GameObject tar)
        {
            var gameObject = tar as Character;
            return gameObject != null && MeleeDps.Contains(gameObject.CurrentJob);
        }

        public static bool IsRangedDps(this GameObject tar)
        {
            var gameObject = tar as Character;
            return gameObject != null && RangedDps.Contains(gameObject.CurrentJob);
        }

        public static bool IsPhysical(this GameObject tar)
        {
            var gameObject = tar as Character;
            return gameObject != null && Physical.Contains(gameObject.CurrentJob);
        }

        public static bool IsCaster(this GameObject tar)
        {
            var gameObject = tar as Character;
            return gameObject != null && Caster.Contains(gameObject.CurrentJob);
        }

        public static bool IsChocobo(this GameObject tar)
        {
            var gameObject = tar as Character;
            return gameObject != null && Chocobo.Contains(gameObject.CurrentJob);
        }

        public static bool AllyIsValid(this GameObject unit)
        {
            var unitAsBattleCharacter = unit as BattleCharacter;

            if (unitAsBattleCharacter == null || !unitAsBattleCharacter.IsValid)
                return false;

            return !unitAsBattleCharacter.IsChocobo()
                   && unitAsBattleCharacter.Icon != PlayerIcon.Disconnected
                   && unitAsBattleCharacter.Icon != PlayerIcon.Offline
                   && unitAsBattleCharacter.Icon != PlayerIcon.Viewing_Cutscene;
        }

        #region Helpers

        private static readonly List<ClassJobType> Tanks = new List<ClassJobType>
        {
            ClassJobType.Gladiator,
            ClassJobType.Paladin,
            ClassJobType.Marauder,
            ClassJobType.Warrior,
            ClassJobType.DarkKnight
        };

        private static readonly List<ClassJobType> Healers = new List<ClassJobType>
        {
            ClassJobType.Arcanist,
            ClassJobType.Scholar,
            ClassJobType.Conjurer,
            ClassJobType.WhiteMage,
            ClassJobType.Astrologian
        };

        private static readonly List<ClassJobType> RangedDps = new List<ClassJobType>
        {
            ClassJobType.Archer,
            ClassJobType.Bard,
            ClassJobType.Thaumaturge,
            ClassJobType.BlackMage,
            ClassJobType.Machinist,
            ClassJobType.Summoner,
            ClassJobType.RedMage,
        };

        private static readonly List<ClassJobType> MeleeDps = new List<ClassJobType>
        {
            ClassJobType.Lancer,
            ClassJobType.Dragoon,
            ClassJobType.Pugilist,
            ClassJobType.Monk,
            ClassJobType.Rogue,
            ClassJobType.Ninja,
            ClassJobType.Samurai,
        };

        private static readonly List<ClassJobType> Physical = new List<ClassJobType>
        {
            ClassJobType.Gladiator,
            ClassJobType.Paladin,
            ClassJobType.Marauder,
            ClassJobType.Warrior,
            ClassJobType.DarkKnight,
            ClassJobType.Lancer,
            ClassJobType.Dragoon,
            ClassJobType.Pugilist,
            ClassJobType.Monk,
            ClassJobType.Rogue,
            ClassJobType.Ninja,
            ClassJobType.Samurai,
            ClassJobType.Archer,
            ClassJobType.Bard,
            ClassJobType.Machinist,
        };

        private static readonly List<ClassJobType> Caster = new List<ClassJobType>
        {
            ClassJobType.Arcanist,
            ClassJobType.Summoner,
            ClassJobType.Scholar,
            ClassJobType.Conjurer,
            ClassJobType.WhiteMage,
            ClassJobType.Astrologian,
            ClassJobType.Thaumaturge,
            ClassJobType.BlackMage,
            ClassJobType.RedMage,
        };

        private static readonly List<ClassJobType> Chocobo = new List<ClassJobType>
        {
            ClassJobType.Adventurer
        };

        public static IEnumerable<BattleCharacter> PartyMembers
        {
            get
            {
                return
                    PartyManager.VisibleMembers
                        .Select(pm => pm.GameObject as BattleCharacter)
                        .Where(pm => pm.IsTargetable);
            }
        }

        public static IEnumerable<BattleCharacter> HealManager
        {
            get
            {
                return
                    GameObjectManager.GetObjectsOfType<BattleCharacter>(true, true)
                        .Where(hm => hm.IsAlive && (PartyMembers.Contains(hm) || hm == Core.Player) && !hm.IsNpc)
                        .OrderBy(HPScore);
            }
        }

        public static IEnumerable<BattleCharacter> ResManager
        {
            get
            {
                return
                    GameObjectManager.GetObjectsOfType<BattleCharacter>(true, true)
                        .Where(hm => PartyMembers.Contains(hm) && !hm.IsNpc)
                        .OrderBy(HPScore);
            }
        }

        public static bool NeedsCleanse(this Character unit)
        {
            return unit.CharacterAuras.Any(r => r.IsDispellable);
        }

        private static float HPScore(BattleCharacter c)
        {
            var score = 100 - c.CurrentHealthPercent;

            if (c.IsTank())
            {
                score += 5f;
            }
            if (c.IsHealer())
            {
                score += 3f;
            }
            return score;
        }

        #endregion Helpers

        #region IsBoss

        public static bool IsBoss(this GameObject unit)
        {
            if (unit == null || !unit.IsValid)
            {
                return false;
            }

            return BossList.Contains(unit.NpcId);
        }

        private static readonly HashSet<uint> BossList = new HashSet<uint>
        {
            /*Striking Dummy*/ 541, 71, 72, 73, 74, 75, 81, 85, 92, 93, 94, 101, 119, 185, 189, 255, 257, 328, 343, 419, 422, 444, 446, 447, 448, 450, 451, 452, 465, 470, 478, 479, 498, 512, 514, 516, 517, 535, 548, 554, 557, 558, 568, 569, 581, 596, 597, 598, 599, 600, 614, 618, 619, 620, 626, 627, 628, 668, 689, 690, 698, 708, 710, 713, 714, 715, 717, 718, 719, 720, 721, 722, 723, 724, 725, 729, 732, 734, 735, 742, 743, 750, 751, 752, 758, 761, 762, 763, 768, 811, 815, 816, 823, 824, 826, 828, 836, 840, 841, 842, 843, 844, 845, 849, 850, 852, 853, 854, 855, 856, 857, 858, 859, 860, 861, 862, 863, 864, 866, 867, 868, 869, 870, 871, 872, 873, 874, 876, 878, 882, 883, 884, 885, 886, 887, 888, 903, 906, 907, 918, 919, 920, 921, 922, 924, 935, 936, 946, 948, 949, 966, 969, 982, 992, 1008, 1009, 1011, 1012, 1013, 1015, 1053, 1054, 1055, 1057, 1058, 1059, 1060, 1062, 1108, 1109, 1114, 1127, 1133, 1137, 1143, 1152, 1154, 1160, 1165, 1178, 1185, 1194, 1197, 1200, 1201, 1202, 1204, 1206, 1234, 1237, 1238, 1242, 1243, 1245, 1246, 1247, 1248, 1249, 1250, 1251, 1252, 1253, 1254, 1265, 1269, 1279, 1300, 1301, 1314, 1325, 1326, 1329, 1372, 1373, 1378, 1380, 1381, 1382, 1391, 1394, 1395, 1396, 1398, 1399, 1400, 1401, 1402, 1403, 1404, 1415, 1416, 1418, 1419, 1421, 1422, 1423, 1425, 1426, 1427, 1428, 1429, 1430, 1431, 1432, 1433, 1434, 1435, 1436, 1437, 1438, 1439, 1440, 1441, 1442, 1443, 1444, 1445, 1452, 1454, 1455, 1456, 1457, 1458, 1466, 1482, 1483, 1484, 1486, 1488, 1489, 1492, 1493, 1494, 1495, 1496, 1500, 1502, 1503, 1504, 1505, 1506, 1507, 1508, 1510, 1512, 1513, 1514, 1515, 1516, 1517, 1518, 1519, 1520, 1521, 1522, 1523, 1524, 1525, 1526, 1527, 1528, 1530, 1531, 1576, 1580, 1610, 1629, 1632, 1640, 1644, 1645, 1646, 1651, 1661, 1662, 1663, 1664, 1665, 1666, 1667, 1668, 1670, 1677, 1678, 1680, 1696, 1699, 1700, 1701, 1702, 1704, 1728, 1729, 1764, 1776, 1779, 1786, 1792, 1793, 1801, 1802, 1808, 1855, 1858, 1872, 1878, 1881, 1901, 1942, 1961, 1967, 1972, 1973, 1977, 2002, 2006, 2013, 2015, 2017, 2030, 2031, 2059, 2062, 2064, 2067, 2069, 2071, 2073, 2081, 2082, 2083, 2084, 2085, 2095, 2098, 2099, 2100, 2118, 2135, 2136, 2137, 2139, 2140, 2141, 2143, 2146, 2147, 2148, 2160, 2163, 2167, 2168, 2176, 2181, 2183, 2216, 2231, 2259, 2264, 2285, 2286, 2289, 2341, 2346, 2352, 2354, 2355, 2356, 2357, 2358, 2360, 2364, 2365, 2366, 2367, 2505, 2506, 2507, 2512, 2516, 2550, 2551, 2560, 2564, 2597, 2602, 2603, 2604, 2605, 2606, 2607, 2610, 2611, 2612, 2618, 2627, 2628, 2629, 2630, 2631, 2632, 2653, 2665, 2666, 2714, 2775, 2778, 2780, 2791, 2792, 2793, 2794, 2795, 2796, 2800, 2809, 2811, 2815, 2821, 2824, 2826, 2832, 2846, 2852, 2856, 2860, 2861, 2872, 2873, 2878, 2886, 2903, 2917, 2919, 2920, 2921, 2922, 2923, 2924, 2925, 2926, 2927, 2928, 2929, 2930, 2931, 2932, 2933, 2934, 2936, 2937, 2938, 2939, 2940, 2941, 2942, 2943, 2944, 2945, 2946, 2947, 2948, 2949, 2950, 2951, 2952, 2953, 2954, 2956, 2957, 2958, 2959, 2960, 2961, 2962, 2964, 2965, 2966, 2967, 2968, 2969, 2994, 3013, 3014, 3015, 3038, 3044, 3046, 3047, 3091, 3093, 3095, 3112, 3113, 3119, 3120, 3127, 3129, 3133, 3138, 3149, 3159, 3164, 3165, 3169, 3172, 3173, 3174, 3175, 3192, 3197, 3204, 3208, 3210, 3212, 3213, 3214, 3215, 3216, 3217, 3231, 3234, 3247, 3248, 3249, 3250, 3251, 3272, 3280, 3287, 3299, 3300, 3304, 3320, 3330, 3345, 3369, 3374, 3376, 3387, 3406, 3409, 3434, 3452, 3455, 3458, 3459, 3460, 3462, 3463, 3465, 3632, 3633, 3634, 3635, 3636, 3637, 3638, 3639, 3640, 3641, 3642, 3643, 3644, 3645, 3649, 3660, 3663, 3664, 3679, 3716, 3717, 3718, 3719, 3720, 3726, 3727, 3728, 3734, 3735, 3745, 3772, 3780, 3783, 3785, 3786, 3787, 3789, 3791, 3793, 3798, 3818, 3821, 3822, 3849, 3850, 3858, 3860, 3861, 3865, 3870, 3871, 3881, 3915, 3925, 3926, 3927, 3930, 3934, 3936, 3937, 3938, 3939, 3940, 3941, 3942, 3943, 3944, 3949, 3950, 3951, 3953, 3954, 3955, 3956, 3957, 3960, 3964, 3965, 3966, 3967, 3968, 3969, 3970, 3971, 3972, 3975, 3980, 3981, 3982, 3983, 3984, 3986, 3987, 3988, 3989, 3991, 3992, 3993, 3997, 3999, 4000, 4006, 4009, 4014, 4015, 4017, 4021, 4026, 4080, 4122, 4129, 4130, 4131, 4133, 4141, 4142, 4144, 4154, 4155, 4156, 4157, 4162, 4163, 4164, 4167, 4172, 4177, 4178, 4184, 4189, 4190, 4191, 4192, 4193, 4197, 4198, 4199, 4204, 4209, 4215, 4219, 4223, 4224, 4226, 4227, 4228, 4231, 4233, 4235, 4236, 4237, 4238, 4239, 4240, 4242, 4243, 4244, 4259, 4261, 4267, 4269, 4270, 4273, 4274, 4275, 4276, 4303, 4304, 4321, 4331, 4332, 4350, 4351, 4353, 4354, 4356, 4357, 4358, 4359, 4360, 4361, 4362, 4363, 4364, 4366, 4367, 4368, 4369, 4370, 4371, 4372, 4375, 4376, 4377, 4380, 4388, 4389, 4403, 4404, 4405, 4414, 4415, 4416, 4417, 4419, 4427, 4489, 4499, 4561, 4571, 4595, 4596, 4597, 4613, 4614, 4615, 4616, 4623, 4626, 4631, 4632, 4633, 4634, 4658, 4662, 4663, 4666, 4671, 4672, 4687, 4699, 4703, 4705, 4706, 4707, 4708, 4709, 4735, 4736, 4737, 4738, 4739, 4740, 4741, 4742, 4743, 4744, 4747, 4775, 4776, 4777, 4778, 4805, 4808, 4811, 4812, 4813, 4846, 4847, 4848, 4849, 4871, 4878, 4896, 4897, 4906, 4911, 4913, 4914, 4915, 4943, 4944, 4952, 4954, 4958, 4962, 4963, 4973, 4974, 4999, 5012, 5025, 5038, 5059, 5170, 5174, 5180, 5181, 5193, 5194, 5195, 5196, 5197, 5199, 5201, 5202, 5203, 5204, 5217, 5219, 5239, 5265, 5269, 5272, 5309, 5321, 5333, 5345, 5356, 5371, 5384, 5397, 5410, 5424, 5438, 5449, 5461, 5471, 5478, 5507, 5509, 5514, 5515, 5517, 5526, 5527, 5529, 5530, 5532, 5533, 5564, 5567, 5568, 5573, 5574, 5575, 5576, 5593, 5598, 5599, 5600, 5601, 5608, 5611, 5614, 5617, 5620, 5627, 5629, 5631, 5633, 5639, 5640, 5643, 5644, 5646, 5648, 5658, 5659, 5663, 5664, 5665, 5666, 5667, 5668, 5669, 5727, 5820, 5821, 5928, 5953, 5954, 5962, 5963, 5964, 5970, 5975, 5979, 5984, 5985, 5986, 5987, 5988, 5989, 5990, 5991, 5992, 5993, 5994, 5995, 5996, 5997, 5998, 5999, 6000, 6001, 6002, 6003, 6004, 6005, 6006, 6007, 6008, 6009, 6010, 6011, 6012, 6013, 6014, 6015, 6038, 6039, 6040, 6041, 6042, 6046, 6047, 6048, 6052, 6055, 6057, 6070, 6071, 6072, 6073, 6074, 6085, 6087, 6089, 6090, 6091, 6092, 6093, 6095, 6096, 6104, 6105, 6106, 6111, 6113, 6117, 6118, 6123, 6124, 6125, 6145, 6146, 6148, 6149, 6152, 6153, 6173, 6177, 6197, 6198, 6199, 6205, 6221, 6225, 6237, 6238, 6242, 6243, 6244, 6267, 6268, 6271, 6289, 6290, 6291, 6292, 6297, 6298, 6299, 6300, 6303, 6307, 6308, 6309, 6311, 6312, 6313, 6314, 6315, 6316, 6317, 6318, 6319, 6320, 6321, 6324, 6325, 6326, 6330, 6332, 6333, 6338, 6339, 6341, 6342, 6344, 6347, 6348, 6349, 6350, 6351, 6352, 6356, 6367, 6368, 6373, 6374, 6375, 6380, 6381, 6385, 6392, 6395, 6396, 6397, 6400, 6401, 6408, 6413, 6414, 6421, 6422, 6423, 6424, 6425, 6427, 6428, 6434, 6435, 6440, 6441, 6443, 6445, 6446, 6447, 6450, 6452, 6457, 6459, 6460, 6461, 6462, 6463, 6464, 6466, 6467, 6468, 6469, 6470, 6471, 6472, 6473, 6474, 6475, 6480, 6481, 6482, 6487, 6488, 6489, 6492, 6493, 6495, 6496, 6499, 6500, 6503, 6506, 6511, 6513, 6514, 6515, 6516, 6517, 6522, 6523, 6524, 6530, 6532, 6536, 6538, 6539, 6540, 6542, 6545, 6546, 6551, 6552, 6553, 6555, 6556, 6557, 6558, 6559, 6562, 6563, 6566, 6568, 6653, 6668, 6670, 6672, 6675, 6678, 6679, 6683, 6686, 6694, 6700, 6701, 6702, 6703, 6704, 6709, 6715, 6719, 6729, 6738, 6788, 6852, 6853, 6907, 6908, 6910, 6912, 6914, 6916, 6917, 6918, 6919, 6920, 6921, 6922, 6925, 6926, 6929, 6941, 6947, 6948, 6961, 6963, 6964, 6965, 6966, 6967, 6971, 6994, 6995, 6996, 7092, 7055, 7056, 7058, 7092, 7097, 7107, 7127, 7131, 
        };

        #endregion IsBoss
    }
}