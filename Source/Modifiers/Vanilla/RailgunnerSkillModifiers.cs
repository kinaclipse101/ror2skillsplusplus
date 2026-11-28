using System;
using System.Collections.Generic;
using EntityStates;
using EntityStates.Railgunner.Backpack;
using EntityStates.Railgunner.Reload;
using EntityStates.Railgunner.Scope;
using EntityStates.Railgunner.Weapon;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AddressableAssets;
using BaseFireMine = On.EntityStates.Railgunner.Weapon.BaseFireMine;

namespace SkillsPlusPlus.Source.Modifiers
{
    [SkillLevelModifier("RAILGUNNER_PRIMARY_NAME", typeof(FirePistol))]
    internal class RailgunnerXQRSkillModifier : SimpleSkillModifier<FirePistol>
    {
        private float originalDuration = 0;
        private float originalLifetime = 0;

        public override void OnSkillEnter(FirePistol skillState, int level)
        {
            if (Mathf.Abs(originalDuration) < 0.01f)
            {
                originalDuration = skillState.baseDuration;

                ProjectileSimple prefabSimple = skillState.projectilePrefab.GetComponent<ProjectileSimple>();
                if (prefabSimple)
                {
                    originalLifetime = prefabSimple.lifetime;
                }
                else
                {
                    Log.Error("Failed to find the Railgunner ProjectilePrefab somehow");
                }
            }

            skillState.baseDuration = MultScaling(originalDuration, -0.15f, level);

            {
                ProjectileSimple prefabSimple = skillState.projectilePrefab.GetComponent<ProjectileSimple>();
                if (prefabSimple)
                {
                    prefabSimple.lifetime = MultScaling(originalLifetime, 0.20f, level);
                }
            }

            base.OnSkillEnter(skillState, level);
        }
    }

    [SkillLevelModifier(["RAILGUNNER_SECONDARY_NAME", "RAILGUNNER_SNIPE_HEAVY_NAME"], typeof(FireSnipeHeavy))]
    internal class RailgunnerM99SkillModifier : SimpleSkillModifier<FireSnipeHeavy>
    {
        public override void RecalcStats(int level, CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            Log.Debug("setting bonus skill + " + level);
            args.secondarySkill.bonusStockAdd += level/2;
        }

        public override void OnSkillEnter(FireSnipeHeavy skillState, int level)
        {
            skillState.baseDuration = MultScaling(skillState.baseDuration, -0.20f, level);
        }
    }

    [SkillLevelModifier(["RAILGUNNER_SECONDARY_ALT_NAME", "RAILGUNNER_SNIPE_LIGHT_NAME"], typeof(FireSnipeLight), typeof(WindUpScopeLight), typeof(WindDownScopeLight))]
    internal class RailgunnerHH44SkillModifier : BaseSkillModifier
    {
        private static BuffDef KillingSpreeBuff;
        private static BuffDef StrategicRetreatBuff;
        private static BuffDef startTrackingKillsBuff;
        private static bool bScopeActive;
        private static float KillingSpreeTimer = 0;
        private static float SpreeTimerGrace = 2f;

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            if (skillState is WindUpScopeLight scopedIn)
            {
                scopedIn.characterBody.SetBuffCount(startTrackingKillsBuff.buffIndex, 1);
            }
            
            switch (skillState)
            {
                case WindDownScopeLight scopeLight:
                    this.OnWindDownScopeLightEnter(scopeLight, level);
                    break;
            }

            base.OnSkillEnter(skillState, level);
        }

        public void OnWindDownScopeLightEnter(WindDownScopeLight skillState, int level)
        {
            skillState.characterBody.SetBuffCount(startTrackingKillsBuff.buffIndex, 0);
            
            int KSBuffCount = skillState.characterBody.GetBuffCount(KillingSpreeBuff);
            if (KSBuffCount == 0) return;
            
            skillState.characterBody.SetBuffCount(KillingSpreeBuff.buffIndex, 0);

            float SRDuration = 0.25f * level * KSBuffCount;
            foreach (CharacterBody.TimedBuff buff in skillState.characterBody.timedBuffs)
            {
                if (buff.buffIndex != StrategicRetreatBuff.buffIndex) continue;
                buff.timer += SRDuration;
                return;
            }
            
            skillState.characterBody.AddTimedBuff(StrategicRetreatBuff, SRDuration);
        }

