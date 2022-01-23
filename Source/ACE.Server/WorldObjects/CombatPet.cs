using System;
using System.Collections.Generic;

using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Managers;

namespace ACE.Server.WorldObjects
{
    /// <summary>
    /// Summonable monsters combat AI
    /// </summary>
    public partial class CombatPet : Pet
    {
        /// <summary>
        /// A new biota be created taking all of its values from weenie.
        /// </summary>
        public CombatPet(Weenie weenie, ObjectGuid guid) : base(weenie, guid)
        {
            SetEphemeralValues();
        }

        /// <summary>
        /// Restore a WorldObject from the database.
        /// </summary>
        public CombatPet(Biota biota) : base(biota)
        {
            SetEphemeralValues();
        }

        private void SetEphemeralValues()
        {
        }

        public override bool? Init(Player player, PetDevice petDevice)
        {
            var success = base.Init(player, petDevice);

            if (success == null || !success.Value)
                return success;

            SetCombatMode(CombatMode.Melee);
            MonsterState = State.Awake;
            IsAwake = true;

            // copy ratings from pet device
            DamageRating = petDevice.GearDamage;
            DamageResistRating = petDevice.GearDamageResist;
            CritDamageRating = petDevice.GearCritDamage;
            CritDamageResistRating = petDevice.GearCritDamageResist;
            CritRating = petDevice.GearCrit;
            CritResistRating = petDevice.GearCritResist;

            if (Level >= 200)
            {
                if (PetOwner != null)
                {
                    if (player != null)
                    {
                        float modifier = (float)PropertyManager.GetDouble("summons_bonus_mod").Item;

                        var end = player.Endurance.Base;
                        var self = player.Self.Base;

                        var total = end + self;

                        var byAmount = total * modifier;

                        var roundedAmount = Math.Round(byAmount);

                        var finalBonus = (uint)roundedAmount * (uint)(3 * roundedAmount);


                        Strength.StartingValue += finalBonus;
                        Endurance.StartingValue += finalBonus;
                        Coordination.StartingValue += finalBonus;
                        Quickness.StartingValue += finalBonus;
                        Focus.StartingValue += finalBonus;
                        Self.StartingValue += finalBonus;

                        Health.Current = Health.MaxValue;
                        Stamina.Current = Stamina.MaxValue;
                    }
                }
            }
            // summons level 15-100 will get a nice stat increase whenever their creature tick hits.
            else
            {
                switch (Level)
                {
                    case 15:
                        Strength.StartingValue += 30;
                        Endurance.StartingValue += 15;
                        Coordination.StartingValue += 30;
                        Quickness.StartingValue += 75;
                        Focus.StartingValue += 50;
                        Self.StartingValue += 50;
                        break;
                    case 30:
                        Strength.StartingValue += 50;
                        Endurance.StartingValue += 30;
                        Coordination.StartingValue += 50;
                        Quickness.StartingValue += 90;
                        Focus.StartingValue += 70;
                        Self.StartingValue += 70;
                        break;
                    case 50:
                        Strength.StartingValue += 75;
                        Endurance.StartingValue += 45;
                        Coordination.StartingValue += 75;
                        Quickness.StartingValue += 120;
                        Focus.StartingValue += 90;
                        Self.StartingValue += 90;
                        break;
                    case 80:
                        Strength.StartingValue += 100;
                        Endurance.StartingValue += 60;
                        Coordination.StartingValue += 100;
                        Quickness.StartingValue += 150;
                        Focus.StartingValue += 100;
                        Self.StartingValue += 100;
                        break;
                    case 100:
                        Strength.StartingValue += 120;
                        Endurance.StartingValue += 80;
                        Coordination.StartingValue += 120;
                        Quickness.StartingValue += 170;
                        Focus.StartingValue += 110;
                        Self.StartingValue += 110;
                        break;
                }

                Health.Current = Health.MaxValue;
            }

            // are CombatPets supposed to attack monsters that are in the same faction as the pet owner?
            // if not, there are a couple of different approaches to this
            // the easiest way for the code would be to simply set Faction1Bits for the CombatPet to match the pet owner's
            // however, retail pcaps did not contain Faction1Bits for CombatPets

            // doing this the easiest way for the code here, and just removing during appraisal
            Faction1Bits = player.Faction1Bits;

            return true;
        }

        public override void HandleFindTarget()
        {
            var creature = AttackTarget as Creature;

            if (creature == null || creature.IsDead || !IsVisibleTarget(creature))
                FindNextTarget();
        }

        public override bool FindNextTarget()
        {
            var nearbyMonsters = GetNearbyMonsters();
            if (nearbyMonsters.Count == 0)
            {
                //Console.WriteLine($"{Name}.FindNextTarget(): empty");
                return false;
            }

            // get nearest monster
            var nearest = BuildTargetDistance(nearbyMonsters, true);

            if (nearest[0].Distance > VisualAwarenessRangeSq)
            {
                //Console.WriteLine($"{Name}.FindNextTarget(): next object out-of-range (dist: {Math.Round(Math.Sqrt(nearest[0].Distance))})");
                return false;
            }

            AttackTarget = nearest[0].Target;

            //Console.WriteLine($"{Name}.FindNextTarget(): {AttackTarget.Name}");

            return true;
        }

        /// <summary>
        /// Returns a list of attackable monsters in this pet's visible targets
        /// </summary>
        public List<Creature> GetNearbyMonsters()
        {
            var monsters = new List<Creature>();

            foreach (var creature in PhysicsObj.ObjMaint.GetVisibleTargetsValuesOfTypeCreature())
            {
                // why does this need to be in here?
                if (creature.IsDead || !creature.Attackable || creature.Visibility)
                {
                    //Console.WriteLine($"{Name}.GetNearbyMonsters(): refusing to add dead creature {creature.Name} ({creature.Guid})");
                    continue;
                }

                // combat pets do not aggro monsters belonging to the same faction as the pet owner?
                if (SameFaction(creature))
                {
                    // unless the pet owner or the pet is being retaliated against?
                    if (!creature.HasRetaliateTarget(P_PetOwner) && !creature.HasRetaliateTarget(this))
                        continue;
                }

                monsters.Add(creature);
            }

            return monsters;
        }

        public override void Sleep()
        {
            // pets dont really go to sleep, per say
            // they keep scanning for new targets,
            // which is the reverse of the current ACE jurassic park model

            return;  // empty by default
        }
    }
}
