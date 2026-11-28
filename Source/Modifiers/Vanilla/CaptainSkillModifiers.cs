using System;
using UnityEngine;

using EntityStates;
using EntityStates.Captain.Weapon;
using EntityStates.CaptainSupplyDrop;

using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using SkillsPlusPlus.Source;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;

namespace SkillsPlusPlus.Modifiers
{
    [SkillLevelModifier("CAPTAIN_PRIMARY_NAME", typeof(FireCaptainShotgun), typeof(ChargeCaptainShotgun))]
    internal class CaptainShotgunSkillModifier : BaseSkillModifier
    {
        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);
            if (skillState is FireCaptainShotgun fireshotgun)
            {
                fireshotgun.bulletCount = (int)MultScaling(fireshotgun.bulletCount, 0.2f, level);
                fireshotgun.damageCoefficient = MultScaling(fireshotgun.damageCoefficient, 0.1f, level);
            }
        }
    }

    [SkillLevelModifier("CAPTAIN_SECONDARY_NAME", typeof(FireTazer))]
    internal class CaptainTaserSkillModifier : SimpleSkillModifier<FireTazer>
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBodyLevelup, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBodyLevelup, skillDef);
            if (FireTazer.projectilePrefab.TryGetComponent(out ProjectileImpactExplosion projectileImpactExplosion))
            {
                projectileImpactExplosion.blastRadius = MultScaling(2, 0.4f, level);
                projectileImpactExplosion.blastDamageCoefficient = MultScaling(1, 0.2f, level);
            }
        }
    }

    [SkillLevelModifier(["CAPTAIN_UTILITY_NAME", "ANCIENTSCEPTER_CAPTAIN_AIRSTRIKENAME"], typeof(CallAirstrike1), typeof(CallAirstrike2), typeof(CallAirstrike3))]
    internal class CaptainAirstrikeSkillModifier : SimpleSkillModifier<CallAirstrikeBase>
    {
        public override void OnSkillEnter(CallAirstrikeBase skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            var radius = MultScaling(8, 0.2f, level);
            if (skillState.projectilePrefab.TryGetComponent(out ProjectileImpactExplosion projectileImpactExplosion))
            {
                projectileImpactExplosion.blastRadius = radius;
                projectileImpactExplosion.blastDamageCoefficient = MultScaling(1, 0.2f, level);
            }
            if (skillState.projectilePrefab.TryGetComponent(out ProjectileController projectileController))
            {
                var expanderTransform = projectileController.ghostPrefab.transform.Find("Expander");
                if (expanderTransform != null)
                {
                    expanderTransform.localScale = Vector3.one * radius;
                }
            }
        }
    }

    [SkillLevelModifier("CAPTAIN_UTILITY_ALT1_NAME", typeof(CallAirstrikeAlt), typeof(SetupAirstrikeAlt))]
    internal class CaptainDiabloStrikeSkillModifier : BaseSkillModifier
    {
        private static int diabloStrikeProjectileCatalogIndex = -1; // i <3 magic numbers i guess ,m,m

        public override void OnSkillLeveledUp(int level, CharacterBody characterBodyLevelup, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBodyLevelup, skillDef);
            
            if (diabloStrikeProjectileCatalogIndex == -1)
            {
                diabloStrikeProjectileCatalogIndex = ProjectileCatalog.FindProjectileIndex("CaptainAirstrikeAltProjectile");
            }
        }

        public override void SetupSkill()
        {
            On.EntityStates.Captain.Weapon.CallAirstrikeBase.ModifyProjectile += CallAirstrikeBaseOnModifyProjectile;
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Captain.CaptainAirstrikeAltGhost_prefab).Completed +=
                handle =>
                {
                    handle.Result.AddComponent<DiabloEffectScaler>();
                };
            base.SetupSkill();
        }

        private void CallAirstrikeBaseOnModifyProjectile(On.EntityStates.Captain.Weapon.CallAirstrikeBase.orig_ModifyProjectile orig, CallAirstrikeBase self, ref FireProjectileInfo fireprojectileinfo)
        {
            orig(self, ref fireprojectileinfo);

            if (self is not CallAirstrikeAlt) return;
            
            int skillLevel = SkillUpgradeHelper.GetSkillUpgradeFromName(self.gameObject, "CAPTAIN_UTILITY_ALT1_NAME");
            if (skillLevel <= 0) return;
            
            fireprojectileinfo.useFuseOverride = true;
            fireprojectileinfo.fuseOverride = Mathf.Clamp(20f - skillLevel, 0, 20);
        }

        public override void RecalcStats(int level, CharacterBody sender, StatHookEventArgs args)
        {
            args.utilityCooldownMultAdd -= 1 - (5f / (level + 5f));
        }
        
        public override void TakeDamage(int level, HealthComponent self, DamageInfo di, CharacterBody characterBody)
        {
            if (level <= 0) return;
            if (diabloStrikeProjectileCatalogIndex == -1)
            {
                diabloStrikeProjectileCatalogIndex = ProjectileCatalog.FindProjectileIndex("CaptainAirstrikeAltProjectile");
            }

            if (!di.inflictor || !di.inflictor.TryGetComponent(out ProjectileController controller) || controller.catalogIndex != diabloStrikeProjectileCatalogIndex) return;
            if (self?.body?.teamComponent?.teamIndex != characterBody?.teamComponent?.teamIndex) return;
            
            var friendlyFireProtection = (25 / ((float)level + 25));
            if (self.combinedHealthFraction <= friendlyFireProtection) return;
            
            di.damage = (self.combinedHealth - (level * 0.04f * self.fullCombinedHealth)) * 2;    //Friendly fire deals half damage
            di.damageType |= DamageType.BypassArmor;
            di.damageType &= ~DamageType.AOE;
        }
    }

    public class DiabloEffectScaler : MonoBehaviour
    {
        private ProjectileGhostController ghostController;
        private bool applied;

        private void OnEnable()
        {
            applied = false;
        }

        //start only happens on the first ghost for some reason and onenable is too early i think ?,..,
        private void FixedUpdate()
        {
            if (applied) return;
            
            applied = true;
            Setup();
        }

        private void Setup()
        {
            ghostController = GetComponent<ProjectileGhostController>();
            int skillLevel = SkillUpgradeHelper.GetSkillUpgradeFromName(ghostController.authorityTransform?.GetComponent<ProjectileController>()?.owner, "CAPTAIN_UTILITY_ALT1_NAME");

            var CenterTransform = transform.Find("AreaIndicatorCenter");
            if (!CenterTransform) return;
            
            for (int i = 0; i < CenterTransform.childCount; i++)
            {
                var child = CenterTransform.GetChild(i);

                //Update the Ring Animation
                if (child.TryGetComponent(out ObjectScaleCurve scaleCurve))
                {
                    scaleCurve.timeMax = Mathf.Clamp(20f - skillLevel, 0, 20);
                }
                else
                {
                    //Dodge Rings and update the Lasers
                    var vertical = child.Find("LaserVerticalOffset");
                    if (!vertical) continue;
                    
                    var laser = vertical.Find("Laser");
                    if (!laser) continue;
                    
                    if (laser.gameObject.TryGetComponent(out ObjectTransformCurve laserCurve))
                    {
                        laserCurve.timeMax = Mathf.Clamp(20f - skillLevel, 0, 20);
                    }
                }
            }
        }
    }

    [SkillLevelModifier([
        "CAPTAIN_SPECIAL_NAME", 
        "CAPTAIN_SKILL_USED_UP_NAME", 
        "CAPTAIN_SKILL_DISCONNECT_NAME", 
        "CAPTAIN_SUPPLY_HEAL_NAME", 
        "CAPTAIN_SUPPLY_SHOCKING_NAME",
        "CAPTAIN_SUPPLY_EQUIPMENT_RESTOCK_NAME",
        "CAPTAIN_SUPPLY_HACKING_NAME"
    ], typeof(SetupSupplyDrop), typeof(DeployState), typeof(HealZoneMainState), typeof(ShockZoneMainState), typeof(HackingMainState), typeof(HackingInProgressState), typeof(EquipmentRestockMainState))]
    internal class CaptainSupplyDropHealingSkillModifier : BaseSkillModifier
    {
        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is DeployState deploying)
            {
                var modelLocator = deploying.outer?.commonComponents.modelLocator;
                if (modelLocator != null)
                {
                    var indicatorTransform = modelLocator.modelTransform?.Find("Indicator");
                    if (indicatorTransform != null)
                    {
                        indicatorTransform.localScale = Vector3.one * HackingMainState.baseRadius / 2;
                        if (indicatorTransform.TryGetComponent(out ObjectScaleCurve objectScaleCurve))
                        {
                            objectScaleCurve.baseScale = indicatorTransform.localScale;
                        }
                    }
                }
            }

            if (skillState is EquipmentRestockMainState equipmentRestock)
            {
                equipmentRestock.activationCost = 100f / AdditiveScaling(3, 1, level);
            }
        }

        public override void OnSkillLeveledUp(int level, CharacterBody characterBodyLevelup, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBodyLevelup, skillDef);
            if (HealZoneMainState.healZonePrefab.TryGetComponent(out HealingWard healingWard))
            {
                var healRadius = MultScaling(10, 0.2f, level);
                healingWard.radius = healRadius;
            }
            ShockZoneMainState.shockRadius = MultScaling(10, 0.3f, level);

            HackingMainState.baseRadius = MultScaling(10, 0.2f, level);
            HackingInProgressState.baseDuration = MultScaling(15, -0.2f, level);

            var warningZone = SetupSupplyDrop.blueprintPrefab.transform.Find("Base/Warning");
            if (warningZone)
            {
                warningZone.localScale = new Vector3(HackingMainState.baseRadius / 2, HackingMainState.baseRadius / 2, warningZone.localScale.z);
            }
        }
    }
}