        public void RegisterKillingBuffs()
        {
            BuffDef trackingBuff = ScriptableObject.CreateInstance<BuffDef>();
            trackingBuff.buffColor = new Color(0.78f, 0.20f, 0.78f);
            trackingBuff.canStack = true;
            trackingBuff.eliteDef = null;
            trackingBuff.iconSprite = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/CritOnUse/bdFullCrit.asset").WaitForCompletion().iconSprite;
            trackingBuff.isDebuff = true;
            trackingBuff.isHidden = true;
            trackingBuff.name = "RailgunnerTrackingKillsBuff";
            startTrackingKillsBuff = trackingBuff;
            ContentAddition.AddBuffDef(trackingBuff);
            
            BuffDef killingBuff = ScriptableObject.CreateInstance<BuffDef>();
            killingBuff.buffColor = new Color(0.78f, 0.20f, 0.78f);
            killingBuff.canStack = true;
            killingBuff.eliteDef = null;
            killingBuff.iconSprite = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/CritOnUse/bdFullCrit.asset").WaitForCompletion().iconSprite;
            killingBuff.isDebuff = true;
            killingBuff.name = "RailgunnerKillingSpreeBuff";
            KillingSpreeBuff = killingBuff;
            ContentAddition.AddBuffDef(killingBuff);

            
            BuffDef retreatBuff = ScriptableObject.CreateInstance<BuffDef>();
            retreatBuff.buffColor = new Color(0.74f, 0.36f, 0.23f);
            retreatBuff.canStack = false;
            retreatBuff.eliteDef = null;
            retreatBuff.iconSprite = Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC1/MoveSpeedOnKill/bdKillMoveSpeed.asset").WaitForCompletion().iconSprite;
            retreatBuff.isDebuff = true;
            retreatBuff.name = "RailgunnerStrategicRetreatBuff";
            StrategicRetreatBuff = retreatBuff;
            ContentAddition.AddBuffDef(retreatBuff);
        }

