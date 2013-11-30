using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CommonBehaviors.Actions;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Styx.Bot.Quest_Behaviors {
    [CustomBehaviorFileName(@"Misc\DKEnchantWeapon")]
    public class DKEnchantWeapon : CustomForcedBehavior {

        // ===========================================================
        // Constants
        // ===========================================================

        // ===========================================================
        // Fields
        // ===========================================================

        private Composite _root;

        private bool _isDisposed;

        public LocalPlayer Me = StyxWoW.Me;

        private bool _isBehaviorDone;
        private bool _runOnce;
        private WoWObject _runeforge;


        private const int TradeSkillID = 53428;
        private int _itemID;
        private int _enchantID;
        private int _weaponEnchantID;

        // ===========================================================
        // Constructors
        // ===========================================================

        public DKEnchantWeapon(Dictionary<string, string> args)
            : base(args) {
            try {
                SpellID = GetAttributeAsNullable("SpellID", true, ConstrainAs.SpellId, null) ?? 0;


            } catch(Exception except) {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                LogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message
                                    + "\nFROM HERE:\n"
                                    + except.StackTrace + "\n");
                IsAttributeProblem = true;
            }
        }

        // ===========================================================
        // Getter & Setter
        // ===========================================================

        public int SpellID { get; set; }

        // ===========================================================
        // Methods for/from SuperClass/Interfaces
        // ===========================================================

        public override void OnStart() {
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();

            if(IsDone) {
                return;
            }

            BotEvents.OnBotStop += BotEvents_OnBotStop;
        }

        public override void OnFinished() {
            if(!_isDisposed) {
                BotEvents.OnBotStop -= BotEvents_OnBotStop;

                _isBehaviorDone = false;
                _runeforge = null;

                if(Lua.GetReturnVal<bool>("return TradeSkillFrame:IsVisible()", 0)) {
                    Lua.DoString("CloseTradeSkill()");
                }

                GC.SuppressFinalize(this);

                base.OnFinished();
            }

            _isDisposed = true;
        }

        public override bool IsDone { get { return (_isBehaviorDone); } }

        protected override Composite CreateBehavior() {
            return _root ?? (_root =
                new PrioritySelector(
                    new Decorator(context => !_isBehaviorDone,
                        new Sequence(
                            new Action(ret => SetIDs()),
                            new DecoratorContinue(context => _enchantID == _weaponEnchantID,
                                new Action(ret => _isBehaviorDone = true)
                            ),
                            new DecoratorContinue(context => _enchantID != _weaponEnchantID,
                                new DecoratorContinue(context => IsViable(Me) && Me.IsAlive && !Me.Combat,
                                    new Sequence(
                                        new DecoratorContinue(context => _runeforge == null,
                                            new Action(ret => FindRuneforge())
                                        ),
                                        new DecoratorContinue(context => _runeforge != null,
                                            new Action(ret => NavigateToRuneforge())
                                        ),
                                        new DecoratorContinue(context => _runeforge.WithinInteractRange,
                                            new Sequence(
                                                new DecoratorContinue(context => Lua.GetReturnVal<bool>("return StaticPopup1:IsVisible()", 0),
                                                    new Action(ret => Lua.DoString("StaticPopup1Button1:Click()"))
                                                ),
                                                new DecoratorContinue(context => !Lua.GetReturnVal<bool>("return TradeSkillFrame:IsVisible()", 0),
                                                    new Action(ret => WoWSpell.FromId(TradeSkillID).Cast())
                                                ),
                                                new DecoratorContinue(context => Lua.GetReturnVal<bool>("return TradeSkillFrame:IsVisible()", 0) && !_runOnce,
                                                    new Sequence(
                                                        new Action(ret => Lua.DoString("DoTradeSkill(" + GetTradeSkillIndex() + ", 1)")),
                                                        new Action(ret => StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == _itemID).Use()),
                                                        new DecoratorContinue(context => Lua.GetReturnVal<bool>("return StaticPopup1:IsVisible()", 0),
                                                            new Action(ret => Lua.DoString("StaticPopup1Button1:Click()"))
                                                        ),
                                                        new Action(ret => _runOnce = true)
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            );
        }

        // ===========================================================
        // Methods
        // ===========================================================

        public void CustomNormalLog(string message, params object[] args) {
            Logging.Write(Colors.DeepSkyBlue, "[DKEnchantWeapon]: " + message, args);
        }

        public bool IsViable(WoWObject pWoWObject) {
            return (pWoWObject != null) && pWoWObject.IsValid;
        }

        public void FindRuneforge() {
            _runeforge = ObjectManager.GetObjectsOfTypeFast<WoWObject>().FirstOrDefault(runeforge => runeforge.IsValid && (runeforge.Entry == 207577 || runeforge.Entry == 207578 || runeforge.Entry == 207579));
        }

        public void NavigateToRuneforge() {
            if(_runeforge.WithinInteractRange) {
                return;
            }

            CustomNormalLog("Not within range of a runeforge.");

            var runeforgeLocation = WoWMovement.CalculatePointFrom(_runeforge.Location, 7f);

            Navigator.MoveTo(runeforgeLocation);
        }

        private int GetTradeSkillIndex() {
            var count = Lua.GetReturnVal<int>("return GetNumTradeSkills()", 0);
            for(var i = 1; i <= count; i++) {
                var link = Lua.GetReturnVal<string>("return GetTradeSkillItemLink(" + i + ")", 0);

                // Make sure it's not a category!
                if(string.IsNullOrEmpty(link)) {
                    continue;
                }

                link = link.Remove(0, link.IndexOf(':') + 1);

                if(link.IndexOf(':') != -1)
                    link = link.Remove(link.IndexOf(':'));
                else
                    link = link.Remove(link.IndexOf('|'));

                var id = int.Parse(link);

                //CustomNormalLog("ID: " + id + " at " + i + " - " + WoWSpell.FromId(id).Name);

                if(id == SpellID)
                    return i;
            }

            return 0;
        }

        /// <summary>
        ///     Returns an item link for an item in the unit's inventory. The player's inventory is actually extended to include items in the bank, 
        ///     items in the player's containers and the player's key ring in addition to the items the player has equipped. The appropriate inventoryID 
        ///     can be found by calling the appropriate function.
        /// </summary>
        /// <param name="pUnit">A unit to query; only valid for 'player' or the unit currently being inspected (string, unitID)</param>
        /// <param name="pSlot">An inventory slot number, as can be obtained from GetInventorySlotInfo. (number, inventoryID)</param>
        /// <returns>
        ///     <para>link = An item link for the given item (string, hyperlink)</para>
        /// </returns>
        /// <remarks>
        ///     <para>-- Inventory slots</para>
        ///     <para>INVSLOT_AMMO            = 0;</para>
        ///     <para>INVSLOT_HEAD            = 1; INVSLOT_FIRST_EQUIPPED = INVSLOT_HEAD;</para>
        ///     <para>INVSLOT_NECK            = 2;</para>
        ///     <para>INVSLOT_SHOULDER        = 3;</para>
        ///     <para>INVSLOT_BODY            = 4;</para>
        ///     <para>INVSLOT_CHEST           = 5;</para>
        ///     <para>INVSLOT_WAIST           = 6;</para>
        ///     <para>INVSLOT_LEGS            = 7;</para>
        ///     <para>INVSLOT_FEET            = 8;</para>
        ///     <para>INVSLOT_WRIST           = 9;</para>
        ///     <para>INVSLOT_HAND            = 10;</para>
        ///     <para>INVSLOT_FINGER1         = 11;</para>
        ///     <para>INVSLOT_FINGER2         = 12;</para>
        ///     <para>INVSLOT_TRINKET1        = 13;</para>
        ///     <para>INVSLOT_TRINKET2        = 14;</para>
        ///     <para>INVSLOT_BACK            = 15;</para>
        ///     <para>INVSLOT_MAINHAND        = 16;</para>
        ///     <para>INVSLOT_OFFHAND         = 17;</para>
        ///     <para>INVSLOT_RANGED          = 18;</para>
        ///     <para>INVSLOT_TABARD          = 19;</para>
        ///     <para>INVSLOT_LAST_EQUIPPED   = INVSLOT_TABARD;</para>
        ///     <para> </para>
        ///     <para>http://wowprogramming.com/docs/api/GetInventoryItemLink</para>
        /// </remarks>
        public string GetInventoryItemLink(string pUnit, int pSlot) {
            return Lua.GetReturnVal<string>(string.Format("return GetInventoryItemLink('{0}', {1})", pUnit, pSlot), 0);
        }

        public void SetEnchantIDsToSpellIDs() {
            // Rune of Cinderglacier
            if(SpellID == 53341) {
                _enchantID = 3369;
            }

            // Rune of Lichbane
            if(SpellID == 53331) {
                _enchantID = 3366;
            }

            // Rune of Razorice
            if(SpellID == 53343) {
                _enchantID = 3370;
            }

            // Rune of Spellbreaking
            if(SpellID == 54447) {
                _enchantID = 3595;
            }

            // Rune of Spellshattering
            if(SpellID == 53342) {
                _enchantID = 3367;
            }

            // Rune of Swordbreaking
            if(SpellID == 54446) {
                _enchantID = 3594;
            }

            // Rune of Swordshattering
            if(SpellID == 53323) {
                _enchantID = 3365;
            }

            // Rune of the Fallen Crusader
            if(SpellID == 53344) {
                _enchantID = 3368;
            }

            // Rune of the Nerubian Carapace
            if(SpellID == 70164) {
                _enchantID = 3883;
            }

            // Rune of the Stoneskin Gargoyle
            if(SpellID == 62158) {
                _enchantID = 3847;
            }
        }


        public void GetIDsFromString(string strSource) {
            var first = strSource.IndexOf(":", 0, StringComparison.Ordinal);
            var firstString = strSource.Substring(first);

            var itemId = firstString.IndexOf(":", 0, StringComparison.Ordinal);
            var itemIdString = firstString.Substring(itemId + 1);

            var enchantId = itemIdString.IndexOf(":", 0, StringComparison.Ordinal);
            var enchantIdString = itemIdString.Substring(enchantId + 1);

            var rest = enchantIdString.IndexOf(":", 0, StringComparison.Ordinal);
            var restString = enchantIdString.Substring(rest);

            var desiredItemIDString = itemIdString.Substring(0, (itemIdString.Length - enchantIdString.Length - 1));

            var desiredEnchantIDString = enchantIdString.Substring(0, (enchantIdString.Length - restString.Length));

            var desiredItemIDInt = Convert.ToInt32(desiredItemIDString);
            var desiredEnchantIDInt = Convert.ToInt32(desiredEnchantIDString);

            _itemID = desiredItemIDInt;
            _weaponEnchantID = desiredEnchantIDInt;
        }

        public void SetIDs() {
            if(_enchantID == 0) { SetEnchantIDsToSpellIDs(); }

            var link = GetInventoryItemLink("player", 16);
            GetIDsFromString(link);
            
            if(_itemID == 0 || _enchantID == _weaponEnchantID) {
                _isBehaviorDone = true;
            }
        }

        // ===========================================================
        // Inner and Anonymous Classes
        // ===========================================================

        private void BotEvents_OnBotStop(EventArgs args) { OnFinished(); }
    }
}
