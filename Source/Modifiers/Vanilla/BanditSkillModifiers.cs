using EntityStates.Bandit2;
using EntityStates.Bandit2.Weapon;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using SkillsPlusPlus.Source;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;

namespace SkillsPlusPlus.Modifiers
{
    [SkillLevelModifier("BANDIT2_PRIMARY_NAME", typeof(FireShotgun2))]
    internal class BanditFireShotgunSkillModifier : SimpleSkillModifier<FireShotgun2>
    {
        public override void OnSkillEnter(FireShotgun2 skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            skillState.bulletCount += level;
            skillState.damageCoefficient = MultScaling(skillState.damageCoefficient, 0.05f, level);

            skillState.minFixedSpreadYaw += level * 0.5f;
            skillState.maxFixedSpreadYaw += level;
        }
    }

    [SkillLevelModifier("BANDIT2_PRIMARY_ALT_NAME", typeof(Bandit2FireRifle))]
    internal class BanditFireRifleSkillModifier : SimpleSkillModifier<Bandit2FireRifle>
    {
        private float baseBloom = 0;
        public override void OnSkillEnter(Bandit2FireRifle skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (Mathf.Abs(baseBloom) < 0.01f)
            {
                baseBloom = skillState.spreadBloomValue;
            }

            if (level > 0)
            {
                skillState.procCoefficient = AdditiveScaling(skillState.procCoefficient, 0.1f, level);
                skillState.damageCoefficient = MultScaling(skillState.damageCoefficient, 0.2f, level);

                skillState.spreadBloomValue = baseBloom * (4 / ((float)level + 4));
            }
        }
    }

    [SkillLevelModifier("BANDIT2_SECONDARY_NAME", typeof(SlashBlade))]
    internal class BanditBladeSkillModifier : SimpleSkillModifier<SlashBlade>
    {
        private Vector3 originalHitboxScale = Vector3.zero;
        
        public override void OnSkillEnter(SlashBlade slash, int level)
        {
            //Visual Scaling
            slash.swingEffectPrefab.transform.localScale = new Vector3(1 + level, 1 + level, 1 + level);

            base.OnSkillEnter(slash, level);

            slash.damageCoefficient = MultScaling(slash.damageCoefficient, 0.2f, level);
        }

        public override void OnSkillLeveledUp(int level, CharacterBody characterBodyLevelup, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBodyLevelup, skillDef);

            //Hitbox Scaling
            HitBoxGroup hitboxGroup = characterBodyLevelup.modelLocator.modelTransform.GetComponent<HitBoxGroup>();

            if (!hitboxGroup || hitboxGroup.groupName != "SlashBlade")
            {
                Debug.LogWarning("didn't get Bandit's slashHitbox. probably got changed?. aborting");

                return;
            }

            Transform hitboxTransform = hitboxGroup.hitBoxes[0].transform;

