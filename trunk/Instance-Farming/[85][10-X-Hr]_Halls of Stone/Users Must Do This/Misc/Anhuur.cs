/* Created By TheBrodieMan and Raphus, derived from Invoking the Serpent */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;

using CommonBehaviors.Actions;

using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Honorbuddy.QuestBehaviorCore;

using Action = Styx.TreeSharp.Action;

// <!-- CustomBehavior - Anhuur - contributed by Brodie -->	
//					<CustomBehavior File="Misc\Anhuur" />
					
					
namespace Honorbuddy.Quest_Behaviors.SpecificQuests.Anhuur
{
	[CustomBehaviorFileName(@"Anhuur")]
	public class Anhuur : CustomForcedBehavior
	{
		public Anhuur(Dictionary<string, string> args)
			: base(args)
		{
			try
			{
			}
			catch
			{
				Logging.Write("Problem parsing a QuestId in behavior: Halls of Origination - Anhuur");
			}
		}
		public int QuestId { get; set; }
		private bool _isBehaviorDone;
		public static int ItokaId = 39425; // boss
		public static int AnvilId1 = 203133; //object
		public static int AnvilId2 = 203136; // object
		public static int ItokaAura = 74938; // aura
		private Composite _root;
		public QuestCompleteRequirement questCompleteRequirement = QuestCompleteRequirement.NotComplete;
		public QuestInLogRequirement questInLogRequirement = QuestInLogRequirement.InLog;
		
		public override bool IsDone { get { return _isBehaviorDone; } }
		
		private LocalPlayer Me { get { return (StyxWoW.Me); } }

		public override void OnStart()
		{
			OnStart_HandleAttributeProblem();
			if (!IsDone)
			{
				if (TreeRoot.Current != null && TreeRoot.Current.Root != null && TreeRoot.Current.Root.LastStatus != RunStatus.Running)
				{
					var currentRoot = TreeRoot.Current.Root;
					if (currentRoot is GroupComposite)
					{
						var root = (GroupComposite)currentRoot;
						root.InsertChild(0, CreateBehavior());
					}
				}
				PlayerQuest Quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
				TreeRoot.GoalText = ((Quest != null) ? ("\"" + Quest.Name + "\"") : "In Progress");
				
				TreeHooks.Instance.InsertHook("Combat_Main", 0, AnvilBehavior());
				Targeting.Instance.RemoveTargetsFilter += Instance_RemoveTargetsFilter;
			}
		}
		
		static void Instance_RemoveTargetsFilter(System.Collections.Generic.List<WoWObject> units)
		{
			units.RemoveAll(o =>
				{
					var unit = o as WoWUnit;

					if (unit != null && unit.HasAura(ItokaAura))
						return true;

					return false;
				});
		}

		public WoWUnit Itoka
		{
			get
			{
				return ObjectManager.GetObjectsOfType<WoWUnit>().Where
					(u => u.Entry == ItokaId && u.IsAlive)
					.OrderBy(u => u.DistanceSqr).FirstOrDefault();
			}
		}
		
		public WoWUnit Anvils
		{
			get
			{
				return ObjectManager.GetObjectsOfType<WoWUnit>().Where
					(u => (u.Entry == AnvilId1 || u.Entry == AnvilId2) && u.IsValid)
					.OrderBy(u => u.DistanceSqr).FirstOrDefault();
			}
		}
		
		public Composite DoneYet
		{
			get
			{
				return
					new Decorator(ret => Me.IsQuestObjectiveComplete(QuestId, 1) || Itoka == null, new Action(delegate
					{
						TreeRoot.StatusText = "Finished!";
						_isBehaviorDone = true;
						return RunStatus.Success;
					}));
			}
		}
		
		public override void OnFinished()
		{
			TreeHooks.Instance.RemoveHook("Combat_Main", AnvilBehavior());
			Targeting.Instance.RemoveTargetsFilter -= Instance_RemoveTargetsFilter;
		}
		
		public Composite AnvilBehavior()
		{
			return _root ?? (_root = new Decorator(ret => !_isBehaviorDone,
				new PrioritySelector(
					DoneYet,
					new Decorator(context => Anvils != null && (BotPoi.Current.Entry != AnvilId1 || BotPoi.Current.Entry != AnvilId2),
						new Sequence(
							new ActionSetPoi(true, context => new BotPoi(Anvils, PoiType.Interact)),
							new Action(context => Anvils.Interact()),
							new Action(context =>
							{
								var poiUnit = BotPoi.Current.AsObject as WoWUnit;
								if (poiUnit.Distance > poiUnit.InteractRange)
									Navigator.MoveTo(poiUnit.Location);
							}))),
					new Decorator(context => Anvils == null && BotPoi.Current.Entry != ItokaId,
						new Sequence(
							new ActionSetPoi(true, context => new BotPoi(Itoka, PoiType.Kill)),
							new Action(context => Itoka.Target())
				)))));
		}
	}
}