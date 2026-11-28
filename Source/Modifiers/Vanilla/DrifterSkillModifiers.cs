using System;
using EntityStates;
using EntityStates.Drifter;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.EntityStates.FalseSonBoss;
using On.RoR2.Projectile;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;
using UnityEngine;

namespace SkillsPlusPlus.Source.Modifiers
{
    [SkillLevelModifier(["DRIFTER_PRIMARY_NAME", "DRIFTER_UTILITY_C_NAME"], typeof(BluntForceBase), typeof(BluntForceEntry), typeof(BluntForceHit1), typeof(BluntForceHit2), typeof(BluntForceHit3), typeof(BluntForceTornado))]
    class DrifterBluntForceSkillModifier : BaseSkillModifier
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);
            if (skillState is BluntForceBase)
            {
                Log.Debug("BluntForceBase");
            }
            switch (skillState)
            {
                case BluntForceHit1 _:
                    Log.Debug("BluntForceHit1");
                    break;
                case BluntForceHit2 _:
                    Log.Debug("BluntForceHit2");
                    break;
                case BluntForceHit3 _:
                    Log.Debug("BluntForceHit3");
                    break;
            }
        }
    }

    [SkillLevelModifier("DRIFTER_SECONDARY_NAME", typeof(Cleanup))]
    class DrifterCleanupSkillModifier : BaseSkillModifier
    {

        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is Cleanup)
            {
                Log.Debug("Cleanup");
            }
        }
    }

    [SkillLevelModifier("DRIFTER_SECONDARY_ALT_NAME", typeof(JunkCube))]
    class DrifterJunkCubeSkillModifier : BaseSkillModifier
    {

        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is JunkCube)
            {
                Log.Debug("JunkCube");
            }
        }
    }

    [SkillLevelModifier(["DRIFTER_UTILITY_NAME", "DRIFTER_UTILITY_B_NAME"], typeof(Repossess), typeof(AimRepossess), typeof(RepossessBullseyeSearch))]
    class DrifterRepossessSkillModifier : BaseSkillModifier
    {

        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is Repossess)
            {
                Log.Debug("Repossess");
            } 
            else if (skillState is AimRepossess)
            {
                Log.Debug("AimRepossess");
            } 
        }
    }

    [SkillLevelModifier("DRIFTER_UTILITY_ALT_NAME", typeof(TornadoSlam), typeof(ChargeTornadoSlam))]
    class DrifterTornadoSlamSkillModifier : BaseSkillModifier
    {

        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is TornadoSlam)
            {
                Log.Debug("TornadoSlam");
            } 
            else if (skillState is ChargeTornadoSlam)
            {
                Log.Debug("ChargeTornadoSlam");
            }
        }
    }
    
    [SkillLevelModifier("DRIFTER_SPECIAL_NAME", typeof(Salvage))]
    class DrifterSalvageSkillModifier : BaseSkillModifier
    {
        public override void SetupSkill()
        {
            base.SetupSkill();
            
            IL.EntityStates.Drifter.Salvage.OnEnter += SalvageOnOnEnter;
        }

        private void SalvageOnOnEnter(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdcI4(4),
                    x => x.MatchStfld<Salvage>("itemsToDrop")
                ))
            {
                //c.Index -= 3;
                c.RemoveRange(3);
            }
            else 
            {
                Log.Error(il.Method.Name + " S++ IL Hook failed!");
            }
            
            Log.Debug("salvage = " + il.ToString());
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is not Salvage salvage) return;
            
            Log.Debug("Salvage");
            salvage.itemsToDrop = AdditiveScaling(4, 1, level);
            salvage.delayBetweenDrops = (salvage.delayBetweenDrops * 4) / salvage.itemsToDrop;
            Log.Debug("Salvage delay = " + salvage.delayBetweenDrops);
            //salvage.delayBetweenDrops = AdditiveScaling(salvage.itemsToDrop, 1, level);
        }
    }
    
    [SkillLevelModifier("DRIFTER_SPECIAL_ALT_NAME", typeof(CastTinker))]
    class DrifterTinkerSkillModifier : BaseSkillModifier
    {
        public override void SetupSkill()
        {
            base.SetupSkill();
            
            IL.RoR2.Projectile.TinkerProjectile.ManageMonster += TinkerProjectileOnManageMonster;
            IL.RoR2.Projectile.TinkerProjectile.TransmuteTargetObject += TinkerProjectileOnTransmuteTargetObject;
        }

        private void TinkerProjectileOnTransmuteTargetObject(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(
                    x => x.MatchLdloca(7),
                    x => x.MatchLdcR4(1),
                    x => x.MatchStfld(typeof(RoR2.UniquePickup), "decayValue")
                ))
            {
                c.Index += 1;
                c.Remove();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<RoR2.Projectile.TinkerProjectile, float>>((tinkerProjectile) =>
                    {
                        int level = GetSkillLevel(tinkerProjectile.ownerBody);
                        float newdecay = 1;
                        
                        newdecay += 0.5f * (level);
                        
                        Log.Debug("decayvalue else time = " + newdecay);
                        return newdecay;
                    }
                );
            }
            else 
            {
                Log.Error(il.Method.Name + " IL Hook failed!");
            }
            
            if (c.TryGotoNext(
                    x => x.MatchLdloca(6),
                    x => x.MatchLdcR4(1),
                    x => x.MatchStfld(typeof(RoR2.UniquePickup), "decayValue")
                ))
            {
                c.Index += 1;
                c.Remove();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<RoR2.Projectile.TinkerProjectile, float>>((tinkerProjectile) =>
                    {
                        int level = GetSkillLevel(tinkerProjectile.ownerBody);
                        float newdecay = 1;
                        
                        newdecay += 0.5f * (level);
                        
                        Log.Debug("decayvalue else time = " + newdecay);
                        return newdecay;
                    }
                );
            }
            else 
            {
                Log.Error(il.Method.Name + " S++ IL Hook failed!");
            }
        }

        private void TinkerProjectileOnManageMonster(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(
                    x => x.MatchLdloc(0),
                    x => x.MatchLdloc(1),
                    x => x.MatchLdcR4(4),
                    x => x.MatchCallvirt(typeof(RoR2.CharacterBody), "AddTimedBuff")
                ))
            {
                c.Index += 2;
                c.Remove();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<RoR2.Projectile.TinkerProjectile, float>>((tinkerProjectile) =>
                    {
                        int level = GetSkillLevel(tinkerProjectile.ownerBody);
                        
                        Log.Debug("debuff time = " + MultScaling(4f, 0.25f, level));
                        return MultScaling(4f, 0.25f, level);
                    }
                );
            }
            else 
            {
                Log.Error(il.Method.Name + " S++ IL Hook failed!");
            }
            
            Log.Debug("managemonster = " + il.ToString());
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is not CastTinker tinker) return;
            
            Log.Debug("CastTinker");
                
            tinker.projectilePrefab.transform.localScale = new Vector3(
                MultScaling(1, .25f, level),
                MultScaling(1, .25f, level),
                MultScaling(1, .25f, level));
            Log.Debug("scale = " + tinker.projectilePrefab.transform.localScale);
        }
    }
}