            if (originalHitboxScale == Vector3.zero)
            {
                originalHitboxScale = hitboxTransform.localScale;
            }
            hitboxTransform.localScale = new Vector3(MultScaling(originalHitboxScale.x, 0.2f, level), MultScaling(originalHitboxScale.y, 0.2f, level), MultScaling(originalHitboxScale.z, 0.3f, level));
        }
    }

    [SkillLevelModifier("BANDIT2_SECONDARY_ALT_NAME", typeof(Bandit2FireShiv))]
    internal class BanditFireShivSkillModifier : SimpleSkillModifier<Bandit2FireShiv>
    {
        public override void OnSkillEnter(Bandit2FireShiv skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            skillState.maxShivCount = 1 + (level / 2);
            skillState.baseDuration = skillState.baseDuration + (skillState.baseDuration * (level / 2) * 0.5f);
            skillState.damageCoefficient = MultScaling(skillState.damageCoefficient, 0.1f, level);
        }
    }

    [SkillLevelModifier("BANDIT2_UTILITY_NAME", typeof(ThrowSmokebomb))]
    internal class BanditSkillThrowSmokebombModifier : SimpleSkillModifier<ThrowSmokebomb>
    {
        private static float baseRadius = 0;
        private static float baseDamage = 0;
        private static BuffDef BanditSpeedBuff;

        public static void RegisterBanditSpeedBuff()
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();

            buffDef.buffColor = new Color(0.6f, 0.11f, 0.59f);
            buffDef.buffIndex = (BuffIndex)63;
            buffDef.canStack = true;
            buffDef.eliteDef = null;
            buffDef.iconSprite = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Bandit2/bdCloakSpeed.asset").WaitForCompletion().iconSprite;
            buffDef.isDebuff = false;
            buffDef.name = "BanditSpeedBuff";

            BanditSpeedBuff = buffDef;
            ContentAddition.AddBuffDef(buffDef);
        }

        public override void OnSkillEnter(ThrowSmokebomb skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            for (int i = 0; i < level; i++)
            {
                skillState.outer.commonComponents.characterBody.AddTimedBuff(BanditSpeedBuff, StealthMode.duration);
            }
        }

        public override void OnSkillLeveledUp(int level, CharacterBody characterBodyLevelup, SkillDef skillDef)
        {
            if (Mathf.Abs(baseRadius - 0) < 0.1f)
            {
                baseRadius = StealthMode.blastAttackRadius;
                baseDamage = StealthMode.blastAttackDamageCoefficient;
            }

            StealthMode.blastAttackRadius = MultScaling(baseRadius, 0.15f, level);
            StealthMode.blastAttackDamageCoefficient = MultScaling(baseDamage, 0.2f, level);
        }

        public static void RecalculateStats(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender?.HasBuff(BanditSpeedBuff) == true)
            {
                args.baseMoveSpeedAdd += sender.GetBuffCount(BanditSpeedBuff);
            }
        }

        public override void SetupSkill()
        {
            base.SetupSkill();

            RegisterBanditSpeedBuff();
            GetStatCoefficients += RecalculateStats;
        }
    }


    [SkillLevelModifier("BANDIT2_SPECIAL_NAME", typeof(FireSidearmResetRevolver))]
    internal class BanditSkillResetRevolverModifier : SimpleSkillModifier<FireSidearmResetRevolver>
    {
        public override void TakeDamage(int level, HealthComponent self, DamageInfo damageInfo, CharacterBody attackerBody)
        {
            if ((damageInfo?.damageType & RoR2.DamageType.BonusToLowHealth) == RoR2.DamageType.BonusToLowHealth &&
                (damageInfo?.damageType & RoR2.DamageType.ResetCooldownsOnKill) == RoR2.DamageType.ResetCooldownsOnKill)
            {
                if (level > 0)
                {
                    Log.Debug($"level = {level} mult {Mathf.Lerp(1.0f + level * 0.3f, 1.0f + level * 0.1f, self.combinedHealthFraction)}");
                    damageInfo.damage *= Mathf.Lerp(1.0f + level * 0.3f, 1.0f + level * 0.1f, self.combinedHealthFraction);
                }
            }
        }
    }


    [SkillLevelModifier("BANDIT2_SPECIAL_ALT_NAME", typeof(FireSidearmSkullRevolver))]
    internal class BanditSkillSkullRevolverModifier : SimpleSkillModifier<FireSidearmSkullRevolver>
    {
        public override void TakeDamage(int level, HealthComponent self, DamageInfo di, CharacterBody attackerBody)
        {
            if (di != null && self != null)
            {
                if ((di.damageType & RoR2.DamageType.GiveSkullOnKill) == RoR2.DamageType.GiveSkullOnKill)
                {
                    if(level > 0)
                    {
                        float remainingHealth = (self.combinedHealth - di.damage);
                        if (remainingHealth / self.fullCombinedHealth < level * 0.01f && remainingHealth > 0)
                        {
                            di.damage += remainingHealth;
                            di.damageType |= DamageType.BypassArmor;
                        }
                    }
                }
            }
        }
    }
}
