using System;
using System.Collections.Generic;

using log4net;

using ACE.Common;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using ACE.Entity.Enum.Properties;
using ACE.DatLoader;
using ACE.Server.Factories;


namespace ACE.Server.Command.Handlers
{
    public static class PlayerCommands
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // pop
        [CommandHandler("pop", AccessLevel.Player, CommandHandlerFlag.None, 0,
            "Show current world population",
            "")]
        public static void HandlePop(Session session, params string[] parameters)
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Current world population: {PlayerManager.GetOnlineCount():N0}", ChatMessageType.Broadcast);
        }
        [CommandHandler("checkxp", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0)]
        public static void HandleCheckXp(Session session, params string[] parameters)
        {
            if (session.Player.Level >= 275)
            {
                var currentxp = session.Player.TotalXpBeyond;

                var currentremaining = currentxp - session.Player.TotalExperience;

                session.Network.EnqueueSend(new GameMessageSystemChat($"You need {currentremaining:N0}xp to reach level {session.Player.Level + 1}. Required total xp is {currentxp:N0}", ChatMessageType.Broadcast));
            }
            else
                return;
        }

        private static long CalculateAttributeCost(ref int amt, int currentAmt, long availableXp, bool max)
        {
            ulong maxLong = 9223372036854775807;
            ulong attrcost;
            ulong multiamount = 0UL;
            var attrCostEthereal = currentAmt;

            for (var i = 1; i <= amt || max; i++)
            {
                attrcost = (ulong)Math.Round(500000000D * Math.Pow((1D + (attrCostEthereal / 125D)), 3D));
                if (((multiamount + attrcost) > maxLong) || (max && (long)(multiamount + attrcost) > availableXp))
                {
                    amt = i - 1;
                    break;
                }

                multiamount += attrcost;
                attrCostEthereal++;
            }
            return (long)multiamount;
        }

        [CommandHandler("raise", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Raise attributes over max.")]
        public static void HandleAttribute(Session session, params string[] parameters)
        {
            var amt = 1;
            bool max = false;
            if (parameters.Length > 1)
            {
                if (parameters[1].Equals("max", StringComparison.OrdinalIgnoreCase))
                {
                    max = true;
                }
                else
                {
                    int.TryParse(parameters[1], out amt);
                }
            }

            if (parameters[0].Equals("str", StringComparison.OrdinalIgnoreCase))
            {
                var str = session.Player.Strength;

                if (!str.IsMaxRank)
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat($"Your Strength is not max level yet. Please raise strength until it is maxxed out. ", ChatMessageType.Broadcast));
                    return;
                }

                var xpCost = CalculateAttributeCost(ref amt, session.Player.RaisedStr, session.Player.AvailableExperience ?? 0, max);

                if (session.Player.AvailableExperience < xpCost || amt == 0)
                {
                    if (amt > 1)
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Strength up {amt} times. ", ChatMessageType.Broadcast));
                    }
                    else
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Strength up. ", ChatMessageType.Broadcast));
                    }
                    return;
                }

                session.Player.RaisedStr += amt;
                str.StartingValue += (uint)amt;
                session.Player.AvailableExperience -= xpCost;


                session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt64(session.Player, PropertyInt64.AvailableExperience, session.Player.AvailableExperience ?? 0));
                session.Network.EnqueueSend(new GameMessagePrivateUpdateAttribute(session.Player, session.Player.Strength));

                session.Network.EnqueueSend(new GameMessageSystemChat($"You have increased your Strength to {str.Base}! XP spent {xpCost:N0}", ChatMessageType.Advancement));
            }

            else if (parameters[0].Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                var end = session.Player.Endurance;

                if (!end.IsMaxRank)
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat($"Your Endurance is not max level yet. Please raise Endurance until it is maxxed out. ", ChatMessageType.Broadcast));
                    return;
                }

                var xpCost = CalculateAttributeCost(ref amt, session.Player.RaisedEnd, session.Player.AvailableExperience ?? 0, max);

                if (session.Player.AvailableExperience < xpCost || amt == 0)
                {
                    if (amt > 1)
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Endurance up {amt} times. ", ChatMessageType.Broadcast));
                    }
                    else
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Endurance up. ", ChatMessageType.Broadcast));
                    }
                    return;
                }

                session.Player.RaisedEnd += amt;
                end.StartingValue += (uint)amt;
                session.Player.AvailableExperience -= xpCost;


                session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt64(session.Player, PropertyInt64.AvailableExperience, session.Player.AvailableExperience ?? 0));
                session.Network.EnqueueSend(new GameMessagePrivateUpdateAttribute(session.Player, session.Player.Endurance));

                session.Network.EnqueueSend(new GameMessageSystemChat($"You have increased your Endurance to {end.Base}! XP spent {xpCost:N0}", ChatMessageType.Advancement));
            }

            else if (parameters[0].Equals("coord", StringComparison.OrdinalIgnoreCase))
            {
                var coord = session.Player.Coordination;

                if (!coord.IsMaxRank)
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat($"Your Coordination is not max level yet. Please raise Coordination until it is maxxed out. ", ChatMessageType.Broadcast));
                    return;
                }

                var xpCost = CalculateAttributeCost(ref amt, session.Player.RaisedCoord, session.Player.AvailableExperience ?? 0, max);

                if (session.Player.AvailableExperience < xpCost || amt == 0)
                {
                    if (amt > 1)
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Coordination up {amt} times. ", ChatMessageType.Broadcast));
                    }
                    else
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Coordination up. ", ChatMessageType.Broadcast));
                    }
                    return;
                }

                session.Player.RaisedCoord += amt;
                coord.StartingValue += (uint)amt;
                session.Player.AvailableExperience -= xpCost;


                session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt64(session.Player, PropertyInt64.AvailableExperience, session.Player.AvailableExperience ?? 0));
                session.Network.EnqueueSend(new GameMessagePrivateUpdateAttribute(session.Player, session.Player.Coordination));

                session.Network.EnqueueSend(new GameMessageSystemChat($"You have increased your Coordination to {coord.Base}! XP spent {xpCost:N0}", ChatMessageType.Advancement));
            }

            else if (parameters[0].Equals("quick", StringComparison.OrdinalIgnoreCase))
            {
                var quick = session.Player.Quickness;

                if (!quick.IsMaxRank)
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat($"Your Quickness is not max level yet. Please raise Quickness until it is maxxed out. ", ChatMessageType.Broadcast));
                    return;
                }

                var xpCost = CalculateAttributeCost(ref amt, session.Player.RaisedQuick, session.Player.AvailableExperience ?? 0, max);

                if (session.Player.AvailableExperience < xpCost || amt == 0)
                {
                    if (amt > 1)
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Quickness up {amt} times. ", ChatMessageType.Broadcast));
                    }
                    else
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Quickness up. ", ChatMessageType.Broadcast));
                    }
                    return;
                }

                session.Player.RaisedQuick += amt;
                quick.StartingValue += (uint)amt;
                session.Player.AvailableExperience -= xpCost;


                session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt64(session.Player, PropertyInt64.AvailableExperience, session.Player.AvailableExperience ?? 0));
                session.Network.EnqueueSend(new GameMessagePrivateUpdateAttribute(session.Player, session.Player.Quickness));

                session.Network.EnqueueSend(new GameMessageSystemChat($"You have increased your Quickness to {quick.Base}! XP spent {xpCost:N0}", ChatMessageType.Advancement));
            }

            else if (parameters[0].Equals("focus", StringComparison.OrdinalIgnoreCase))
            {
                var focus = session.Player.Focus;

                if (!focus.IsMaxRank)
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat($"Your Focus is not max level yet. Please raise Focus until it is maxxed out. ", ChatMessageType.Broadcast));
                    return;
                }

                var xpCost = CalculateAttributeCost(ref amt, session.Player.RaisedFocus, session.Player.AvailableExperience ?? 0, max);

                if (session.Player.AvailableExperience < xpCost || amt == 0)
                {
                    if (amt > 1)
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Focus up {amt} times. ", ChatMessageType.Broadcast));
                    }
                    else
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Focus up. ", ChatMessageType.Broadcast));
                    }
                    return;
                }

                session.Player.RaisedFocus += amt;
                focus.StartingValue += (uint)amt;
                session.Player.AvailableExperience -= xpCost;


                session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt64(session.Player, PropertyInt64.AvailableExperience, session.Player.AvailableExperience ?? 0));
                session.Network.EnqueueSend(new GameMessagePrivateUpdateAttribute(session.Player, session.Player.Focus));

                session.Network.EnqueueSend(new GameMessageSystemChat($"You have increased your Focus to {focus.Base}! XP spent {xpCost:N0}", ChatMessageType.Advancement));
            }


            else if (parameters[0].Equals("self", StringComparison.OrdinalIgnoreCase))
            {
                var self = session.Player.Self;

                if (!self.IsMaxRank)
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat($"Your Self is not max level yet. Please raise Self until it is maxxed out. ", ChatMessageType.Broadcast));
                    return;
                }

                var xpCost = CalculateAttributeCost(ref amt, session.Player.RaisedSelf, session.Player.AvailableExperience ?? 0, max);

                if (session.Player.AvailableExperience < xpCost || amt == 0)
                {
                    if (amt > 1)
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Self up {amt} times. ", ChatMessageType.Broadcast));
                    }
                    else
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available experience to level your Self up. ", ChatMessageType.Broadcast));
                    }
                    return;
                }

                session.Player.RaisedSelf += amt;
                self.StartingValue += (uint)amt;
                session.Player.AvailableExperience -= xpCost;


                session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt64(session.Player, PropertyInt64.AvailableExperience, session.Player.AvailableExperience ?? 0));
                session.Network.EnqueueSend(new GameMessagePrivateUpdateAttribute(session.Player, session.Player.Self));

                session.Network.EnqueueSend(new GameMessageSystemChat($"You have increased your Self to {self.Base}! XP spent {xpCost:N0}", ChatMessageType.Advancement));
            }

            else
            {

                session.Network.EnqueueSend(new GameMessageSystemChat($"must specify which attribute you wish to raise. ex. /raise STR or /raise END or /raise COORD or /raise QUICK or /raise FOCUS or /raise SELF", ChatMessageType.Broadcast));

            }

        }

        [CommandHandler("enlighten", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Raise Luminance Augs over max.")]
        public static void HandleLuminanceAugs(Session session, params string[] parameters)
        {
            if (parameters.Length > 0)
            {
                if (parameters[0].Equals("Defense", StringComparison.OrdinalIgnoreCase) || parameters[0].Equals("DRR", StringComparison.OrdinalIgnoreCase))
                {
                    var drr = session.Player.LumAugDamageReductionRating;

                    // Do we want to require they already have their 5 Lum Augs from Asheron's Castle?
                    // if (drr < 5) {
                    // session.Network.EnqueueSend(new GameMessageSystemChat($"You do not yet have 5 Defense Luminance Augs, please raise your Defense Rating at Asheron's Castle first.", ChatMessageType.Broadcast));
                    // return;
                    // }
                    var lumCost = 15000000;
                    if (session.Player.SpendLuminance(lumCost))
                    {
                        var newDRR = drr + 1;
                        session.Player.UpdateProperty(session.Player, PropertyInt.LumAugDamageReductionRating, newDRR);
                        session.Player.EnqueueBroadcast(false, new GameMessagePublicUpdatePropertyInt(session.Player, PropertyInt.LumAugDamageReductionRating, Convert.ToInt32(newDRR)));
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You have increased your Defense Rating to {newDRR}! Luminance spent {lumCost:N0}", ChatMessageType.Advancement));
                    }
                    else
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available luminance to raise your Defense Rating.", ChatMessageType.Broadcast));
                    }
                    return;
                }

                if (parameters[0].Equals("Damage", StringComparison.OrdinalIgnoreCase) || parameters[0].Equals("DR", StringComparison.OrdinalIgnoreCase))
                {
                    var dr = session.Player.LumAugDamageRating;

                    // Do we want to require they already have their 5 Lum Augs from Asheron's Castle?
                    // if (dr < 5) {
                    // session.Network.EnqueueSend(new GameMessageSystemChat($"You do not yet have 5 Damage Luminance Augs, please raise your Damage Rating at Asheron's Castle first.", ChatMessageType.Broadcast));
                    // return;
                    // }
                    var lumCost = 15000000;
                    if (session.Player.SpendLuminance(lumCost))
                    {
                        var newDR = dr + 1;
                        session.Player.UpdateProperty(session.Player, PropertyInt.LumAugDamageRating, newDR);
                        session.Player.EnqueueBroadcast(false, new GameMessagePublicUpdatePropertyInt(session.Player, PropertyInt.LumAugDamageRating, Convert.ToInt32(newDR)));
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You have increased your Damage Rating to {newDR}! Luminance spent {lumCost:N0}", ChatMessageType.Advancement));
                    }
                    else
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available luminance to raise your Damage Rating.", ChatMessageType.Broadcast));
                    }
                    return;
                }

                if (parameters[0].Equals("Skills", StringComparison.OrdinalIgnoreCase) || parameters[0].Equals("World", StringComparison.OrdinalIgnoreCase))
                {
                    var world = session.Player.LumAugAllSkills;

                    // Do we want to require they already have their 10 Lum Augs from Asheron's Castle?
                    // if (world < 10) {
                    // session.Network.EnqueueSend(new GameMessageSystemChat($"You do not yet have 10 World Luminance Augs, please raise your Skills at Asheron's Castle first.", ChatMessageType.Broadcast));
                    // return;
                    // }
                    var lumCost = 5000000;
                    if (session.Player.SpendLuminance(lumCost))
                    {
                        var newWorld = world + 1;
                        session.Player.UpdateProperty(session.Player, PropertyInt.LumAugAllSkills, newWorld);
                        session.Player.EnqueueBroadcast(false, new GameMessagePublicUpdatePropertyInt(session.Player, PropertyInt.LumAugAllSkills, Convert.ToInt32(newWorld)));
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You have increased all your skills by 1! Luminance spent {lumCost:N0}", ChatMessageType.Advancement));
                    }
                    else
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available luminance to raise your skills.", ChatMessageType.Broadcast));
                    }
                    return;
                }

                if (parameters[0].Equals("Health", StringComparison.OrdinalIgnoreCase) || parameters[0].Equals("Vitality", StringComparison.OrdinalIgnoreCase))
                {
                    var health = session.Player.Health;

                    var lumCost = 7500000;
                    if (session.Player.SpendLuminance(lumCost))
                    {
                        health.StartingValue += 1;
                        session.Network.EnqueueSend(new GameMessagePrivateUpdateVital(session.Player, health));

                        session.Network.EnqueueSend(new GameMessageSystemChat($"You have increased your Vitality By 1! Luminance spent {lumCost:N0}", ChatMessageType.Advancement));
                    }
                    else
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"You do not have enough available luminance to raise your Vitality.", ChatMessageType.Broadcast));
                    }
                    return;
                }
            }

            session.Network.EnqueueSend(new GameMessageSystemChat($"Please specify which Luminance Aug you wish to raise. ex. /enlighten Damage, /enlighten Defense, /enlighten Skills, or /enlighten Health", ChatMessageType.Broadcast));
        }

        // quest info (uses GDLe formatting to match plugin expectations)
        [CommandHandler("myquests", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Shows your quest log")]
        public static void HandleQuests(Session session, params string[] parameters)
        {
            if (!PropertyManager.GetBool("quest_info_enabled").Item)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("The command \"myquests\" is not currently enabled on this server.", ChatMessageType.Broadcast));
                return;
            }

            var quests = session.Player.QuestManager.GetQuests();

            if (quests.Count == 0)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("Quest list is empty.", ChatMessageType.Broadcast));
                return;
            }

            foreach (var playerQuest in quests)
            {
                var text = "";
                var questName = QuestManager.GetQuestName(playerQuest.QuestName);
                var quest = DatabaseManager.World.GetCachedQuest(questName);
                if (quest == null)
                {
                    Console.WriteLine($"Couldn't find quest {playerQuest.QuestName}");
                    continue;
                }

                var minDelta = quest.MinDelta;
                if (QuestManager.CanScaleQuestMinDelta(quest))
                    minDelta = (uint)(quest.MinDelta * PropertyManager.GetDouble("quest_mindelta_rate").Item);

                text += $"{playerQuest.QuestName.ToLower()} - {playerQuest.NumTimesCompleted} solves ({playerQuest.LastTimeCompleted})";
                text += $"\"{quest.Message}\" {quest.MaxSolves} {minDelta}";

                session.Network.EnqueueSend(new GameMessageSystemChat(text, ChatMessageType.Broadcast));
            }
        }

        /// <summary>
        /// For characters/accounts who currently own multiple houses, used to select which house they want to keep
        /// </summary>
        [CommandHandler("house-select", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1, "For characters/accounts who currently own multiple houses, used to select which house they want to keep")]
        public static void HandleHouseSelect(Session session, params string[] parameters)
        {
            HandleHouseSelect(session, false, parameters);
        }

        public static void HandleHouseSelect(Session session, bool confirmed, params string[] parameters)
        {
            if (!int.TryParse(parameters[0], out var houseIdx))
                return;

            // ensure current multihouse owner
            if (!session.Player.IsMultiHouseOwner(false))
            {
                log.Warn($"{session.Player.Name} tried to /house-select {houseIdx}, but they are not currently a multi-house owner!");
                return;
            }

            // get house info for this index
            var multihouses = session.Player.GetMultiHouses();

            if (houseIdx < 1 || houseIdx > multihouses.Count)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Please enter a number between 1 and {multihouses.Count}.", ChatMessageType.Broadcast));
                return;
            }

            var keepHouse = multihouses[houseIdx - 1];

            // show confirmation popup
            if (!confirmed)
            {
                var houseType = $"{keepHouse.HouseType}".ToLower();
                var loc = HouseManager.GetCoords(keepHouse.SlumLord.Location);

                var msg = $"Are you sure you want to keep the {houseType} at\n{loc}?";
                if (!session.Player.ConfirmationManager.EnqueueSend(new Confirmation_Custom(session.Player.Guid, () => HandleHouseSelect(session, true, parameters)), msg))
                    session.Player.SendWeenieError(WeenieError.ConfirmationInProgress);
                return;
            }

            // house to keep confirmed, abandon the other houses
            var abandonHouses = new List<House>(multihouses);
            abandonHouses.RemoveAt(houseIdx - 1);

            foreach (var abandonHouse in abandonHouses)
            {
                var house = session.Player.GetHouse(abandonHouse.Guid.Full);

                HouseManager.HandleEviction(house, house.HouseOwner ?? 0, true);
            }

            // set player properties for house to keep
            var player = PlayerManager.FindByGuid(keepHouse.HouseOwner ?? 0, out bool isOnline);
            if (player == null)
            {
                log.Error($"{session.Player.Name}.HandleHouseSelect({houseIdx}) - couldn't find HouseOwner {keepHouse.HouseOwner} for {keepHouse.Name} ({keepHouse.Guid})");
                return;
            }

            player.HouseId = keepHouse.HouseId;
            player.HouseInstance = keepHouse.Guid.Full;

            player.SaveBiotaToDatabase();

            // update house panel for current player
            var actionChain = new ActionChain();
            actionChain.AddDelaySeconds(3.0f);  // wait for slumlord inventory biotas above to save
            actionChain.AddAction(session.Player, session.Player.HandleActionQueryHouse);
            actionChain.EnqueueChain();

            Console.WriteLine("OK");
        }

        [CommandHandler("debugcast", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Shows debug information about the current magic casting state")]
        public static void HandleDebugCast(Session session, params string[] parameters)
        {
            var physicsObj = session.Player.PhysicsObj;

            var pendingActions = physicsObj.MovementManager.MoveToManager.PendingActions;
            var currAnim = physicsObj.PartArray.Sequence.CurrAnim;

            session.Network.EnqueueSend(new GameMessageSystemChat(session.Player.MagicState.ToString(), ChatMessageType.Broadcast));
            session.Network.EnqueueSend(new GameMessageSystemChat($"IsMovingOrAnimating: {physicsObj.IsMovingOrAnimating}", ChatMessageType.Broadcast));
            session.Network.EnqueueSend(new GameMessageSystemChat($"PendingActions: {pendingActions.Count}", ChatMessageType.Broadcast));
            session.Network.EnqueueSend(new GameMessageSystemChat($"CurrAnim: {currAnim?.Value.Anim.ID:X8}", ChatMessageType.Broadcast));
        }

        [CommandHandler("fixcast", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Fixes magic casting if locked up for an extended time")]
        public static void HandleFixCast(Session session, params string[] parameters)
        {
            var magicState = session.Player.MagicState;

            if (magicState.IsCasting && DateTime.UtcNow - magicState.StartTime > TimeSpan.FromSeconds(5))
            {
                session.Network.EnqueueSend(new GameEventCommunicationTransientString(session, "Fixed casting state"));
                session.Player.SendUseDoneEvent();
                magicState.OnCastDone();
            }
        }

        [CommandHandler("castmeter", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Shows the fast casting efficiency meter")]
        public static void HandleCastMeter(Session session, params string[] parameters)
        {
            if (parameters.Length == 0)
            {
                session.Player.MagicState.CastMeter = !session.Player.MagicState.CastMeter;
            }
            else
            {
                if (parameters[0].Equals("on", StringComparison.OrdinalIgnoreCase))
                    session.Player.MagicState.CastMeter = true;
                else
                    session.Player.MagicState.CastMeter = false;
            }
            session.Network.EnqueueSend(new GameMessageSystemChat($"Cast efficiency meter {(session.Player.MagicState.CastMeter ? "enabled" : "disabled")}", ChatMessageType.Broadcast));
        }

        private static List<string> configList = new List<string>()
        {
            "Common settings:\nConfirmVolatileRareUse, MainPackPreferred, SalvageMultiple, SideBySideVitals, UseCraftSuccessDialog",
            "Interaction settings:\nAcceptLootPermits, AllowGive, AppearOffline, AutoAcceptFellowRequest, DragItemOnPlayerOpensSecureTrade, FellowshipShareLoot, FellowshipShareXP, IgnoreAllegianceRequests, IgnoreFellowshipRequests, IgnoreTradeRequests, UseDeception",
            "UI settings:\nCoordinatesOnRadar, DisableDistanceFog, DisableHouseRestrictionEffects, DisableMostWeatherEffects, FilterLanguage, LockUI, PersistentAtDay, ShowCloak, ShowHelm, ShowTooltips, SpellDuration, TimeStamp, ToggleRun, UseMouseTurning",
            "Chat settings:\nHearAllegianceChat, HearGeneralChat, HearLFGChat, HearRoleplayChat, HearSocietyChat, HearTradeChat, HearPKDeaths, StayInChatMode",
            "Combat settings:\nAdvancedCombatUI, AutoRepeatAttack, AutoTarget, LeadMissileTargets, UseChargeAttack, UseFastMissiles, ViewCombatTarget, VividTargetingIndicator",
            "Character display settings:\nDisplayAge, DisplayAllegianceLogonNotifications, DisplayChessRank, DisplayDateOfBirth, DisplayFishingSkill, DisplayNumberCharacterTitles, DisplayNumberDeaths"
        };

        /// <summary>
        /// Mapping of GDLE -> ACE CharacterOptions
        /// </summary>
        private static Dictionary<string, string> translateOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Common
            { "ConfirmVolatileRareUse", "ConfirmUseOfRareGems" },
            { "MainPackPreferred", "UseMainPackAsDefaultForPickingUpItems" },
            { "SalvageMultiple", "SalvageMultipleMaterialsAtOnce" },
            { "SideBySideVitals", "SideBySideVitals" },
            { "UseCraftSuccessDialog", "UseCraftingChanceOfSuccessDialog" },

            // Interaction
            { "AcceptLootPermits", "AcceptCorpseLootingPermissions" },
            { "AllowGive", "LetOtherPlayersGiveYouItems" },
            { "AppearOffline", "AppearOffline" },
            { "AutoAcceptFellowRequest", "AutomaticallyAcceptFellowshipRequests" },
            { "DragItemOnPlayerOpensSecureTrade", "DragItemToPlayerOpensTrade" },
            { "FellowshipShareLoot", "ShareFellowshipLoot" },
            { "FellowshipShareXP", "ShareFellowshipExpAndLuminance" },
            { "IgnoreAllegianceRequests", "IgnoreAllegianceRequests" },
            { "IgnoreFellowshipRequests", "IgnoreFellowshipRequests" },
            { "IgnoreTradeRequests", "IgnoreAllTradeRequests" },
            { "UseDeception", "AttemptToDeceiveOtherPlayers" },

            // UI
            { "CoordinatesOnRadar", "ShowCoordinatesByTheRadar" },
            { "DisableDistanceFog", "DisableDistanceFog" },
            { "DisableHouseRestrictionEffects", "DisableHouseRestrictionEffects" },
            { "DisableMostWeatherEffects", "DisableMostWeatherEffects" },
            { "FilterLanguage", "FilterLanguage" },
            { "LockUI", "LockUI" },
            { "PersistentAtDay", "AlwaysDaylightOutdoors" },
            { "ShowCloak", "ShowYourCloak" },
            { "ShowHelm", "ShowYourHelmOrHeadGear" },
            { "ShowTooltips", "Display3dTooltips" },
            { "SpellDuration", "DisplaySpellDurations" },
            { "TimeStamp", "DisplayTimestamps" },
            { "ToggleRun", "RunAsDefaultMovement" },
            { "UseMouseTurning", "UseMouseTurning" },

            // Chat
            { "HearAllegianceChat", "ListenToAllegianceChat" },
            { "HearGeneralChat", "ListenToGeneralChat" },
            { "HearLFGChat", "ListenToLFGChat" },
            { "HearRoleplayChat", "ListentoRoleplayChat" },
            { "HearSocietyChat", "ListenToSocietyChat" },
            { "HearTradeChat", "ListenToTradeChat" },
            { "HearPKDeaths", "ListenToPKDeathMessages" },
            { "StayInChatMode", "StayInChatModeAfterSendingMessage" },

            // Combat
            { "AdvancedCombatUI", "AdvancedCombatInterface" },
            { "AutoRepeatAttack", "AutoRepeatAttacks" },
            { "AutoTarget", "AutoTarget" },
            { "LeadMissileTargets", "LeadMissileTargets" },
            { "UseChargeAttack", "UseChargeAttack" },
            { "UseFastMissiles", "UseFastMissiles" },
            { "ViewCombatTarget", "KeepCombatTargetsInView" },
            { "VividTargetingIndicator", "VividTargetingIndicator" },

            // Character Display
            { "DisplayAge", "AllowOthersToSeeYourAge" },
            { "DisplayAllegianceLogonNotifications", "ShowAllegianceLogons" },
            { "DisplayChessRank", "AllowOthersToSeeYourChessRank" },
            { "DisplayDateOfBirth", "AllowOthersToSeeYourDateOfBirth" },
            { "DisplayFishingSkill", "AllowOthersToSeeYourFishingSkill" },
            { "DisplayNumberCharacterTitles", "AllowOthersToSeeYourNumberOfTitles" },
            { "DisplayNumberDeaths", "AllowOthersToSeeYourNumberOfDeaths" },
        };

        /// <summary>
        /// Manually sets a character option on the server. Use /config list to see a list of settings.
        /// </summary>
        [CommandHandler("config", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1, "Manually sets a character option on the server.\nUse /config list to see a list of settings.", "<setting> <on/off>")]
        public static void HandleConfig(Session session, params string[] parameters)
        {
            if (!PropertyManager.GetBool("player_config_command").Item)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("The command \"config\" is not currently enabled on this server.", ChatMessageType.Broadcast));
                return;
            }

            // /config list - show character options
            if (parameters[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var line in configList)
                    session.Network.EnqueueSend(new GameMessageSystemChat(line, ChatMessageType.Broadcast));

                return;
            }

            // translate GDLE CharacterOptions for existing plugins
            if (!translateOptions.TryGetValue(parameters[0], out var param) || !Enum.TryParse(param, out CharacterOption characterOption))
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Unknown character option: {parameters[0]}", ChatMessageType.Broadcast));
                return;
            }

            var option = session.Player.GetCharacterOption(characterOption);

            // modes of operation:
            // on / off / toggle

            // - if none specified, default to toggle
            var mode = "toggle";

            if (parameters.Length > 1)
            {
                if (parameters[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                    mode = "on";
                else if (parameters[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                    mode = "off";
            }

            // set character option
            if (mode.Equals("on"))
                option = true;
            else if (mode.Equals("off"))
                option = false;
            else
                option = !option;

            session.Player.SetCharacterOption(characterOption, option);

            session.Network.EnqueueSend(new GameMessageSystemChat($"Character option {parameters[0]} is now {(option ? "on" : "off")}.", ChatMessageType.Broadcast));

            // update client
            session.Network.EnqueueSend(new GameEventPlayerDescription(session));
        }

        /// <summary>
        /// Force resend of all visible objects known to this player. Can fix rare cases of invisible object bugs.
        /// Can only be used once every 5 mins max.
        /// </summary>
        [CommandHandler("objsend", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Force resend of all visible objects known to this player. Can fix rare cases of invisible object bugs. Can only be used once every 5 mins max.")]
        public static void HandleObjSend(Session session, params string[] parameters)
        {
            // a good repro spot for this is the first room after the door in facility hub
            // in the portal drop / staircase room, the VisibleCells do not have the room after the door
            // however, the room after the door *does* have the portal drop / staircase room in its VisibleCells (the inverse relationship is imbalanced)
            // not sure how to fix this atm, seems like it triggers a client bug..

            if (DateTime.UtcNow - session.Player.PrevObjSend < TimeSpan.FromMinutes(5))
            {
                session.Player.SendTransientError("You have used this command too recently!");
                return;
            }

            var creaturesOnly = parameters.Length > 0 && parameters[0].Contains("creature", StringComparison.OrdinalIgnoreCase);

            var knownObjs = session.Player.GetKnownObjects();

            foreach (var knownObj in knownObjs)
            {
                if (creaturesOnly && !(knownObj is Creature))
                    continue;

                session.Player.RemoveTrackedObject(knownObj, false);
                session.Player.TrackObject(knownObj);
            }
            session.Player.PrevObjSend = DateTime.UtcNow;
        }

        // show player ace server versions
        [CommandHandler("aceversion", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Shows this server's version data")]
        public static void HandleACEversion(Session session, params string[] parameters)
        {
            if (!PropertyManager.GetBool("version_info_enabled").Item)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("The command \"aceversion\" is not currently enabled on this server.", ChatMessageType.Broadcast));
                return;
            }

            var msg = ServerBuildInfo.GetVersionInfo();

            session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.WorldBroadcast));
        }

        // reportbug < code | content > < description >
        [CommandHandler("reportbug", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 2,
            "Generate a Bug Report",
            "<category> <description>\n" +
            "This command generates a URL for you to copy and paste into your web browser to submit for review by server operators and developers.\n" +
            "Category can be the following:\n" +
            "Creature\n" +
            "NPC\n" +
            "Item\n" +
            "Quest\n" +
            "Recipe\n" +
            "Landblock\n" +
            "Mechanic\n" +
            "Code\n" +
            "Other\n" +
            "For the first three options, the bug report will include identifiers for what you currently have selected/targeted.\n" +
            "After category, please include a brief description of the issue, which you can further detail in the report on the website.\n" +
            "Examples:\n" +
            "/reportbug creature Drudge Prowler is over powered\n" +
            "/reportbug npc Ulgrim doesn't know what to do with Sake\n" +
            "/reportbug quest I can't enter the portal to the Lost City of Frore\n" +
            "/reportbug recipe I cannot combine Bundle of Arrowheads with Bundle of Arrowshafts\n" +
            "/reportbug code I was killed by a Non-Player Killer\n"
            )]
        public static void HandleReportbug(Session session, params string[] parameters)
        {
            if (!PropertyManager.GetBool("reportbug_enabled").Item)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("The command \"reportbug\" is not currently enabled on this server.", ChatMessageType.Broadcast));
                return;
            }

            var category = parameters[0];
            var description = "";

            for (var i = 1; i < parameters.Length; i++)
                description += parameters[i] + " ";

            description.Trim();

            switch (category.ToLower())
            {
                case "creature":
                case "npc":
                case "quest":
                case "item":
                case "recipe":
                case "landblock":
                case "mechanic":
                case "code":
                case "other":
                    break;
                default:
                    category = "Other";
                    break;
            }

            var sn = ConfigManager.Config.Server.WorldName;
            var c = session.Player.Name;

            var st = "ACE";

            //var versions = ServerBuildInfo.GetVersionInfo();
            var databaseVersion = DatabaseManager.World.GetVersion();
            var sv = ServerBuildInfo.FullVersion;
            var pv = databaseVersion.PatchVersion;

            //var ct = PropertyManager.GetString("reportbug_content_type").Item;
            var cg = category.ToLower();

            var w = "";
            var g = "";

            if (cg == "creature" || cg == "npc"|| cg == "item" || cg == "item")
            {
                var objectId = new ObjectGuid();
                if (session.Player.HealthQueryTarget.HasValue || session.Player.ManaQueryTarget.HasValue || session.Player.CurrentAppraisalTarget.HasValue)
                {
                    if (session.Player.HealthQueryTarget.HasValue)
                        objectId = new ObjectGuid((uint)session.Player.HealthQueryTarget);
                    else if (session.Player.ManaQueryTarget.HasValue)
                        objectId = new ObjectGuid((uint)session.Player.ManaQueryTarget);
                    else
                        objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);

                    //var wo = session.Player.CurrentLandblock?.GetObject(objectId);

                    var wo = session.Player.FindObject(objectId.Full, Player.SearchLocations.Everywhere);

                    if (wo != null)
                    {
                        w = $"{wo.WeenieClassId}";
                        g = $"0x{wo.Guid:X8}";
                    }
                }
            }

            var l = session.Player.Location.ToLOCString();

            var issue = description;

            var urlbase = $"https://www.accpp.net/bug?";

            var url = urlbase;
            if (sn.Length > 0)
                url += $"sn={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sn))}";
            if (c.Length > 0)
                url += $"&c={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(c))}";
            if (st.Length > 0)
                url += $"&st={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(st))}";
            if (sv.Length > 0)
                url += $"&sv={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sv))}";
            if (pv.Length > 0)
                url += $"&pv={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pv))}";
            //if (ct.Length > 0)
            //    url += $"&ct={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ct))}";
            if (cg.Length > 0)
            {
                if (cg == "npc")
                    cg = cg.ToUpper();
                else
                    cg = char.ToUpper(cg[0]) + cg.Substring(1);
                url += $"&cg={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(cg))}";
            }
            if (w.Length > 0)
                url += $"&w={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(w))}";
            if (g.Length > 0)
                url += $"&g={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(g))}";
            if (l.Length > 0)
                url += $"&l={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(l))}";
            if (issue.Length > 0)
                url += $"&i={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(issue))}";

            var msg = "\n\n\n\n";
            msg += "Bug Report - Copy and Paste the following URL into your browser to submit a bug report\n";
            msg += "-=-\n";
            msg += $"{url}\n";
            msg += "-=-\n";
            msg += "\n\n\n\n";

            session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.AdminTell));
        }
    }
}
