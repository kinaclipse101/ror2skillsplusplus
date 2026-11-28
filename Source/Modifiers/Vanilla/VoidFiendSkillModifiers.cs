using UnityEngine;

using SkillsPlusPlus.Modifiers;
using RoR2;

using EntityStates;
using EntityStates.VoidSurvivor.Weapon;
using static EntityStates.VoidSurvivor.VoidBlinkBase;

using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine.AddressableAssets;
using R2API;
using GlobalEventManager = On.RoR2.GlobalEventManager;

namespace SkillsPlusPlus.Source.Modifiers
{
    [SkillLevelModifier(["VOIDSURVIVOR_PRIMARY_NAME"], typeof(FireHandBeam), typeof(ChargeHandBeam), typeof(FireCorruptHandBeam), typeof(ChargeCorruptHandBeam))]
    class VoidFiendHandBeamSkillModifier : BaseSkillModifier 
    {
        CharacterBody surv;
        private int debuffTimerAdd;
        public BuffDef VoidFiendSpeedBuff;
        private int survlevel;
        public override void OnSkillLeveledUp(int level, CharacterBody characterBodyLevelup, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBodyLevelup, skillDef);
            surv = characterBodyLevelup;
            survlevel = level;
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);
            Log.Debug($"OnSkillEnter: {skillState.GetType().Name}");
            if(skillState is FireHandBeam beam) {
                Log.Debug(debuffTimerAdd);
                beam.damageCoefficient = MultScaling(beam.damageCoefficient, 0.10f, level);
                debuffTimerAdd = level;
                //beam.bulletCount = MultScaling(beam.bulletCount, 1, level);
            } else if (skillState is FireCorruptHandBeam corruptbeam) {
                corruptbeam.beamVfxPrefab.transform.localScale = new Vector3(corruptbeam.beamVfxPrefab.transform.localScale.x, corruptbeam.beamVfxPrefab.transform.localScale.y, MultScaling(1, 0.30f, level)); //vfx should extend a bit farther imo 
                Log.Debug("FireCorruptHandBeam" + corruptbeam.maxDistance);
                corruptbeam.maxDistance = MultScaling(corruptbeam.maxDistance, 0.25f, level);
                corruptbeam.minDistance = MultScaling(corruptbeam.minDistance, 0.25f, level);
                Log.Debug("FireCorruptHandBeam" + corruptbeam.maxDistance);
                Log.Debug("firecorruptmanged " + corruptbeam.damageCoefficientPerSecond);
                corruptbeam.damageCoefficientPerSecond = MultScaling(corruptbeam.damageCoefficientPerSecond, 0.10f, level);
                Log.Debug("firecorruptmanged " + corruptbeam.damageCoefficientPerSecond);
                if (surv && VoidFiendSpeedBuff && level > 0)
                {
                    surv.AddBuff(VoidFiendSpeedBuff);
                }
                else
                {
                    Log.Debug(surv);
                    Log.Debug(VoidFiendSpeedBuff);
                }
            }
        }
        public override void OnSkillExit(BaseState skillState, int level)
        {
            base.OnSkillExit(skillState, level);
            if (skillState is FireCorruptHandBeam corruptbeam)
            {
                if (surv && VoidFiendSpeedBuff)
                {
                    surv.RemoveBuff(VoidFiendSpeedBuff);
                }
            }
        }

        public override void SetupSkill()
        {
            RegisterSpeedBuff();
            On.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManagerOnOnHitEnemy;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPIOnGetStatCoefficients;
            base.SetupSkill();
        }

        private void RecalculateStatsAPIOnGetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(VoidFiendSpeedBuff))
            {
                args.baseMoveSpeedAdd += MultScaling(0.2f, 0.10f, survlevel);
            }
        }

        public void RegisterSpeedBuff()
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();

            buffDef.buffColor = new Color(0.9f, 0.6f, 0.9f);
            buffDef.canStack = false;
            buffDef.eliteDef = null;
            buffDef.iconSprite = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/SprintOutOfCombat/bdWhipBoost.asset").WaitForCompletion().iconSprite;
            buffDef.isDebuff = false;
            buffDef.name = "VoidFiendSpeedBuff";

            VoidFiendSpeedBuff = buffDef;
            ContentAddition.AddBuffDef(buffDef);
        }

        private void GlobalEventManagerOnOnHitEnemy(GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, DamageInfo damageinfo, GameObject victim)
        {
            orig(self, damageinfo, victim);
            if (!surv) return;
            if (damageinfo == null) return;
            if (damageinfo.damageType.damageSource != DamageSource.Primary) return;
            if (damageinfo.attacker != surv.gameObject) return;
            if (debuffTimerAdd == 0) return;
            if (surv.TryGetComponent(out VoidSurvivorController controller))
            {
                if (controller.isCorrupted)
                    return;
            }
            Log.Debug(debuffTimerAdd + " debuffing longer");
            if (victim.TryGetComponent(out CharacterBody charbody))
            {
                charbody.AddTimedBuff(RoR2Content.Buffs.Slow50, 3 + debuffTimerAdd); // 3 is the one the damagetype adds 
            }
        }
    }

    //flood ,,.
    [SkillLevelModifier("VOIDSURVIVOR_SECONDARY_NAME", typeof(ChargeMegaBlaster), typeof(FireMegaBlasterBase), typeof(FireMegaBlasterBig), typeof(FireMegaBlasterSmall))]
    internal class VoidFiendChargeMegaSkillModifier : BaseSkillModifier
    {
        public override void OnSkillEnter(BaseState skillState, int level)
        {
            if (skillState is ChargeMegaBlaster chargeMegaBlaster)
            {
                Log.Debug("ChargeMegaBlaster");
                chargeMegaBlaster.baseDuration = MultScaling(2f, -0.15f, level);
            }
            else if (skillState is FireMegaBlasterBase firemegablaster)
            {
                Log.Debug("FireMegaBlasterBase");
                firemegablaster.projectilePrefab.transform.localScale = new Vector3(MultScaling(1, 0.15f, level), MultScaling(1, 0.15f, level), MultScaling(1, 0.15f, level));
                Log.Debug(firemegablaster.projectilePrefab.tag);
                if (!firemegablaster.projectilePrefab.TryGetComponent(out ProjectileImpactExplosion impactExplosion))
                    return;
                impactExplosion.blastRadius = MultScaling(5, 0.15f, level);
                impactExplosion.blastDamageCoefficient = MultScaling(1, 0.10f, level);
                impactExplosion.impactEffect.transform.localScale = new Vector3(MultScaling(1, 0.15f, level), MultScaling(1, 0.15f, level), MultScaling(1, 0.15f, level));
            } /*else if (skillState is FireMegaBlasterBig) {
                Logger.Debug("FireMegaBlasterBig");
            } else if (skillState is FireMegaBlasterSmall) {
                Logger.Debug("FireMegaBlasterSmall");
            }*/
        }
    }

    // corrupt flood .,,.
    [SkillLevelModifier("VOIDSURVIVOR_SECONDARY_NAME", typeof(FireCorruptDisks))] 
    internal class VoidFiendFireCorruptDiskSkillModifier : SimpleSkillModifier<FireCorruptDisks>
    {
        private CharacterBody surv;
        public float stockamount;
        public int skilllevel;

        public override void OnSkillLeveledUp(int level, CharacterBody characterBodyLevelup, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBodyLevelup, skillDef);
            skilllevel = level;
            surv = characterBodyLevelup;
        }
        public override void OnSkillEnter(FireCorruptDisks skillState, int level)
        {
            if (!(skillState is FireCorruptDisks fireCorruptDisks)) return;
            Log.Debug("FireCorruptDisks");
            fireCorruptDisks.projectilePrefab.transform.localScale = new Vector3(MultScaling(1, 0.10f, level), MultScaling(1, 0.10f, level), MultScaling(1, 0.10f, level));
            Log.Debug(fireCorruptDisks.projectilePrefab.tag);
            if (!fireCorruptDisks.projectilePrefab.TryGetComponent(out ProjectileImpactExplosion impactExplosion))
                return;
            impactExplosion.blastDamageCoefficient = MultScaling(1, 0.15f, level);
        }

        public override void SetupSkill()
        {
            //On.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManagerOnOnHitEnemy;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPIOnGetStatCoefficients;
            base.SetupSkill();
        }

        private void RecalculateStatsAPIOnGetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender != surv) return;
            Log.Debug(sender.gameObject);
            Log.Debug(surv);
            // this should technically never be null but you never know ,..
            if (surv.TryGetComponent(out VoidSurvivorController controller))
            {
                if (controller.isCorrupted)
                    args.secondaryCooldownMultAdd -= 1 - MultScaling(1, -0.10f, skilllevel);
            }
            Log.Debug(1 - MultScaling(1, -0.10f, skilllevel));
        }
    }

    [SkillLevelModifier("VOIDSURVIVOR_UTILITY_NAME", typeof(VoidBlinkUp))]
    internal class VoidFiendVoidBlinkUpSkillModifier : SimpleSkillModifier<VoidBlinkUp>
    {
        private BuffDef VoidFiendArmorBuff;
        private CharacterBody surv;
        private int skilllevel;
        public override void OnSkillLeveledUp(int level, CharacterBody characterBodyLevelup, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBodyLevelup, skillDef);
            surv = characterBodyLevelup;
            skilllevel = level;
        }
        public override void OnSkillExit(VoidBlinkUp skillState, int level)
        {
            Log.Debug("VoidBlinkUp");
            if (level > 0)
                surv.AddTimedBuff(VoidFiendArmorBuff, 3);
        }
        public override void SetupSkill()
        {
            RegisterArmorBuff();
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPIOnGetStatCoefficients;
            base.SetupSkill();
        }

        public void RegisterArmorBuff()
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();

            buffDef.buffColor = new Color(0.9f, 0.4f, 0.7f);
            buffDef.canStack = false;
            buffDef.eliteDef = null;
            buffDef.iconSprite = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Common/bdHiddenInvincibility.asset").WaitForCompletion().iconSprite;
            buffDef.isDebuff = false;
            buffDef.name = "VoidFiendDamageSpeedBuff";

            VoidFiendArmorBuff = buffDef;
            ContentAddition.AddBuffDef(buffDef);
        }
        private void RecalculateStatsAPIOnGetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(VoidFiendArmorBuff))
            {
                args.armorAdd += AdditiveScaling(0, 50, skilllevel);
            }
        }
    }

    [SkillLevelModifier("VOIDSURVIVOR_UTILITY_NAME", typeof(VoidBlinkDown))]
    internal class VoidFiendVoidBlinkDownSkillModifier : SimpleSkillModifier<VoidBlinkDown>
    {
        private BuffDef VoidFiendDamageSpeedBuff;
        private CharacterBody surv;
        public override void OnSkillLeveledUp(int level, CharacterBody characterBodyLevelup, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBodyLevelup, skillDef);
            surv = characterBodyLevelup;
        }
        
        public override void OnSkillExit(VoidBlinkDown skillState, int level)
        {
            Log.Debug("VoidBlinkDown");
            if (level > 0)
                skillState.characterBody.AddTimedBuff(VoidFiendDamageSpeedBuff, level + 1);
        }

        public override void SetupSkill()
        {
            RegisterDamageSpeedBuff();
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPIOnGetStatCoefficients;
            base.SetupSkill();
        }

        private void RecalculateStatsAPIOnGetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (!sender.HasBuff(VoidFiendDamageSpeedBuff)) return;
            args.attackSpeedMultAdd += 0.20f;
            args.moveSpeedMultAdd += 0.20f;
        }

        public void RegisterDamageSpeedBuff()
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();

            buffDef.buffColor = new Color(0.9f, 0.4f, 0.7f);
            buffDef.canStack = false;
            buffDef.eliteDef = null;
            buffDef.iconSprite = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/SprintOutOfCombat/bdWhipBoost.asset").WaitForCompletion().iconSprite;
            buffDef.isDebuff = false;
            buffDef.name = "VoidFiendDamageSpeedBuff";

            VoidFiendDamageSpeedBuff = buffDef;
            ContentAddition.AddBuffDef(buffDef);
        }
    }

    [SkillLevelModifier("VOIDSURVIVOR_SPECIAL_NAME", typeof(CrushCorruption), typeof(ChargeCrushCorruption))]
    internal class VoidFiendCrushCorruptionSkillModifier : BaseSkillModifier
    {
        public override void OnSkillEnter(BaseState skillState, int level)
        {
            if (skillState is CrushCorruption crushCorruption)
            {
                Log.Debug("CrushCorruption");
                crushCorruption.selfHealFraction = AdditiveScaling(crushCorruption.selfHealFraction, 0.15f, level);
            }
        }
    }

    [SkillLevelModifier("VOIDSURVIVOR_SPECIAL_NAME", typeof(CrushHealth), typeof(ChargeCrushHealth))]
    internal class VoidFiendCrushHealthSkillModifier : BaseSkillModifier
    {
        private CharacterBody surv;
        public override void OnSkillLeveledUp(int level, CharacterBody characterBodyLevelup, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBodyLevelup, skillDef);
            surv = characterBodyLevelup;
        }
        public override void OnSkillEnter(BaseState skillState, int level)
        {
            if (skillState is CrushHealth crushHealth)
            {
                Log.Debug("CrushHealth");
                crushHealth.corruptionChange = AdditiveScaling(crushHealth.corruptionChange, 15f, level);
            }
            else if (skillState is ChargeCrushHealth)
            {
                Log.Debug("ChargeCrushHealth");
            }
        }
    }
}