        public override void RecalcStats(int level, CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(StrategicRetreatBuff))
            {
                args.baseMoveSpeedAdd += 3f;
            }
            if (sender.HasBuff(KillingSpreeBuff))
            {
                args.armorAdd += (sender.GetBuffCount(KillingSpreeBuff) * level);
            }
        }
        
        public override void SetupSkill()
        {
            RegisterKillingBuffs();

            GlobalEventManager.onCharacterDeathServer += KilledEnemy;

            base.SetupSkill();
        }

        private void KilledEnemy(DamageReport damageReport)
        {
            if (damageReport?.attackerBody?.HasBuff(startTrackingKillsBuff) == true)
            {
                damageReport.attackerBody.AddBuff(KillingSpreeBuff);
            }
        }
    }

    [SkillLevelModifier("RAILGUNNER_UTILITY_NAME", typeof(FireMineConcussive))]
    internal class RailgunnerConcussiveMineSkillModifier : SimpleSkillModifier<FireMineConcussive>
    {
        private static BuffDef FeatherFallBuff;
        private static int ConcussiveProjectileCatalogIndex = -1;
        private static float baseThrowForce = 0;

        public override void OnSkillEnter(FireMineConcussive skillState, int level)
        {
            if (ConcussiveProjectileCatalogIndex == -1)
            {
                if (skillState.projectilePrefab.TryGetComponent(out ProjectileController projectileController))
                {
                    ConcussiveProjectileCatalogIndex = projectileController.catalogIndex;
                }
            }

            base.OnSkillEnter(skillState, level);
        }

        public override void SetupSkill()
        {
            RegisterFeatherFallBuff();

            On.RoR2.CharacterBody.FixedUpdate += CharacterBody_FixedUpdate;
            On.EntityStates.Railgunner.Weapon.BaseFireMine.ModifyProjectileInfo += BaseFireMineOnModifyProjectileInfo;

            base.SetupSkill();
        }

        private void BaseFireMineOnModifyProjectileInfo(BaseFireMine.orig_ModifyProjectileInfo orig, EntityStates.Railgunner.Weapon.BaseFireMine self, ref FireProjectileInfo fireprojectileinfo)
        {
            orig(self, ref fireprojectileinfo);
            if (self is not FireMineConcussive fireMineConcussive) return;
            
            fireprojectileinfo.force = MultScaling(fireprojectileinfo.force, 0.10f, GetSkillLevel(self.characterBody));
        }

        public void RegisterFeatherFallBuff()
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.buffColor = new Color(1f, 0.8f, 0.55f);
            buffDef.canStack = false;
            buffDef.eliteDef = null;
            buffDef.iconSprite = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Bandit2/bdCloakSpeed.asset").WaitForCompletion().iconSprite;
            buffDef.isDebuff = false;
            buffDef.name = "RailgunnerFeatherFallBuff";
            FeatherFallBuff = buffDef;
            ContentAddition.AddBuffDef(buffDef);
        }

        public override void TakeDamage(int level, HealthComponent self, DamageInfo damageInfo, CharacterBody attackerBody)
        {
            if (level == 0) return;
            if (self.body != attackerBody) return;
 
            if (damageInfo.inflictor && damageInfo.inflictor.TryGetComponent(out ProjectileController controller) && controller.catalogIndex == ConcussiveProjectileCatalogIndex)
            {
                //theres a new way to do this added in dlc3 but i forgot the name function it was ,..,
                for (int i = 0; i < level; i++)
                {
                    self.body.AddTimedBuff(FeatherFallBuff, 1.0f + (0.5f * level));
                }
                self.body?.characterMotor?.AddDisplacement(new Vector3(0f, 5f, 0f));
            }
        }

        public static void CharacterBody_FixedUpdate(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            if (self && self.HasBuff(FeatherFallBuff) && !self.characterMotor.isGrounded)
            {
                float upVelocity = self.characterMotor.velocity.y;

                upVelocity = Mathf.Max(upVelocity, -30f * (3.5f / (self.GetBuffCount(FeatherFallBuff) + 3.5f)));
                self.characterMotor.velocity = new Vector3(self.characterMotor.velocity.x, upVelocity, self.characterMotor.velocity.z);
            }

            orig(self);
        }
    }
    
    [SkillLevelModifier("RAILGUNNER_UTILITY_ALT_NAME", typeof(FireMineBlinding))]
    internal class RailgunnerPolarFieldMineSkillModifier : SimpleSkillModifier<FireMineBlinding>
    {
        public static BuffDef TimePressureDebuff;
        private static int PolarProjectileCatalogIndex = -1;
        private static float baseThrowForce = 0;

        public override void OnSkillEnter(FireMineBlinding skillState, int level)
        {
            FirstTimeSetup(skillState);

            if (skillState.projectilePrefab.TryGetComponent(out ProjectileSimple projectile))
            {
                projectile.desiredForwardSpeed = MultScaling(baseThrowForce, 0.15f, level);
            }
            
            base.OnSkillEnter(skillState, level);
        }

        private static void FirstTimeSetup(FireMineBlinding skillState)
        {
            if (PolarProjectileCatalogIndex != -1) return;
            
            if (skillState.projectilePrefab.TryGetComponent(out ProjectileController projectileController))
            {
                PolarProjectileCatalogIndex = projectileController.catalogIndex;
            }
            if (skillState.projectilePrefab.TryGetComponent(out ProjectileSimple projectileSimple))
            {
                baseThrowForce = projectileSimple.desiredForwardSpeed;
            }
        }

        public override void SetupSkill()
        {
            base.SetupSkill();
            
            RegisterTimePressureDebuff();
            
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_Railgunner.RailgunnerMineAltDetonated_prefab).Completed += handle =>
            {
                handle.Result.AddComponent<SppRailgunnerBlindingWardController>();
            };
        }

        //ik this makes it so its only for the railgunner that threw it but uhhhhhhhhhh idc its either this or a custom buffward implementation that adds a stack of a buff ,..,,
        public override void TakeDamage(int level, HealthComponent self, DamageInfo damageInfo, CharacterBody attackerBody)
        {
            if (self.body.HasBuff(TimePressureDebuff))
            {
                damageInfo.damage = MultScaling(damageInfo.damage, 0.1f, level);
            }
        }
        
        public void RegisterTimePressureDebuff()
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.buffColor = new Color(0.5f, 0.6f, 1f);
            buffDef.canStack = false;
            buffDef.eliteDef = null;
            buffDef.iconSprite = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Grandparent/bdOverheat.asset").WaitForCompletion().iconSprite;
            buffDef.isDebuff = true;
            buffDef.name = "RailgunnerTimePressureBuff";
            TimePressureDebuff = buffDef;
            ContentAddition.AddBuffDef(buffDef);
        }
    }
    
    public class SppRailgunnerBlindingWardController : MonoBehaviour
    {
        public void Start()
        {
            ProjectileController projectileController = GetComponent<ProjectileController>();
            int levelCount = SkillUpgradeHelper.GetSkillUpgradeFromName(projectileController.owner, "RAILGUNNER_UTILITY_ALT_NAME");
            if (levelCount <= 0) return;
                
            BuffWard oldWard = GetComponent<BuffWard>();
            BuffWard newWard = gameObject.AddComponent<BuffWard>();
            newWard.buffDef = RailgunnerPolarFieldMineSkillModifier.TimePressureDebuff;
            newWard.radius = oldWard.radius;
            newWard.shape = oldWard.shape;
            newWard.interval = oldWard.interval;
            newWard.rangeIndicator = oldWard.rangeIndicator;
            newWard.expires = oldWard.expires;
            newWard.invertTeamFilter = oldWard.invertTeamFilter;
            newWard.expireDuration = oldWard.expireDuration;
            newWard.buffDuration = oldWard.buffDuration;
        }
    }

    [SkillLevelModifier("RAILGUNNER_SPECIAL_NAME", typeof(FireSnipeSuper))]
    internal class RailgunnerSuperchargeSkillModifier : SimpleSkillModifier<FireSnipeSuper>
    {
        private static float originalProcRate = 0f;
        private static float originalCritMult = 0f;
        private static SkillUpgradeHelper.SkillUpgrades superchargeSkill;

        private static float originalRechargeRate;

        public override void OnSkillEnter(FireSnipeSuper skillState, int level)
        {
            if (Mathf.Abs(originalProcRate) < 0.1f)
            {
                originalProcRate = skillState.procCoefficient;
                originalCritMult = skillState.critDamageMultiplier;
            }

            if (level > 0)
            {
                skillState.procCoefficient = MultScaling(originalProcRate, 0.1f, level);
                skillState.critDamageMultiplier = MultScaling(originalCritMult, 0.2f, level);
            }

            base.OnSkillEnter(skillState, level);
        }
        public override void OnSkillLeveledUp(int level, CharacterBody characterBodyLevelup, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBodyLevelup, skillDef);

            if (superchargeSkill == null)
            {
                superchargeSkill = registeredSkill;
            }

            originalRechargeRate = 1 - (5f / (level + 5f));
            Log.Debug(originalRechargeRate);
        }

        public override void RecalcStats(int level, CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (level <= 0) return;
            args.utilitySkill.cooldownMultAdd -= 1 - (20f / (superchargeSkill.skillLevel + 20f));
            args.specialSkill.cooldownMultAdd -= originalRechargeRate;
        }
    }

    [SkillLevelModifier("RAILGUNNER_SPECIAL_ALT_NAME", typeof(FireSnipeCryo), typeof(ExpiredCryo), typeof(ChargedCryo))]
    internal class RailgunnerCryochargeSkillModifier : BaseSkillModifier
    {
        private static BuffDef scopedInBuff;
        private static BuffDef FrostfireBuff;
        private static DotController.DotIndex FrostfireIndex; 
        private static float originalRechargeRate = 0;

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is ChargedCryo cryo)
            {
                this.OnChargedCryo(cryo, level);
            }
        }

        public override void OnSkillExit(BaseState skillState, int level)
        {
            base.OnSkillExit(skillState, level);
            switch (skillState)
            {
                case FireSnipeCryo state:
                    this.OnFireSnipeCryo(state, level);
                    break;
                case ExpiredCryo state:
                    this.OnExpiredCryo(state, level);
                    break;
            }
        }

        public void OnFireSnipeCryo(FireSnipeCryo skillstate, int level)
        {
            skillstate.characterBody.SetBuffCount(scopedInBuff.buffIndex, 0);
        }

        public void OnExpiredCryo(ExpiredCryo skillstate, int level)
        {
            skillstate.characterBody.SetBuffCount(scopedInBuff.buffIndex, 0);
        }

        public void OnChargedCryo(ChargedCryo skillstate, int level)
        {
            skillstate.characterBody.SetBuffCount(scopedInBuff.buffIndex, level);
        }

        public void RegisterFrostfireBuff()
        {
            BuffDef checkerBuff = ScriptableObject.CreateInstance<BuffDef>();
            checkerBuff.buffColor = new Color(0.5f, 0.5f, 1f);
            checkerBuff.canStack = false;
            checkerBuff.eliteDef = null;
            checkerBuff.iconSprite = Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC1/StrengthenBurn/bdStrongerBurn.asset").WaitForCompletion().iconSprite;
            checkerBuff.isDebuff = true;
            checkerBuff.name = "RailgunnerCryoScopedBuff";
            checkerBuff.isHidden = true;
            scopedInBuff = checkerBuff;
            ContentAddition.AddBuffDef(checkerBuff);
            
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.buffColor = new Color(0.5f, 0.5f, 1f);
            buffDef.canStack = false;
            buffDef.eliteDef = null;
            buffDef.iconSprite = Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC1/StrengthenBurn/bdStrongerBurn.asset").WaitForCompletion().iconSprite;
            buffDef.isDebuff = true;
            buffDef.name = "RailgunnerFrostfireBuff";
            FrostfireBuff = buffDef;
            ContentAddition.AddBuffDef(buffDef);

            DotController.DotDef dotDef = new DotController.DotDef();
            dotDef.associatedBuff = FrostfireBuff;
            dotDef.damageCoefficient = 2f;
            dotDef.damageColorIndex = DamageColorIndex.Item;
            dotDef.terminalTimedBuff = FrostfireBuff;
            dotDef.interval = 0.5f;
            FrostfireIndex = DotAPI.RegisterDotDef(dotDef);
        }

        public override void TakeDamage(int level, HealthComponent self, DamageInfo di, CharacterBody attackerBody)
        {
            if (!attackerBody.HasBuff(scopedInBuff)) return;
            if (level <= 0) return;
                
            self.body.AddTimedBuff(FrostfireBuff, 2f * level);
            int ignitionTanks = attackerBody.inventory.GetItemCountEffective(DLC1Content.Items.StrengthenBurn);
            DotController.InflictDot(self.gameObject, di.attacker, FrostfireIndex, 2f * level, (1 + 3 * ignitionTanks), 1);
        }

        public override void SetupSkill()
        {
            RegisterFrostfireBuff();
            
            base.SetupSkill();
        }

        public override void RecalcStats(int level, CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            args.specialSkill.cooldownMultAdd -= 1 - (10f / (level + 10f));
        }
    }
}
